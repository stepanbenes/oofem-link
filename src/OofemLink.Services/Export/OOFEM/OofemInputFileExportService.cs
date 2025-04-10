﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Common.OofemNames;
using OofemLink.Common.Enumerations;
using OofemLink.Common.Extensions;
using OofemLink.Common.MathPhys;
using OofemLink.Services.DataAccess;
using OofemLink.Data.DataTransferObjects;
using OofemLink.Data.MeshEntities;
using OofemLink.Common.Diagnostics;

namespace OofemLink.Services.Export.OOFEM
{
	class OofemInputFileExportService : IExportService
	{
		#region Fields, constructor

		readonly string inputFileFullPath, outputFileFullPath;
		readonly ISimulationService simulationService;
		readonly IModelService modelService;

		public OofemInputFileExportService(ISimulationService simulationService, IModelService modelService, string inputFileFullPath, string outputFileDirectory = null)
		{
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
			ViewSimulationDto simulation;

			using (new TimeReport("SIMULATION"))
			{
				// load simulation from db
				simulation = await simulationService.GetOneAsync(simulationId);

				if (simulation == null)
					throw new KeyNotFoundException($"Simulation with id {simulationId} was not found.");
				if (simulation.ModelId == null)
					throw new InvalidDataException($"Simulation {simulationId} does not contain any model.");
			}

			int modelId = simulation.ModelId.Value;
			MeshDto mesh;

			using (new TimeReport("MESHES"))
			{
				// load all meshes related to this simulation
				var modelMeshes = await modelService.GetAllMeshesAsync(modelId);

				if (modelMeshes.Count == 0)
					throw new InvalidDataException($"No mesh found for model {modelId}.");
				if (modelMeshes.Count > 1)
					throw new NotSupportedException($"Multiple meshes for single model are not yet supported (model {modelId}).");

				mesh = modelMeshes.Single();
			}

			IReadOnlyList<AttributeDto> crossSectionAttributes, materialAttributes, boundaryConditionAttributes, hingeAttributes, lcsAttributes, independentSpringAttributes;
			IReadOnlyDictionary<int, MeshEntitySet> attributeSetMap;

			using (new TimeReport("CS QUERY"))
			{
				crossSectionAttributes = await modelService.GetAllAttributesAsync(modelId, query => query.Where(a => a.Type == AttributeType.CrossSection));
			}
			using (new TimeReport("MAT QUERY"))
			{
				materialAttributes = await modelService.GetAllAttributesAsync(modelId, query => query.Where(a => a.Type == AttributeType.Material));
			}
			using (new TimeReport("BC QUERY"))
			{
				boundaryConditionAttributes = await modelService.GetAllAttributesAsync(modelId, query => query.Where(a => a.Type == AttributeType.BoundaryCondition));
			}
			using (new TimeReport("HINGE QUERY"))
			{
				hingeAttributes = await modelService.GetAllAttributesAsync(modelId, query => query.Where(a => a.Type == AttributeType.Hinge));
			}
			using (new TimeReport("LCS QUERY"))
			{
				lcsAttributes = await modelService.GetAllAttributesAsync(modelId, query => query.Where(a => a.Type == AttributeType.LocalCoordinateSystem));
			}
			using (new TimeReport("SPRING QUERY"))
			{
				independentSpringAttributes = await modelService.GetAllAttributesAsync(modelId, query => query.Where(a => a.Type == AttributeType.Spring && !a.HasParentAttributes));
			}
			using (new TimeReport("ATTRIBUTE SET MAP"))
			{
				attributeSetMap = await modelService.GetAttributeSetMapAsync(modelId, mesh.Id);
			}

			List<int> elementsWithDummyCS = new List<int>();

			// =========================================================================================

			var input = new InputBuilder();

			// HEADER >>>
			using (new TimeReport("HEADER"))
			{
				// OUTPUT FILE NAME
				input.AddOutputFileRecord(new OutputFileRecord(outputFileFullPath));
				// DESCRIPTION
				input.AddDescriptionRecord(new DescriptionRecord($"Project: {simulation.ProjectName}, Task: {simulation.TaskName}"));

				// domain specify degrees of freedom, but it is not used anymore and will be removed in near future, it remains here just for backward compatibility
				input.AddDomainRecord(new DomainRecord("3dshell")); // TODO: avoid hard-coded string

				// default outputmanager giving outfile, in this case beam3d.out, only specific elements or time steps can be exported, here we export all of them
				input.AddOutputManagerRecord(new OutputManagerRecord());
			}

			using (new TimeReport("NODES"))
			{
				// NODES
				foreach (var node in mesh.Nodes)
				{
					input.AddDofManagerRecord(new NodeRecord(node.Id, node.X, node.Y, node.Z));
				}
			}

			string defaultZAxisParameter = getDefaultZAxisParameterBeam3d(simulation.DimensionFlags);

			using (new TimeReport("ELEMENTS"))
			{
				// ELEMENTS
				foreach (var element in mesh.Elements)
				{
					ElementRecord elementRecord;
					switch (element.Type)
					{
						case CellType.LineLinear:
							{
								Debug.Assert(element.NodeIds.Count == 2);
								elementRecord = new ElementRecord(ElementNames.beam3d, element.Id, element.Type, element.NodeIds, defaultZAxisParameter);
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
			}

			using (new TimeReport("LCS"))
			{
				// LOCAL COORDINATE SYSTEMS
				foreach (var lcsAttribute in lcsAttributes)
				{
					MeshEntitySet set;
					if (attributeSetMap.TryGetValue(lcsAttribute.Id, out set))
					{
						Debug.Assert(set.Nodes.Count == 0);
						Debug.Assert(set.ElementEdges.Count == 0);
						Debug.Assert(set.ElementSurfaces.Count == 0);
						Debug.Assert(set.Elements.Count > 0);
						string lcsParameter = $"{lcsAttribute.Name} {lcsAttribute.Parameters}";
						foreach (int elementId in set.Elements)
						{
							var elementRecord = input.ElementRecords[elementId];
							elementRecord.Parameters = lcsParameter;
						}
					}
				}
			}

			using (new TimeReport("SPRINGS"))
			{
				// SPRINGS
				foreach (var spring in independentSpringAttributes)
				{
					MeshEntitySet set = attributeSetMap[spring.Id];
					foreach (var nodeId in set.Nodes)
					{
						var springElementRecord = new ElementRecord(spring.Name, id: input.MaxElementId + 1, type: CellType.Point, nodeIds: new[] { nodeId }, parameters: spring.Parameters);
						input.AddElementRecord(springElementRecord);
						elementsWithDummyCS.Add(springElementRecord.Id);
					}
					foreach (var edge in set.ElementEdges)
					{
						ElementRecord parentElementRecord = input.ElementRecords[edge.ElementId];
						int node1Id, node2Id;
						getNodesOfEdge(parentElementRecord, edge.EdgeRank, out node1Id, out node2Id);
						var springElementRecord = new ElementRecord(spring.Name, id: input.MaxElementId + 1, type: CellType.LineLinear, nodeIds: new[] { node1Id, node2Id }, parameters: spring.Parameters);
						input.AddElementRecord(springElementRecord);
						elementsWithDummyCS.Add(springElementRecord.Id);
					}
				}
			}

			using (new TimeReport("HINGES"))
			{
				// HINGES
				foreach (var hinge in hingeAttributes)
				{
					var set = attributeSetMap[hinge.Id];
					var masterNodeRecord = input.DofManagerRecords[set.Nodes.Single()];
					var slaveNodeRecord = new RigidArmNodeRecord(input.MaxDofManagerId + 1, masterNodeRecord.X, masterNodeRecord.Y, masterNodeRecord.Z, masterNodeRecord.Id, hinge.Parameters);
					input.AddDofManagerRecord(slaveNodeRecord);

					var beamElementRecord = input.ElementRecords[set.Elements.Single()];
					beamElementRecord.ReplaceNode(oldNodeId: masterNodeRecord.Id, newNodeId: slaveNodeRecord.Id);

					// hinge springs
					foreach (var childAttribute in hinge.ChildAttributes)
					{
						if (childAttribute.Type == AttributeType.Spring)
						{
							var springElementRecord = new ElementRecord(childAttribute.Name, id: input.MaxElementId + 1, type: CellType.LineLinear, nodeIds: new[] { masterNodeRecord.Id, slaveNodeRecord.Id }, parameters: childAttribute.Parameters);
							input.AddElementRecord(springElementRecord);
							elementsWithDummyCS.Add(springElementRecord.Id);
						}
					}
				}
			}

			using (new TimeReport("MATERIALS"))
			{
				// MATERIALS
				foreach (var materialAttribute in materialAttributes)
				{
					input.AddMaterialRecord(new MaterialRecord(materialAttribute.Name, id: materialAttribute.Id, parameters: materialAttribute.Parameters));
				}
			}

			using (new TimeReport("CROSS-SECTIONS"))
			{
				// CROSS-SECTIONS
				for (int i = 0; i < crossSectionAttributes.Count; i++)
				{
					var crossSectionAttribute = crossSectionAttributes[i];
					var materialAttribute = crossSectionAttribute.ChildAttributes.Single(a => a.Type == AttributeType.Material); // TODO: handle cases with non-single referenced materials
					var setRecord = new SetRecord(attributeSetMap[crossSectionAttribute.Id]);
					input.AddSetRecord(setRecord);
					input.AddCrossSectionRecord(new CrossSectionRecord(crossSectionAttribute.Name, id: i + 1, parameters: crossSectionAttribute.Parameters, material: input.MaterialRecords[materialAttribute.Id], set: setRecord));
				}
			}

			using (new TimeReport("BC"))
			{
				// BOUNDARY CONDITIONS
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
					MeshEntitySet set;
					if (!attributeSetMap.TryGetValue(bcAttribute.Id, out set))
						set = MeshEntitySet.Empty;
					var setRecord = new SetRecord(set);
					input.AddSetRecord(setRecord);
					input.AddBoundaryConditionRecord(new BoundaryConditionRecord(bcAttribute.Name, id: bcAttribute.Id, parameters: bcAttribute.Parameters, timeFunction: timeFunctionRecord, set: setRecord));
				}
			}

			using (new TimeReport("DUMMY"))
			{
				// append dummy cross-section and material if needed
				if (elementsWithDummyCS.Count > 0)
				{
					var set = MeshEntitySet.Empty.WithElements(elementsWithDummyCS);
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
			}

			using (new TimeReport("SUPPORTS FOR MITC"))
			{
				// add boundary condition for mitc4shell element nodes (rotation in normal direction must be fixed)

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
					var setRecord = new SetRecord(MeshEntitySet.Empty.WithNodes(nodesThatNeedToBeFixed));
					var timeFunctionRecord = new TimeFunctionRecord(TimeFunctionNames.ConstantFunction, id: 0, value: 1);
					string parameters = $"dofs 1 {dofId} values 1 0";
					var boundaryConditionRecord = new BoundaryConditionRecord(BoundaryConditionNames.BoundaryCondition, 0, parameters, timeFunctionRecord, setRecord);

					input.AddBoundaryConditionRecord(boundaryConditionRecord);
					input.AddTimeFunctionRecord(timeFunctionRecord);
					input.AddSetRecord(setRecord);
				}
			}

			using (new TimeReport("SUBSOIL"))
			{
				// attach quad1platesubsoil elements to elements having WinklerPasternak material assigned to them

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
					var newSetRecord = new SetRecord(MeshEntitySet.Empty.WithElements(soilElementIds));
					input.AddSetRecord(newSetRecord);
					crossSectionRecord.Set = newSetRecord; // replace old set with new set with newly created soil elements
				}
			}

			using (new TimeReport("PARTIAL LOAD"))
			{
				// split beams under partially applied loads

				var partiallyAppliedLoads = await modelService.GetAllPartialAttributeOnCurveApplicationsAsync(modelId, mesh.Id, AttributeType.BoundaryCondition);
				foreach (var partiallyAppliedLoad in partiallyAppliedLoads)
				{
					var boundaryConditionRecord = input.BoundaryConditionRecords[partiallyAppliedLoad.AttributeId];
					var setRecord = boundaryConditionRecord.Set;
					bool startIsLoose = partiallyAppliedLoad.RelativeStart.HasValue && partiallyAppliedLoad.RelativeStart.Value > 0;
					bool endIsLoose = partiallyAppliedLoad.RelativeEnd.HasValue && partiallyAppliedLoad.RelativeEnd.Value < 1;
					if (startIsLoose)
					{
						var relativePositions = new List<double> { partiallyAppliedLoad.RelativeStart.Value };
						if (endIsLoose)
							relativePositions.Add((partiallyAppliedLoad.RelativeEnd.Value - partiallyAppliedLoad.RelativeStart.Value) / (1.0 - partiallyAppliedLoad.RelativeStart.Value));
						var newElementIds = splitBeamElement(input, partiallyAppliedLoad.ElementId, relativePositions);
						// append edge to set record
						setRecord.Set = setRecord.Set.WithElementEdges(setRecord.Set.ElementEdges.Add(new ElementEdge(newElementIds[0], edgeRank: 1)));
					}
					else if (endIsLoose)
					{
						var ignored = splitBeamElement(input, partiallyAppliedLoad.ElementId, relativePositions: new[] { partiallyAppliedLoad.RelativeEnd.Value });
						// append edge to set record
						setRecord.Set = setRecord.Set.WithElementEdges(setRecord.Set.ElementEdges.Add(new ElementEdge(partiallyAppliedLoad.ElementId, edgeRank: 1)));
					}
					else
						throw new InvalidDataException($"Neither start nor end is not specified for partially applied load.");
				}
			}

			using (new TimeReport("ENG MODEL"))
			{
				// Type of so-called engineering model, willbe the same for now, for non-linear problems we will need switch to nonlinear static. The nlstatic can have several keywords specifying solver parameters, convergence criteria and so on, nmodules = number of export modules
				input.AddEngineeringModelRecord(new EngineeringModelRecord(
						engineeringModelName: "LinearStatic", // TODO: take this from analysis parameters in Simulation object
						numberOfTimeSteps: simulation.TimeSteps.Count,
						exportModules: new[] { createVtkXmlExportModuleRecord(input) }
					));
			}

			using (new TimeReport("GO GO, WRITE!"))
			{
				// create input file
				input.WriteToFile(inputFileFullPath);
			}
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

			var regionSet = MeshEntitySet.Empty.WithElements(mitc4shellElementIds);
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

		private List<int> splitBeamElement(InputBuilder input, int elementToSplitId, IEnumerable<double> relativePositions)
		{
			ElementRecord elementRecord = input.ElementRecords[elementToSplitId];

			if (elementRecord.Type != CellType.LineLinear)
				throw new InvalidOperationException($"Element is expected to be of type {CellType.LineLinear}");

			DofManagerRecord beginNode = input.DofManagerRecords[elementRecord.NodeIds[0]];
			DofManagerRecord endNode = input.DofManagerRecords[elementRecord.NodeIds[1]];

			Vector3d beginPoint = new Vector3d(beginNode.X, beginNode.Y, beginNode.Z);
			Vector3d endPoint = new Vector3d(endNode.X, endNode.Y, endNode.Z);
			Vector3d direction = endPoint - beginPoint;

			var splitNodeIds = new List<int>();

			foreach (double relativePosition in relativePositions)
			{
				if (relativePosition <= 0 || relativePosition >= 1)
					throw new ArgumentOutOfRangeException(nameof(relativePosition), $"Argument is expected to be in range (0, 1), but has value {relativePosition}");
				Vector3d splitPoint = beginPoint + direction * relativePosition;
				NodeRecord splitNodeRecord = new NodeRecord(input.MaxDofManagerId + 1, splitPoint.X, splitPoint.Y, splitPoint.Z);
				input.AddDofManagerRecord(splitNodeRecord);
				splitNodeIds.Add(splitNodeRecord.Id);
			}

			splitNodeIds.Add(endNode.Id); // append last node

			elementRecord.ReplaceNode(endNode.Id, splitNodeIds[0]);

			var newElementIds = new List<int>();

			for (int i = 1; i < splitNodeIds.Count; i++)
			{
				ElementRecord newElementRecord = elementRecord.WithNodes(input.MaxElementId + 1, splitNodeIds[i - 1], splitNodeIds[i]);
				input.AddElementRecord(newElementRecord);
				copyAllAttributesFromElementToElement(input, elementToSplitId, newElementRecord.Id);
				newElementIds.Add(newElementRecord.Id);
			}

			return newElementIds;
		}

		private void copyAllAttributesFromElementToElement(InputBuilder input, int sourceElementId, int targetElementId)
		{
			var csSetRecords = input.CrossSectionRecords.Values.Select(cs => cs.Set);
			var bcSetRecords = input.BoundaryConditionRecords.Values.Select(bc => bc.Set);

			foreach (var setRecord in csSetRecords.Concat(bcSetRecords))
			{
				if (setRecord.Set.Elements.Contains(sourceElementId))
				{
					setRecord.Set = setRecord.Set.WithElements(setRecord.Set.Elements.Add(targetElementId));
				}

				if (setRecord.Set.ElementEdges.Any(edge => edge.ElementId == sourceElementId))
				{
					var newEdgeList = new List<ElementEdge>();
					foreach (var edge in setRecord.Set.ElementEdges)
					{
						newEdgeList.Add(edge);
						if (edge.ElementId == sourceElementId)
						{
							newEdgeList.Add(new ElementEdge(targetElementId, edge.EdgeRank)); // WARNING: copying edge rank works only for beams
						}
					}
					setRecord.Set = setRecord.Set.WithElementEdges(newEdgeList);
				}

				if (setRecord.Set.ElementSurfaces.Any(surface => surface.ElementId == sourceElementId))
				{
					var newSurfaceList = new List<ElementSurface>();
					foreach (var surface in setRecord.Set.ElementSurfaces)
					{
						newSurfaceList.Add(surface);
						if (surface.ElementId == sourceElementId)
						{
							newSurfaceList.Add(new ElementSurface(targetElementId, surface.SurfaceRank)); // WARNING: copying surface rank works only for beams
						}
					}
					setRecord.Set = setRecord.Set.WithElementSurfaces(newSurfaceList);
				}
			}
		}

		#endregion
	}
}
