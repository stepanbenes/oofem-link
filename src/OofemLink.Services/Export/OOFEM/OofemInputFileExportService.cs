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

		readonly string fileFullPath;
		readonly DataContext dataContext;

		public OofemInputFileExportService(DataContext dataContext, string fileFullPath)
		{
			this.dataContext = dataContext;
			if (!string.IsNullOrEmpty(fileFullPath))
				this.fileFullPath = fileFullPath;
			else
				this.fileFullPath = "oofem.in";
		}

		#endregion

		#region Public methods

		public void ExportSimulation(int simulationId)
		{
			using (var input = new InputBuilder(fileFullPath))
			{
				createOofemInput(input, simulationId);
			}
		}

		#endregion

		#region Private methods

		private void createOofemInput(InputBuilder input, int simulationId)
		{
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

			List<Node> nodes;
			List<Element> elements;
			List<ModelAttribute> crossSections;
			List<ModelAttribute> materials;
			List<ModelAttribute> boundaryConditions;
			List<TimeFunction> timeFunctions;

			// load all model entities from db
			{
				var mesh = model.Meshes.Single();

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

				nodes = nodesQuery.ToList();
				elements = elementsQuery.ToList();
				crossSections = crossSectionsQuery.ToList();
				materials = materialsQuery.ToList();
				boundaryConditions = boundaryConditionsQuery.ToList();
				timeFunctions = timeFunctionsQuery.ToList();
			}

			Dictionary<ModelAttribute, Set> attributeSetMap = createSetMapForAttributes(crossSections, boundaryConditions);
			List<Set> sets = attributeSetMap.Values.Distinct().OrderBy(s => s.Id).ToList();

			// =========================================================================================

			// Output file name
			string outputFileFullPath = Path.GetTempFileName(); // TODO: allow output file path to be configurable
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
					initialConditionCount: 0, /**/
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
						input.AddElement("beam3d", element.Id).WithNodes(nodeIds); //.WithParameters("zaxis 3 0 0 1"); // TODO: grab zAxis parameter from attributes, apply this if none is found
						break;
					case CellType.TriangleLinear:
						Debug.Assert(nodeIds.Length == 3);
						input.AddElement("mitc4shell", element.Id).WithNodes(nodeIds[0], nodeIds[1], nodeIds[2], nodeIds[2]); // last node is doubled
						break;
					case CellType.QuadLinear:
						Debug.Assert(nodeIds.Length == 4);
						input.AddElement("mitc4shell", element.Id).WithNodes(nodeIds);
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
					 .WithParameters(crossSection.Parameters)
					 .HasMaterial(materialId: attributeIdToMaterialIdMap[childAttributeId])
					 .AppliesToSet(attributeSetMap[crossSection].Id);
			}

			addDebugComment(input, "MATERIALS");
			foreach (var material in materials)
			{
				input.AddMaterial(material.Name, id: attributeIdToMaterialIdMap[material.Id]).WithParameters(material.Parameters);
			}

			addDebugComment(input, "BOUNDARY CONDITIONS");
			for (int i = 0; i < boundaryConditions.Count; i++) // write Boundary Conditions (including Loads)
			{
				var bc = boundaryConditions[i];
				// TODO: handle case when bc.TimeFunctionId is null (TimeFunction is not assigned)
				input.AddBoundaryCondition(bc.Name, id: i + 1).InTime(bc.TimeFunctionId.Value).WithParameters(bc.Parameters).AppliesToSet(attributeSetMap[bc].Id);
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
				input.AddSet(set.Id).WithNodes(set.Nodes).WithElements(set.Elements).WithElementEdges(set.ElementEdges);
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

		private Dictionary<ModelAttribute, Set> createSetMapForAttributes(IEnumerable<ModelAttribute> crossSections, IEnumerable<ModelAttribute> boundaryConditions)
		{
			// TODO: group queries by attribute ids - avoid foreach loops

			var map = new Dictionary<ModelAttribute, Set>();
			int setId = 1;
			foreach (var csAttribute in crossSections)
			{
				var elements1dQuery = from curveAttribute in dataContext.Set<CurveAttribute>()
									  where curveAttribute.ModelId == csAttribute.ModelId
									  where curveAttribute.AttributeId == csAttribute.Id
									  from macroCurve in curveAttribute.Macro.MacroCurves
									  where macroCurve.CurveId == curveAttribute.CurveId
									  from curveElement in macroCurve.Curve.CurveElements
									  select curveElement.ElementId;
				var elements2dQuery = from surfaceAttribute in dataContext.Set<SurfaceAttribute>()
									  where surfaceAttribute.ModelId == csAttribute.ModelId
									  where surfaceAttribute.AttributeId == csAttribute.Id
									  from macroSurface in surfaceAttribute.Macro.MacroSurfaces
									  where macroSurface.SurfaceId == surfaceAttribute.SurfaceId
									  from surfaceElement in macroSurface.Surface.SurfaceElements
									  select surfaceElement.ElementId;
				var elements3dQuery = from volumeAttribute in dataContext.Set<VolumeAttribute>()
									  where volumeAttribute.ModelId == csAttribute.ModelId
									  where volumeAttribute.AttributeId == csAttribute.Id
									  from volumeElement in volumeAttribute.Volume.VolumeElements
									  select volumeElement.ElementId;
				var elementIds = elements1dQuery.Concat(elements2dQuery).Concat(elements3dQuery).OrderBy(id => id).ToArray();
				var set = new Set(setId++).WithElements(elementIds);

				map.Add(csAttribute, set);
			}

			foreach (var bcAttribute in boundaryConditions)
			{
				var vertexQuery = from vertexAttribute in dataContext.Set<VertexAttribute>()
								  where vertexAttribute.ModelId == bcAttribute.ModelId
								  where vertexAttribute.AttributeId == bcAttribute.Id
								  from vertexNode in vertexAttribute.Vertex.VertexNodes
								  orderby vertexNode.NodeId
								  select vertexNode.NodeId;
				var elementEdgeQuery = from curveAttribute in dataContext.Set<CurveAttribute>()
									   where curveAttribute.ModelId == bcAttribute.ModelId
									   where curveAttribute.AttributeId == bcAttribute.Id
									   from macroCurve in curveAttribute.Macro.MacroCurves
									   where macroCurve.CurveId == curveAttribute.CurveId
									   from curveElement in macroCurve.Curve.CurveElements
									   orderby curveElement.ElementId, curveElement.Rank
									   select new KeyValuePair<int, short>(curveElement.ElementId, /*EdgeId:*/ curveElement.Rank);
				var elementSurfaceQuery = from surfaceAttribute in dataContext.Set<SurfaceAttribute>()
										  where surfaceAttribute.ModelId == bcAttribute.ModelId
										  where surfaceAttribute.AttributeId == bcAttribute.Id
										  from macroSurface in surfaceAttribute.Macro.MacroSurfaces
										  where macroSurface.SurfaceId == surfaceAttribute.SurfaceId
										  from surfaceElement in macroSurface.Surface.SurfaceElements
										  orderby surfaceElement.ElementId, surfaceElement.Rank
										  select new KeyValuePair<int, short>(surfaceElement.ElementId, /*SurfaceId:*/ surfaceElement.Rank);
				var elementVolumeQuery = from volumeAttribute in dataContext.Set<VolumeAttribute>()
										 where volumeAttribute.ModelId == bcAttribute.ModelId
										 where volumeAttribute.AttributeId == bcAttribute.Id
										 from volumeElement in volumeAttribute.Volume.VolumeElements
										 orderby volumeElement.ElementId
										 select volumeElement.ElementId;
				var set = new Set(setId++)
					.WithNodes(vertexQuery.ToArray())
					.WithElements(elementVolumeQuery.ToArray())
					.WithElementEdges(elementEdgeQuery.ToArray());

				map.Add(bcAttribute, set);
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
