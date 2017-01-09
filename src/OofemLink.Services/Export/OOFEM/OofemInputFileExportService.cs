using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OofemLink.Common.OofemNames;
using OofemLink.Common.Enumerations;
using OofemLink.Common.Extensions;
using OofemLink.Data;
using OofemLink.Data.Entities;

namespace OofemLink.Services.Export.OOFEM
{
	class OofemInputFileExportService : IExportService
	{
		#region Fields, constructor

		readonly string inputFileFullPath, outputFileFullPath;
		readonly DataContext dataContext;

		public OofemInputFileExportService(DataContext dataContext, string inputFileFullPath, string outputFileDirectory = null)
		{
			this.dataContext = dataContext;
			Debug.Assert(!string.IsNullOrEmpty(inputFileFullPath));
			this.inputFileFullPath = inputFileFullPath;
			if (string.IsNullOrEmpty(outputFileDirectory))
				outputFileDirectory = Path.GetDirectoryName(inputFileFullPath);
			this.outputFileFullPath = Path.Combine(outputFileDirectory, Path.ChangeExtension(Path.GetFileName(inputFileFullPath), "out"));
		}

		#endregion

		#region Public methods

		public void ExportSimulation(int simulationId)
		{
			// TODO: refactor this method to DataAccess.ModelService class

			// load simulation from db
			var simulation = dataContext.Simulations
								.Include(s => s.Project)
								.Include(s => s.TimeSteps)
								.Include(s => s.Model)
								.ThenInclude(m => m.Meshes)
								.FirstOrDefault(s => s.Id == simulationId);

			if (simulation == null)
				throw new KeyNotFoundException($"Simulation with id {simulationId} was not found.");
			if (simulation.Model == null)
				throw new InvalidDataException($"Simulation {simulationId} does not contain any model.");

			var model = simulation.Model;

			if (model.Meshes.Count == 0)
				throw new InvalidDataException($"No mesh found for model {model.Id}.");
			if (model.Meshes.Count > 1)
				throw new NotSupportedException($"Multiple meshes for single model are not yet supported (model {model.Id}).");

			var mesh = model.Meshes.Single();

			List<Node> nodes;
			List<Element> elements;
			List<ModelAttribute> crossSections;
			List<ModelAttribute> materials;
			List<ModelAttribute> boundaryConditions;
			List<TimeFunction> timeFunctions;
			Dictionary<int, ModelAttribute> elementLcsMap;

			// load all model entities from db
			{
				// TODO: [Optimization] are orderings necessary?

				var nodesQuery = from node in dataContext.Nodes
								 where node.MeshId == mesh.Id
								 orderby node.Id
								 select node;
				var elementsQuery = from element in dataContext.Elements.Include(e => e.ElementNodes)
									where element.MeshId == mesh.Id
									orderby element.Id
									select element;
				var crossSectionsQuery = from attribute in dataContext.Attributes.Include(a => a.ChildAttributes)
										 where attribute.ModelId == model.Id
										 where attribute.Type == AttributeType.CrossSection
										 orderby attribute.Id
										 select attribute;
				var materialsQuery = from attribute in dataContext.Attributes
									 where attribute.ModelId == model.Id
									 where attribute.Type == AttributeType.Material
									 orderby attribute.Id
									 select attribute;
				var boundaryConditionsQuery = from attribute in dataContext.Attributes
											  where attribute.ModelId == model.Id
											  where attribute.Type == AttributeType.BoundaryCondition
											  orderby attribute.Id
											  select attribute;
				var timeFunctionsQuery = from timeFunction in dataContext.TimeFunctions.Include(tf => tf.Values)
										 where timeFunction.ModelId == model.Id
										 orderby timeFunction.Id
										 select timeFunction;
				var lcsQuery = from curveAttribute in dataContext.Set<CurveAttribute>()
							   where curveAttribute.ModelId == model.Id
							   where curveAttribute.Attribute.Type == AttributeType.LocalCoordinateSystem
							   from curveElement in curveAttribute.Curve.CurveElements
							   where curveElement.MeshId == mesh.Id
							   group curveAttribute.Attribute by curveElement.ElementId;

				// materialize queries
				nodes = nodesQuery.ToList();
				elements = elementsQuery.ToList();
				crossSections = crossSectionsQuery.ToList();
				materials = materialsQuery.ToList();
				boundaryConditions = boundaryConditionsQuery.ToList();
				timeFunctions = timeFunctionsQuery.ToList();
				elementLcsMap = lcsQuery.ToDictionary(g => g.Key, g => g.Single()); // TODO: handle multiple lcs attributes per element
			}

			Dictionary<int, Set> attributeIdSetMap = createSetMapForModelAttributes(model.Id, mesh.Id);
			List<Set> sets = attributeIdSetMap.Values.Distinct().OrderBy(s => s.Id).ToList();

			// =========================================================================================
			using (var input = new InputBuilder(inputFileFullPath))
			{
				// Output file name
				input.AddPlainText(outputFileFullPath);
				// Description
				input.AddPlainText($"Project: {simulation.Project?.Name}, Task: {simulation.TaskName}");

				// Type of so-called engineering model, willbe the same for now, for non-linear problems we will need switch to nonlinear static. The nlstatic can have several keywords specifying solver parameters, convergence criteria and so on, nmodules = number of export modules
				input.AddEngineeringModel(
						engineeringModelName: "LinearStatic", // TODO: take this from analysis parameters in Simulation object
						numberOfTimeSteps: simulation.TimeSteps.Count,
						numberOfExportModules: 1 /**/
					);

				// the export module is vtk
				// TODO: this is hard-coded now, enable this to be configurable
				input.AddPlainText("vtkxml tstep_all domain_all primvars 1 1");

				// domain specify degrees of freedom, but it is not used anymore and will be removed in near future, it remains here just for backward compatibility
				// TODO: avoid hard-coded string
				input.AddDomain("3dshell");

				// default outputmanager giving outfile, in this case beam3d.out, only specific elements or time steps can be exported, here we export all of them
				// TODO: avoid hard-coded string
				input.AddPlainText("OutputManager tstep_all dofman_all element_all");

				// number of dofmanagers(generalization of nodes), number of elements, n of cross-sections, n of materials, boundary conditions, initial conditions, load time functions, and sets
				input.AddRecordCounts(
						dofManagerCount: nodes.Count,
						elementCount: elements.Count,
						crossSectionCount: crossSections.Count,
						materialCount: materials.Count,
						boundaryConditionCount: boundaryConditions.Count,
						initialConditionCount: 0, /*no initial conditions for time-independent analysis (statics)*/
						timeFunctionCount: timeFunctions.Count,
						setCount: sets.Count
					);

				addDebugComment(input, "NODES");
				foreach (var node in nodes)
				{
					input.AddNode(node.Id).WithCoordinates(node.X, node.Y, node.Z);
				}

				addDebugComment(input, "ELEMENTS");
				foreach (var element in elements)
				{
					var nodeIds = (from elementNode in element.ElementNodes
								   orderby elementNode.Rank
								   select elementNode.NodeId).ToArray();

					switch (element.Type)
					{
						case CellType.LineLinear:
							Debug.Assert(nodeIds.Length == 2);
							ModelAttribute lcsAttribute;
							string lcsParameter;
							if (elementLcsMap.TryGetValue(element.Id, out lcsAttribute))
								lcsParameter = $"{lcsAttribute.Name} {lcsAttribute.Parameters}";
							else
								lcsParameter = getDefaultZAxisParameterBeam3d(simulation.DimensionFlags);
							input.AddElement(ElementNames.beam3d, element.Id).WithNodes(nodeIds).WithParameter(lcsParameter);
							break;
						case CellType.TriangleLinear:
							Debug.Assert(nodeIds.Length == 3);
							input.AddElement(ElementNames.mitc4shell, element.Id).WithNodes(nodeIds[0], nodeIds[1], nodeIds[2], nodeIds[2]); // last node is doubled
							break;
						case CellType.QuadLinear:
							Debug.Assert(nodeIds.Length == 4);
							input.AddElement(ElementNames.mitc4shell, element.Id).WithNodes(nodeIds);
							break;
						default:
							throw new NotSupportedException($"Element type {element.Type} is not supported.");
					}
				}

				var attributeIdToMaterialIdMap = new Dictionary<int, int>();
				for (int index = 0; index < materials.Count; index++)
				{
					attributeIdToMaterialIdMap.Add(materials[index].Id, index + 1);
				}

				addDebugComment(input, "CROSS-SECTIONS");
				for (int i = 0; i < crossSections.Count; i++)
				{
					var crossSection = crossSections[i];
					int childAttributeId = crossSection.ChildAttributes.Single(a => a.ChildAttribute.Type == AttributeType.Material).ChildAttributeId; // TODO: handle cases with non-single referenced materials
					input.AddCrossSection(crossSection.Name, id: i + 1)
						 .WithParameter(crossSection.Parameters)
						 .HasMaterial(materialId: attributeIdToMaterialIdMap[childAttributeId])
						 .AppliesToSet(attributeIdSetMap[crossSection.Id].Id);
				}

				addDebugComment(input, "MATERIALS");
				foreach (var material in materials)
				{
					input.AddMaterial(material.Name, id: attributeIdToMaterialIdMap[material.Id]).WithParameter(material.Parameters);
				}

				addDebugComment(input, "BOUNDARY CONDITIONS");
				for (int i = 0; i < boundaryConditions.Count; i++) // write Boundary Conditions (including Loads)
				{
					var bc = boundaryConditions[i];
					input.AddBoundaryCondition(bc.Name, id: i + 1).InTime(bc.TimeFunctionId).WithParameter(bc.Parameters).AppliesToSet(attributeIdSetMap[bc.Id].Id);
				}

				addDebugComment(input, "LOAD TIME FUNCTIONS");
				foreach (var timeFunction in timeFunctions)
				{
					var timeFunctionBuilder = input.AddTimeFunction(timeFunction.Name, timeFunction.Id);
					switch (timeFunction.Name) // TODO: replace with type switch when C# 7 is available
					{
						case TimeFunctionNames.ConstantFunction:
							timeFunctionBuilder.WithValue(((ConstantFunction)timeFunction).ConstantValue);
							break;
						case TimeFunctionNames.PeakFunction:
							var tfValue = timeFunction.Values.Single();
							timeFunctionBuilder.InTime(tfValue.TimeStep.Time ?? tfValue.TimeStep.Number).WithValue(tfValue.Value);
							break;
						case TimeFunctionNames.PiecewiseLinFunction:
							timeFunctionBuilder.WithTimeValuePairs(createTimeStepFunctionValuePairs(simulation, timeFunction));
							break;
						default:
							throw new NotSupportedException($"Load time function of type '{timeFunction.Name}' is not supported");
					}
				}

				addDebugComment(input, "SETS");
				foreach (var set in sets)
				{
					input.AddSet(set.Id)
						.WithNodes(set.Nodes)
						.WithElements(set.Elements)
						.WithElementEdges(set.ElementEdges)
						.WithElementSurfaces(set.ElementSurfaces);
				}
			}
		}

		#endregion

		#region Private methods

		private string getDefaultZAxisParameterBeam3d(ModelDimensions dimensionFlags)
		{
			switch (dimensionFlags)
			{
				case ModelDimensions.XY:
				case ModelDimensions.XYZ:
					return $"{Keyword.zaxis} 3 0 0 1";
				case ModelDimensions.XZ:
					return $"{Keyword.zaxis} 3 0 1 0";
				case ModelDimensions.YZ:
					return $"{Keyword.zaxis} 3 1 0 0";
				default:
					throw new InvalidOperationException($"Unexpected dimension of simulation '{dimensionFlags}'");
			}
		}

		private List<KeyValuePair<double, double>> createTimeStepFunctionValuePairs(Simulation simulation, TimeFunction timeFunction)
		{
			var valueMap = timeFunction.Values.ToDictionary(v => v.TimeStepId, v => v.Value);
			var resultQuery = from timeStep in simulation.TimeSteps
							  orderby timeStep.Time ?? timeStep.Number // The particular time values in t array should be sorted according to time scale
							  select new KeyValuePair<double, double>(timeStep.Time ?? timeStep.Number, valueMap.GetValueOrDefault(timeStep.Id));
			return resultQuery.ToList();
		}

		private Dictionary<int, Set> createSetMapForModelAttributes(int modelId, int meshId)
		{
			// TODO: [Optimization] avoid duplication of sets. If the set with same nodes, elements, etc. already exists then don't create new one

			var map = new Dictionary<int, Set>();
			int setId = 1;

			// AttributeTarget.Node:
			{
				var query = from vertexAttribute in dataContext.Set<VertexAttribute>()
							where vertexAttribute.ModelId == modelId
							where vertexAttribute.Attribute.Target == AttributeTarget.Node
							from vertexNode in vertexAttribute.Vertex.VertexNodes
							where vertexNode.MeshId == meshId
							orderby vertexNode.NodeId
							group vertexNode.NodeId by vertexAttribute.AttributeId;
				foreach (var group in query)
				{
					int attributeId = group.Key;
					Set set = new Set(setId++).WithNodes(group.ToArray());
					map.Add(attributeId, set);
				}
			}

			// AttributeTarget.Edge:
			{
				var query = from curveAttribute in dataContext.Set<CurveAttribute>()
							where curveAttribute.ModelId == modelId
							where curveAttribute.Attribute.Target == AttributeTarget.Edge
							from macroCurve in curveAttribute.Macro.MacroCurves
							where macroCurve.CurveId == curveAttribute.CurveId
							from curveElement in macroCurve.Curve.CurveElements
							where curveElement.MeshId == meshId
							orderby curveElement.ElementId, curveElement.Rank
							group new KeyValuePair<int, short>(curveElement.ElementId, /*EdgeId:*/ curveElement.Rank) by curveAttribute.AttributeId;
				foreach (var group in query)
				{
					int attributeId = group.Key;
					Set set = new Set(setId++).WithElementEdges(group.ToArray());
					map.Add(attributeId, set);
				}
			}

			// AttributeTarget.Surface:
			{
				var query = from surfaceAttribute in dataContext.Set<SurfaceAttribute>()
							where surfaceAttribute.ModelId == modelId
							where surfaceAttribute.Attribute.Target == AttributeTarget.Surface
							from macroSurface in surfaceAttribute.Macro.MacroSurfaces
							where macroSurface.SurfaceId == surfaceAttribute.SurfaceId
							from surfaceElement in macroSurface.Surface.SurfaceElements
							where surfaceElement.MeshId == meshId
							orderby surfaceElement.ElementId, surfaceElement.Rank
							group new KeyValuePair<int, short>(surfaceElement.ElementId, /*SurfaceId:*/ surfaceElement.Rank) by surfaceAttribute.AttributeId;
				foreach (var group in query)
				{
					int attributeId = group.Key;
					Set set = new Set(setId++).WithElementSurfaces(group.ToArray());
					map.Add(attributeId, set);
				}
			}

			// AttributeTarget.Volume:
			{
				var elements1dQuery = from curveAttribute in dataContext.Set<CurveAttribute>()
									  where curveAttribute.ModelId == modelId
									  where curveAttribute.Attribute.Target == AttributeTarget.Volume
									  from macroCurve in curveAttribute.Macro.MacroCurves
									  where macroCurve.CurveId == curveAttribute.CurveId
									  from curveElement in macroCurve.Curve.CurveElements
									  where curveElement.MeshId == meshId
									  group curveElement.ElementId by curveAttribute.AttributeId;
				var elements2dQuery = from surfaceAttribute in dataContext.Set<SurfaceAttribute>()
									  where surfaceAttribute.ModelId == modelId
									  where surfaceAttribute.Attribute.Target == AttributeTarget.Volume
									  from macroSurface in surfaceAttribute.Macro.MacroSurfaces
									  where macroSurface.SurfaceId == surfaceAttribute.SurfaceId
									  from surfaceElement in macroSurface.Surface.SurfaceElements
									  where surfaceElement.MeshId == meshId
									  group surfaceElement.ElementId by surfaceAttribute.AttributeId;
				var elements3dQuery = from volumeAttribute in dataContext.Set<VolumeAttribute>()
									  where volumeAttribute.ModelId == modelId
									  where volumeAttribute.Attribute.Target == AttributeTarget.Volume
									  from volumeElement in volumeAttribute.Volume.VolumeElements
									  where volumeElement.MeshId == meshId
									  group volumeElement.ElementId by volumeAttribute.AttributeId;
				var query = elements1dQuery.Concat(elements2dQuery).Concat(elements3dQuery);

				foreach (var group in query)
				{
					int attributeId = group.Key;
					Set set = new Set(setId++).WithElements(group.OrderBy(id => id).ToArray());
					map.Add(attributeId, set);
				}
			}

			return map;
		}

		[Conditional("DEBUG")]
		private static void addDebugComment(InputBuilder input, string comment)
		{
			input.AddComment(comment);
		}

		#endregion
	}
}
