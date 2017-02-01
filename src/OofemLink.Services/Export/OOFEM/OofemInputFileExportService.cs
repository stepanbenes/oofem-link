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
using OofemLink.Common.MathPhys;
using OofemLink.Services.DataAccess;
using OofemLink.Data.DataTransferObjects;

namespace OofemLink.Services.Export.OOFEM
{
	class OofemInputFileExportService : IExportService
	{
		#region Fields, constructor

		readonly string inputFileFullPath, outputFileFullPath;
		readonly DataContext dataContext;
		readonly ISimulationService simulationService;
		readonly IModelService modelService;

		public OofemInputFileExportService(DataContext dataContext, ISimulationService simulationService, IModelService modelService, string inputFileFullPath, string outputFileDirectory = null)
		{
			this.dataContext = dataContext;
			this.simulationService = simulationService;
			this.modelService = modelService;
			Debug.Assert(!string.IsNullOrEmpty(inputFileFullPath));
			this.inputFileFullPath = inputFileFullPath;
			if (string.IsNullOrEmpty(outputFileDirectory))
				outputFileDirectory = Path.GetDirectoryName(inputFileFullPath);
			this.outputFileFullPath = Path.Combine(outputFileDirectory, Path.ChangeExtension(Path.GetFileName(inputFileFullPath), "out"));
		}

		#endregion

		#region Public methods

		public async Task ExportSimulationAsync(int simulationId)
		{
			// TODO: refactor this method to DataAccess.ModelService class, avoid DataContext dependency

			// load simulation from db
			var simulation = await simulationService.GetOneAsync(simulationId);

			if (simulation == null)
				throw new KeyNotFoundException($"Simulation with id {simulationId} was not found.");
			if (simulation.ModelId == null)
				throw new InvalidDataException($"Simulation {simulationId} does not contain any model.");

			int modelId = simulation.ModelId.Value;

			// load all meshes related to this simulation
			var modelMeshes = await modelService.GetAllMeshesAsync(modelId);

			if (modelMeshes.Count == 0)
				throw new InvalidDataException($"No mesh found for model {modelId}.");
			if (modelMeshes.Count > 1)
				throw new NotSupportedException($"Multiple meshes for single model are not yet supported (model {modelId}).");

			var mesh = modelMeshes.Single();

			var crossSectionAttributes = await modelService.GetAllAttributesAsync(modelId, query => query.Where(a => a.Type == AttributeType.CrossSection));
			var materialAttributes = await modelService.GetAllAttributesAsync(modelId, query => query.Where(a => a.Type == AttributeType.Material));
			var boundaryConditionAttributes = await modelService.GetAllAttributesAsync(modelId, query => query.Where(a => a.Type == AttributeType.BoundaryCondition));
			var hingeAttributes = await modelService.GetAllAttributesAsync(modelId, query => query.Where(a => a.Type == AttributeType.Hinge));

			Dictionary<KeyValuePair<int, short>, ModelAttribute> edgeLcsAttributeMap;
			List<ModelAttribute> independentSpringAttributes;
			Dictionary<int, List<ModelAttribute>> hingeSpringAttributeMap;

			// load all model entities from db
			{
				var lcsQuery = from curveAttribute in dataContext.Set<CurveAttribute>()
							   where curveAttribute.ModelId == modelId
							   where curveAttribute.Attribute.Type == AttributeType.LocalCoordinateSystem
							   from curveElement in curveAttribute.Curve.CurveElements
							   where curveElement.MeshId == mesh.Id
							   group curveAttribute.Attribute by new KeyValuePair<int, short>(curveElement.ElementId, curveElement.Rank);
				var independentSpringsQuery = from attribute in dataContext.Attributes
											  where attribute.ModelId == modelId
											  where attribute.Type == AttributeType.Spring
											  where !attribute.ParentAttributes.Any()
											  orderby attribute.Id
											  select attribute;
				var hingeStringQuery = from attribute in dataContext.Attributes
									   where attribute.ModelId == modelId
									   where attribute.Type == AttributeType.Hinge
									   from attributeComposition in attribute.ChildAttributes
									   where attributeComposition.ChildAttribute.Type == AttributeType.Spring
									   group attributeComposition.ChildAttribute by attribute.Id;

				// materialize queries
				edgeLcsAttributeMap = lcsQuery.ToDictionary(g => g.Key, g => g.Single()); // TODO: handle multiple LCS attributes per edge
				independentSpringAttributes = independentSpringsQuery.ToList();
				hingeSpringAttributeMap = hingeStringQuery.ToDictionary(g => g.Key, g => g.ToList());
			}

			Dictionary<int, Set> attributeIdSetMap = createSetMapForModelAttributes(modelId, mesh.Id);

			List<int> elementsWithDummyCS = new List<int>();

			// =========================================================================================

			var input = new InputBuilder();

			// HEADER >>>

			// OUTPUT FILE NAME
			input.AddOutputFileRecord(new OutputFileRecord(outputFileFullPath));
			// DESCRIPTION
			input.AddDescriptionRecord(new DescriptionRecord($"Project: {simulation.ProjectName}, Task: {simulation.TaskName}"));

			// domain specify degrees of freedom, but it is not used anymore and will be removed in near future, it remains here just for backward compatibility
			input.AddDomainRecord(new DomainRecord("3dshell")); // TODO: avoid hard-coded string

			// default outputmanager giving outfile, in this case beam3d.out, only specific elements or time steps can be exported, here we export all of them
			input.AddOutputManagerRecord(new OutputManagerRecord());

			// NODES
			foreach (var node in mesh.Nodes)
			{
				input.AddDofManagerRecord(new NodeRecord(node.Id, node.X, node.Y, node.Z));
			}

			// ELEMENTS
			foreach (var element in mesh.Elements)
			{
				ElementRecord elementRecord;
				switch (element.Type)
				{
					case CellType.LineLinear:
						{
							Debug.Assert(element.NodeIds.Count == 2);
							ModelAttribute lcsAttribute;
							string lcsParameter;
							var edge = new KeyValuePair<int, short>(element.Id, 1); // there is only one edge for 1D element (has rank 1)
							if (edgeLcsAttributeMap.TryGetValue(edge, out lcsAttribute))
								lcsParameter = $"{lcsAttribute.Name} {lcsAttribute.Parameters}";
							else
								lcsParameter = getDefaultZAxisParameterBeam3d(simulation.DimensionFlags);
							elementRecord = new ElementRecord(ElementNames.beam3d, element.Id, element.Type, element.NodeIds, lcsParameter);
						}
						break;
					case CellType.TriangleLinear:
						{
							Debug.Assert(element.NodeIds.Count == 3);
							elementRecord = new ElementRecord(ElementNames.mitc4shell, element.Id, element.Type, nodeIds: new[] { element.NodeIds[0], element.NodeIds[1], element.NodeIds[2], element.NodeIds[2] }); // last node is doubled
						}
						break;
					case CellType.QuadLinear:
						{
							Debug.Assert(element.NodeIds.Count == 4);
							elementRecord = new ElementRecord(ElementNames.mitc4shell, element.Id, element.Type, element.NodeIds);
						}
						break;
					default:
						throw new NotSupportedException($"Element type {element.Type} is not supported.");
				}
				input.AddElementRecord(elementRecord);
			}

			// SPRINGS
			foreach (var spring in independentSpringAttributes)
			{
				Set set = attributeIdSetMap[spring.Id];
				foreach (var nodeId in set.Nodes)
				{
					var springElementRecord = new ElementRecord(spring.Name, id: input.MaxElementId + 1, type: CellType.Point, nodeIds: new[] { nodeId }, parameters: spring.Parameters);
					input.AddElementRecord(springElementRecord);
					elementsWithDummyCS.Add(springElementRecord.Id);
				}
				foreach (var edge in set.ElementEdges)
				{
					int elementId = edge.Key;
					short edgeRank = edge.Value;
					ElementRecord parentElementRecord = input.ElementRecords[elementId];
					int node1Id, node2Id;
					getNodesOfEdge(parentElementRecord, edgeRank, out node1Id, out node2Id);
					var springElementRecord = new ElementRecord(spring.Name, id: input.MaxElementId + 1, type: CellType.LineLinear, nodeIds: new[] { node1Id, node2Id }, parameters: spring.Parameters);
					input.AddElementRecord(springElementRecord);
					elementsWithDummyCS.Add(springElementRecord.Id);
				}
			}

			// HINGES
			foreach (var hinge in hingeAttributes)
			{
				var set = attributeIdSetMap[hinge.Id];
				var masterNodeRecord = input.DofManagerRecords[set.Nodes.Single()];
				var slaveNodeRecord = new RigidArmNodeRecord(input.MaxDofManagerId + 1, masterNodeRecord.X, masterNodeRecord.Y, masterNodeRecord.Z, masterNodeRecord.Id, hinge.Parameters);
				input.AddDofManagerRecord(slaveNodeRecord);

				var beamElementRecord = input.ElementRecords[set.Elements.Single()];
				var updatedElementRecord = beamElementRecord.WithReplacedNode(oldNodeId: masterNodeRecord.Id, newNodeId: slaveNodeRecord.Id);
				input.UpdateElementRecord(updatedElementRecord);

				// hinge springs
				List<ModelAttribute> hingeSprings;
				if (hingeSpringAttributeMap.TryGetValue(hinge.Id, out hingeSprings))
				{
					foreach (var spring in hingeSprings)
					{
						var springElementRecord = new ElementRecord(spring.Name, id: input.MaxElementId + 1, type: CellType.LineLinear, nodeIds: new[] { masterNodeRecord.Id, slaveNodeRecord.Id }, parameters: spring.Parameters);
						input.AddElementRecord(springElementRecord);
						elementsWithDummyCS.Add(springElementRecord.Id);
					}
				}
			}

			// MATERIALS
			foreach (var materialAttribute in materialAttributes)
			{
				input.AddMaterialRecord(new MaterialRecord(materialAttribute.Name, id: materialAttribute.Id, parameters: materialAttribute.Parameters));
			}

			// CROSS-SECTIONS
			for (int i = 0; i < crossSectionAttributes.Count; i++)
			{
				var crossSectionAttribute = crossSectionAttributes[i];
				int materialId = crossSectionAttribute.ChildAttributeIds.Single(); // TODO: handle cases with non-single referenced materials
				var setRecord = new SetRecord(attributeIdSetMap[crossSectionAttribute.Id]);
				input.AddSetRecord(setRecord);
				input.AddCrossSectionRecord(new CrossSectionRecord(crossSectionAttribute.Name, id: i + 1, parameters: crossSectionAttribute.Parameters, material: input.MaterialRecords[materialId], set: setRecord));
			}

			// BOUNDARY CONDITIONS
			{
				var timeFunctionIdToRecordMap = new Dictionary<int, TimeFunctionRecord>();
				foreach (var bcAttribute in boundaryConditionAttributes) // write Boundary Conditions (including Loads)
				{
					TimeFunctionRecord timeFunctionRecord;
					if (!timeFunctionIdToRecordMap.TryGetValue(bcAttribute.TimeFunctionId, out timeFunctionRecord)) // TODO: extract this caching to local function when C# 7 is available
					{
						TimeFunctionDto timeFunction = await modelService.GetTimeFunctionAsync(modelId, bcAttribute.TimeFunctionId);
						timeFunctionRecord = createTimeFunctionRecord(timeFunction, simulation.TimeSteps);

						input.AddTimeFunctionRecord(timeFunctionRecord);
						timeFunctionIdToRecordMap.Add(bcAttribute.TimeFunctionId, timeFunctionRecord);
					}
					var setRecord = new SetRecord(attributeIdSetMap[bcAttribute.Id]);
					input.AddSetRecord(setRecord);
					input.AddBoundaryConditionRecord(new BoundaryConditionRecord(bcAttribute.Name, id: bcAttribute.Id, parameters: bcAttribute.Parameters, timeFunction: timeFunctionRecord, set: setRecord));
				}
			}

			// append dummy cross-section and material if needed
			if (elementsWithDummyCS.Count > 0)
			{
				var set = new Set().WithElements(elementsWithDummyCS.ToArray());
				var setRecord = new SetRecord(set);
				var dummyMaterialRecord = createDummyMaterialRecord();
				var dummyCrossSectionRecord = createDummyCrossSectionRecord(dummyMaterialRecord, setRecord);

				input.AddCrossSectionRecord(dummyCrossSectionRecord);
				input.AddMaterialRecord(dummyMaterialRecord);
				input.AddSetRecord(setRecord);

				// Uncomment following to append "mat X crossSect Y" to Spring elements' parameters
				//foreach (int elementId in elementsWithDummyCS)
				//{
				//	var elementRecord = input.ElementRecords[elementId];
				//	if (elementRecord.Name == ElementNames.Spring)
				//	{
				//		var updatedElementRecord = elementRecord.WithAppendedParameters($"mat {dummyMaterialRecord.Id} crossSect {dummyCrossSectionRecord.Id}");
				//		input.AddOrUpdateElementRecord(updatedElementRecord);
				//	}
				//}
			}

			// add boundary condition for mitc4shell element nodes (rotation in normal direction must be fixed)
			{
				// NOTE: very Very VERY ugly code; does not work for elements having normal that does not align with GCS
				// TODO: rethink and rewrite using e.g. lcs on nodes

				HashSet<int> nodesThatNeedToBeFixed = new HashSet<int>();
				foreach (var mitc4shellElementRecords in input.ElementRecords.Values.Where(e => e.Name == ElementNames.mitc4shell))
				{
					foreach (int nodeId in mitc4shellElementRecords.NodeIds)
					{
						nodesThatNeedToBeFixed.Add(nodeId);
					}
				}
				var nodeRecordsThatNeedToBeFixed = nodesThatNeedToBeFixed.Select(id => input.DofManagerRecords[id]).ToList();
				if (nodeRecordsThatNeedToBeFixed.Count > 0)
				{
					int dofId;
					var first = nodeRecordsThatNeedToBeFixed[0];
					if (nodeRecordsThatNeedToBeFixed.All(n => n.X == first.X))      // this is ugly, I know
						dofId = 4; // rotation X
					else if (nodeRecordsThatNeedToBeFixed.All(n => n.Y == first.Y)) // don't look please
						dofId = 5; // rotation Y
					else if (nodeRecordsThatNeedToBeFixed.All(n => n.Z == first.Z)) // I am very sorry :(
						dofId = 6; // rotation Z
					else                                                            // get rid of the misery
						throw new NotSupportedException("Elements having normal that does not align with GCS are not currently supported");
					/* 
						yay! ✿ (◠‿◠) ♥
					*/
					var setRecord = new SetRecord(new Set().WithNodes(nodesThatNeedToBeFixed.OrderBy(id => id).ToArray()));
					var timeFunctionRecord = new TimeFunctionRecord(TimeFunctionNames.ConstantFunction, id: 0, value: 1);
					string parameters = $"dofs 1 {dofId} values 1 0";
					var boundaryConditionRecord = new BoundaryConditionRecord(BoundaryConditionNames.BoundaryCondition, 0, parameters, timeFunctionRecord, setRecord);

					input.AddBoundaryConditionRecord(boundaryConditionRecord);
					input.AddTimeFunctionRecord(timeFunctionRecord);
					input.AddSetRecord(setRecord);
				}
			}

			// attach quad1platesubsoil elements to elements having WinklerPasternak material assigned to them
			{
				foreach (var crossSectionRecord in from materialRecord in input.MaterialRecords.Values
												   where materialRecord.Name == MaterialNames.WinklerPasternak
												   join crossSection in input.CrossSectionRecords.Values on materialRecord.Id equals crossSection.Material.Id
												   select crossSection)
				{
					var setRecord = crossSectionRecord.Set;
					var soilElementIds = new List<int>();
					foreach (var elementRecord in from elementId in setRecord.Set.Elements
												  select input.ElementRecords[elementId])
					{
						var soilElementRecord = new ElementRecord(ElementNames.quad1platesubsoil, input.MaxElementId + 1, elementRecord.Type, elementRecord.NodeIds);
						input.AddElementRecord(soilElementRecord);
						soilElementIds.Add(soilElementRecord.Id);
					}
					var newSetRecord = new SetRecord(new Set().WithElements(soilElementIds.ToArray()));
					input.AddSetRecord(newSetRecord);
					var updatedCrossSectionRecord = crossSectionRecord.WithSet(newSetRecord);
					input.UpdateCrossSectionRecord(updatedCrossSectionRecord);
				}
			}

			// split beams under partially applied loads
			{
				var partiallyAppliedLoadsQuery = from attribute in dataContext.Attributes
												 where attribute.ModelId == modelId
												 where attribute.Type == AttributeType.BoundaryCondition
												 from curveAttribute in attribute.CurveAttributes
												 where (curveAttribute.RelativeStart != null && curveAttribute.RelativeStart > 0) || (curveAttribute.RelativeEnd != null && curveAttribute.RelativeEnd < 1)
												 from curveElement in curveAttribute.Curve.CurveElements
												 select new { AttributeId = attribute.Id, curveElement.ElementId, curveAttribute.RelativeStart, curveAttribute.RelativeEnd };
				foreach (var partiallyAppliedLoad in partiallyAppliedLoadsQuery)
				{
					int boundaryConditionId = partiallyAppliedLoad.AttributeId;

					if (partiallyAppliedLoad.RelativeStart.HasValue && partiallyAppliedLoad.RelativeStart.Value > 0)
					{
						int secondElementId = splitBeamElement(input, partiallyAppliedLoad.ElementId, partiallyAppliedLoad.RelativeStart.Value);
						copyAllAttributesFromElementToElement(input, partiallyAppliedLoad.ElementId, secondElementId);
						removeBoundaryConditionFromElement(input, boundaryConditionId, partiallyAppliedLoad.ElementId);

						if (partiallyAppliedLoad.RelativeEnd.HasValue && partiallyAppliedLoad.RelativeEnd.Value < 1)
						{
							double relativePosition = (partiallyAppliedLoad.RelativeEnd.Value - partiallyAppliedLoad.RelativeStart.Value) / (1.0 - partiallyAppliedLoad.RelativeStart.Value);
							int thirdElementId = splitBeamElement(input, secondElementId, relativePosition);
							copyAllAttributesFromElementToElement(input, secondElementId, thirdElementId);
							removeBoundaryConditionFromElement(input, boundaryConditionId, thirdElementId);
						}
					}
					else if (partiallyAppliedLoad.RelativeEnd.HasValue && partiallyAppliedLoad.RelativeEnd.Value < 1)
					{
						int secondElementId = splitBeamElement(input, partiallyAppliedLoad.ElementId, partiallyAppliedLoad.RelativeEnd.Value);
						copyAllAttributesFromElementToElement(input, partiallyAppliedLoad.ElementId, secondElementId);
						removeBoundaryConditionFromElement(input, boundaryConditionId, secondElementId);
					}
					else
						throw new InvalidOperationException();
				}
			}

			// Type of so-called engineering model, willbe the same for now, for non-linear problems we will need switch to nonlinear static. The nlstatic can have several keywords specifying solver parameters, convergence criteria and so on, nmodules = number of export modules
			input.AddEngineeringModelRecord(new EngineeringModelRecord(
					engineeringModelName: "LinearStatic", // TODO: take this from analysis parameters in Simulation object
					numberOfTimeSteps: simulation.TimeSteps.Count,
					exportModules: new[] { createVtkXmlExportModuleRecord(input) }
				));

			// create input file
			input.WriteToFile(inputFileFullPath);
		}

		#endregion

		#region Private methods

		private VtkXmlExportModuleRecord createVtkXmlExportModuleRecord(InputBuilder input)
		{
			var mitc4shellElementIds = input.ElementRecords.Values.Where(r => r.Name == ElementNames.mitc4shell).Select(r => r.Id).ToArray();
			if (mitc4shellElementIds.Length == 0)
			{
				return new VtkXmlExportModuleRecord(
					primVars: Array.Empty<int>(),
					vars: new[] { 7 },
					cellVars: Array.Empty<int>(),
					regionSets: Array.Empty<SetRecord>());
			}

			var regionSet = new Set().WithElements(mitc4shellElementIds);
			var regionSetRecord = new SetRecord(regionSet);

			input.AddSetRecord(regionSetRecord);

			return new VtkXmlExportModuleRecord(
				primVars: new[] { 1 },
				vars: Array.Empty<int>(),
				cellVars: new[] { 9, 10 },
				regionSets: new[] { regionSetRecord });
		}

		private CrossSectionRecord createDummyCrossSectionRecord(MaterialRecord materialRecord, SetRecord setRecord)
		{
			return new CrossSectionRecord(CrossSectionNames.SimpleCS, id: 0, parameters: "", material: materialRecord, set: setRecord);
		}

		private MaterialRecord createDummyMaterialRecord()
		{
			return new MaterialRecord(MaterialNames.DummyMat, id: 0, parameters: "");
		}

		private void getNodesOfEdge(ElementRecord elementRecord, short edgeRank, out int node1Id, out int node2Id)
		{
			// TODO: extract this method to ModelService, create special class EdgeDto

			switch (elementRecord.Type)
			{
				case CellType.LineLinear:
					if (edgeRank == 1)
					{
						node1Id = elementRecord.NodeIds[0];
						node2Id = elementRecord.NodeIds[1];
						return;
					}
					break;
				case CellType.TriangleLinear:
					switch (edgeRank)
					{
						case 1:
							node1Id = elementRecord.NodeIds[0];
							node2Id = elementRecord.NodeIds[1];
							return;
						case 2:
							node1Id = elementRecord.NodeIds[1];
							node2Id = elementRecord.NodeIds[2];
							return;
						case 3:
							node1Id = elementRecord.NodeIds[2];
							node2Id = elementRecord.NodeIds[0];
							return;
					}
					break;
				case CellType.QuadLinear:
					switch (edgeRank)
					{
						case 1:
							node1Id = elementRecord.NodeIds[0];
							node2Id = elementRecord.NodeIds[1];
							return;
						case 2:
							node1Id = elementRecord.NodeIds[1];
							node2Id = elementRecord.NodeIds[2];
							return;
						case 3:
							node1Id = elementRecord.NodeIds[2];
							node2Id = elementRecord.NodeIds[3];
							return;
						case 4:
							node1Id = elementRecord.NodeIds[3];
							node2Id = elementRecord.NodeIds[0];
							return;
					}
					break;
				default:
					throw new NotSupportedException($"{elementRecord.Type} element type is not supported");
			}

			throw new InvalidDataException($"Unexpected edge rank {edgeRank} for element type {elementRecord.Type}");
		}

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

		private TimeFunctionRecord createTimeFunctionRecord(TimeFunctionDto timeFunction, IReadOnlyList<TimeStepDto> timeSteps)
		{
			// TODO: replace with type switch when C# 7 is available

			var constantFunction = timeFunction as ConstantFunctionDto;
			if (constantFunction != null)
			{
				return new TimeFunctionRecord(constantFunction.Name, timeFunction.Id, value: constantFunction.ConstantValue);
			}

			var peakFunction = timeFunction as PeakFunctionDto;
			if (peakFunction != null)
			{
				var timeStep = timeSteps.Single(ts => ts.Number == peakFunction.TimeNumber);
				return new TimeFunctionRecord(peakFunction.Name, timeFunction.Id, time: timeStep.Time ?? timeStep.Number, value: peakFunction.Value);
			}

			var piecewiseLinFunction = timeFunction as PiecewiseLinFunctionDto;
			if (piecewiseLinFunction != null)
			{
				var timeValueMap = new Dictionary<int, double>();
				for (int i = 0; i < piecewiseLinFunction.TimeNumbers.Count; i++)
					timeValueMap.Add(piecewiseLinFunction.TimeNumbers[i], piecewiseLinFunction.Values[i]);
				var timeValuePairs = from timeStep in timeSteps
									 orderby timeStep.Time ?? timeStep.Number // The particular time values in t array should be sorted according to time scale
									 select new KeyValuePair<double, double>(timeStep.Time ?? timeStep.Number, timeValueMap.GetValueOrDefault(timeStep.Number));
				return new TimeFunctionRecord(timeFunction.Name, timeFunction.Id, timeValuePairs.ToList());
			}

			throw new NotSupportedException($"Load time function of type '{timeFunction.Name}' is not supported");
		}

		private Dictionary<int, Set> createSetMapForModelAttributes(int modelId, int meshId)
		{
			// TODO: [Optimization] avoid duplication of sets. If the set with same nodes, elements, etc. already exists then don't create new one

			var map = new Dictionary<int, Set>();

			// AttributeTarget.Node:
			{
				var vertexQuery = from vertexAttribute in dataContext.Set<VertexAttribute>()
								  where vertexAttribute.ModelId == modelId
								  where vertexAttribute.Attribute.Target == AttributeTarget.Node
								  from vertexNode in vertexAttribute.Vertex.VertexNodes
								  where vertexNode.MeshId == meshId
								  group vertexNode.NodeId by vertexAttribute.AttributeId;
				var curveQuery = from curveAttribute in dataContext.Set<CurveAttribute>() // some attributes assigned to curves are meant to be applied to nodes (BoundaryCondition)
								 where curveAttribute.ModelId == modelId
								 where curveAttribute.Attribute.Target == AttributeTarget.Node
								 from curveNode in curveAttribute.Curve.CurveNodes
								 where curveNode.MeshId == meshId
								 group curveNode.NodeId by curveAttribute.AttributeId;

				var query = vertexQuery.Concat(curveQuery);

				foreach (var group in query)
				{
					int attributeId = group.Key;
					Set set;
					if (!map.TryGetValue(attributeId, out set))
						set = new Set();
					map[attributeId] = set.WithNodes(set.Nodes.Concat(group).OrderBy(id => id).Distinct().ToArray());
				}
			}

			// AttributeTarget.Edge:
			{
				var query = from curveAttribute in dataContext.Set<CurveAttribute>()
							where curveAttribute.ModelId == modelId
							where curveAttribute.Attribute.Target == AttributeTarget.Edge
							from curveElement in curveAttribute.Curve.CurveElements
							where curveElement.MeshId == meshId
							orderby curveElement.ElementId, curveElement.Rank
							group new KeyValuePair<int, short>(curveElement.ElementId, /*EdgeId:*/ curveElement.Rank) by curveAttribute.AttributeId;
				foreach (var group in query)
				{
					int attributeId = group.Key;
					Set set = new Set().WithElementEdges(group.ToArray());
					map.Add(attributeId, set);
				}
			}

			// AttributeTarget.Surface:
			{
				var query = from surfaceAttribute in dataContext.Set<SurfaceAttribute>()
							where surfaceAttribute.ModelId == modelId
							where surfaceAttribute.Attribute.Target == AttributeTarget.Surface
							from surfaceElement in surfaceAttribute.Surface.SurfaceElements
							where surfaceElement.MeshId == meshId
							orderby surfaceElement.ElementId, surfaceElement.Rank
							group new KeyValuePair<int, short>(surfaceElement.ElementId, /*SurfaceId:*/ surfaceElement.Rank) by surfaceAttribute.AttributeId;
				foreach (var group in query)
				{
					int attributeId = group.Key;
					Set set = new Set().WithElementSurfaces(group.ToArray());
					map.Add(attributeId, set);
				}
			}

			// AttributeTarget.Volume:
			{
				var elements1dQuery = from curveAttribute in dataContext.Set<CurveAttribute>()
									  where curveAttribute.ModelId == modelId
									  where curveAttribute.Attribute.Target == AttributeTarget.Volume
									  from curveElement in curveAttribute.Curve.CurveElements
									  where curveElement.MeshId == meshId
									  group curveElement.ElementId by curveAttribute.AttributeId;
				var elements2dQuery = from surfaceAttribute in dataContext.Set<SurfaceAttribute>()
									  where surfaceAttribute.ModelId == modelId
									  where surfaceAttribute.Attribute.Target == AttributeTarget.Volume
									  from surfaceElement in surfaceAttribute.Surface.SurfaceElements
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
					Set set;
					if (!map.TryGetValue(attributeId, out set))
						set = new Set();
					map[attributeId] = set.WithElements(set.Elements.Concat(group).OrderBy(id => id).Distinct().ToArray());
				}
			}

			// AttributeTarget.Undefined:
			{
				var nodesQuery = from vertexAttribute in dataContext.Set<VertexAttribute>()
								 where vertexAttribute.ModelId == modelId
								 where vertexAttribute.Attribute.Target == AttributeTarget.Undefined
								 from vertexNode in vertexAttribute.Vertex.VertexNodes
								 where vertexNode.MeshId == meshId
								 group vertexNode.NodeId by vertexAttribute.AttributeId;
				var elements1dQuery = from curveAttribute in dataContext.Set<CurveAttribute>()
									  where curveAttribute.ModelId == modelId
									  where curveAttribute.Attribute.Target == AttributeTarget.Undefined
									  from curveElement in curveAttribute.Curve.CurveElements
									  where curveElement.MeshId == meshId
									  group curveElement.ElementId by curveAttribute.AttributeId;
				var elements2dQuery = from surfaceAttribute in dataContext.Set<SurfaceAttribute>()
									  where surfaceAttribute.ModelId == modelId
									  where surfaceAttribute.Attribute.Target == AttributeTarget.Undefined
									  from surfaceElement in surfaceAttribute.Surface.SurfaceElements
									  where surfaceElement.MeshId == meshId
									  group surfaceElement.ElementId by surfaceAttribute.AttributeId;
				var elements3dQuery = from volumeAttribute in dataContext.Set<VolumeAttribute>()
									  where volumeAttribute.ModelId == modelId
									  where volumeAttribute.Attribute.Target == AttributeTarget.Undefined
									  from volumeElement in volumeAttribute.Volume.VolumeElements
									  where volumeElement.MeshId == meshId
									  group volumeElement.ElementId by volumeAttribute.AttributeId;


				foreach (var group in nodesQuery)
				{
					int attributeId = group.Key;
					Set set;
					if (!map.TryGetValue(attributeId, out set))
						set = new Set();
					map[attributeId] = set.WithNodes(set.Nodes.Concat(group).OrderBy(id => id).Distinct().ToArray());
				}

				var elementsQuery = elements1dQuery.Concat(elements2dQuery).Concat(elements3dQuery);

				foreach (var group in elementsQuery)
				{
					int attributeId = group.Key;
					Set set;
					if (!map.TryGetValue(attributeId, out set))
						set = new Set();
					map[attributeId] = set.WithElements(set.Elements.Concat(group).OrderBy(id => id).Distinct().ToArray());
				}
			}

			return map;
		}

		private int splitBeamElement(InputBuilder input, int elementId, double relativePosition)
		{
			if (relativePosition <= 0 || relativePosition >= 1)
				throw new ArgumentOutOfRangeException(nameof(relativePosition), $"Argument is expected to be in range (0, 1), but has value {relativePosition}");

			ElementRecord elementRecord = input.ElementRecords[elementId];

			if (elementRecord.Name != ElementNames.beam3d)
				throw new InvalidOperationException($"Element is expected to be of type {ElementNames.beam3d}");

			DofManagerRecord node1 = input.DofManagerRecords[elementRecord.NodeIds[0]];
			DofManagerRecord node2 = input.DofManagerRecords[elementRecord.NodeIds[1]];

			Vector3d point1 = new Vector3d(node1.X, node1.Y, node1.Z);
			Vector3d point2 = new Vector3d(node2.X, node2.Y, node2.Z);
			Vector3d direction = point2 - point1;
			Vector3d splitPoint = point1 + direction * relativePosition;

			NodeRecord splitNodeRecord = new NodeRecord(input.MaxDofManagerId + 1, splitPoint.X, splitPoint.Y, splitPoint.Z);
			input.AddDofManagerRecord(splitNodeRecord);

			ElementRecord updatedElementRecord = elementRecord.WithReplacedNode(node2.Id, splitNodeRecord.Id);
			ElementRecord newElementRecord = new ElementRecord(elementRecord.Name, input.MaxElementId + 1, elementRecord.Type, new[] { splitNodeRecord.Id, node2.Id }, elementRecord.Parameters);
			input.UpdateElementRecord(updatedElementRecord);
			input.AddElementRecord(newElementRecord);

			return newElementRecord.Id;
		}

		private void copyAllAttributesFromElementToElement(InputBuilder input, int sourceElementId, int targetElementId)
		{
			foreach (var setRecord in input.SetRecords.ToList())
			{
				if (setRecord.Set.Elements.Contains(sourceElementId))
				{
					int[] newElementSet = setRecord.Set.Elements.AppendItem(targetElementId).OrderBy(id => id).Distinct().ToArray();
					setRecord.Set = setRecord.Set.WithElements(newElementSet); // update set record
				}

				if (setRecord.Set.ElementEdges.Any(edge => edge.Key == sourceElementId))
				{
					List<KeyValuePair<int, short>> newEdgeSet = new List<KeyValuePair<int, short>>();
					foreach (var edge in setRecord.Set.ElementEdges)
					{
						newEdgeSet.Add(edge);
						if (edge.Key == sourceElementId)
						{
							newEdgeSet.Add(new KeyValuePair<int, short>(targetElementId, edge.Value)); // TODO: why the rank should be the same?
						}
					}
					setRecord.Set = setRecord.Set.WithElementEdges(newEdgeSet.ToArray());
				}

				if (setRecord.Set.ElementSurfaces.Any(surface => surface.Key == sourceElementId))
				{
					List<KeyValuePair<int, short>> newSurfaceSet = new List<KeyValuePair<int, short>>();
					foreach (var surface in setRecord.Set.ElementSurfaces)
					{
						newSurfaceSet.Add(surface);
						if (surface.Key == sourceElementId)
						{
							newSurfaceSet.Add(new KeyValuePair<int, short>(targetElementId, surface.Value)); // TODO: why the rank should be the same?
						}
					}
					setRecord.Set = setRecord.Set.WithElementSurfaces(newSurfaceSet.ToArray());
				}
			}
		}

		private void removeBoundaryConditionFromElement(InputBuilder input, int boundaryConditionId, int elementId)
		{
			// WARNING: this works if the set is applied only to this attribute!

			BoundaryConditionRecord bcRecord = input.BoundaryConditionRecords[boundaryConditionId];
			SetRecord setRecord = bcRecord.Set;
			Set set = setRecord.Set;
			Set updatedSet = set.WithElements(set.Elements.Where(id => id != elementId).ToArray()).WithElementEdges(set.ElementEdges.Where(edge => edge.Key != elementId).ToArray()).WithElementSurfaces(set.ElementSurfaces.Where(surface => surface.Key != elementId).ToArray());
			setRecord.Set = updatedSet;
		}

		#endregion
	}
}
