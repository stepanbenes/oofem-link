using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using OofemLink.Data.DataTransferObjects;
using OofemLink.Data;
using OofemLink.Data.DbEntities;
using Microsoft.Extensions.Logging;
using OofemLink.Common.OofemNames;
using OofemLink.Data.MeshEntities;
using OofemLink.Common.Enumerations;
using System.Collections.Immutable;

namespace OofemLink.Services.DataAccess
{
	public class ModelService : DataService, IModelService
	{
		public ModelService(DataContext context, ILoggerFactory loggerFactory)
			: base(context, loggerFactory)
		{ }

		public async Task<IReadOnlyList<VertexDto>> GetAllVerticesAsync(int modelId)
		{
			return await Context.Vertices.AsNoTracking().Where(v => v.ModelId == modelId).ProjectTo<VertexDto>().ToListAsync();
		}

		public async Task<VertexDto> GetVertexAsync(int modelId, int vertexId)
		{
			return await Context.Vertices.AsNoTracking().Where(v => v.ModelId == modelId && v.Id == vertexId).ProjectTo<VertexDto>().SingleOrDefaultAsync();
		}

		public async Task CreateVertexAsync(int modelId, VertexDto dto)
		{
			var vertex = Mapper.Map<Vertex>(dto);
			vertex.ModelId = modelId;
			Context.Vertices.Add(vertex);
			await Context.SaveChangesAsync();
		}

		public async Task<IReadOnlyList<CurveDto>> GetAllCurvesAsync(int modelId)
		{
			return await Context.Curves.AsNoTracking().Where(c => c.ModelId == modelId).ProjectTo<CurveDto>().ToListAsync();
		}

		public async Task<IReadOnlyList<MeshDto>> GetAllMeshesAsync(int modelId)
		{
			return await Context.Meshes.AsNoTracking().Where(m => m.ModelId == modelId).ProjectTo<MeshDto>().ToListAsync();
		}

		public async Task<IReadOnlyList<AttributeDto>> GetAllAttributesAsync(int modelId, Func<IQueryable<AttributeDto>, IQueryable<AttributeDto>> filter = null)
		{
			var query = Context.Attributes.AsNoTracking().Where(a => a.ModelId == modelId).ProjectTo<AttributeDto>();
			if (filter != null)
				query = filter(query);
			return await query.ToListAsync();
		}

		public async Task<AttributeDto> GetAttributeAsync(int modelId, int attributeId)
		{
			return await Context.Attributes.AsNoTracking().Where(a => a.ModelId == modelId && a.Id == attributeId).ProjectTo<AttributeDto>().SingleOrDefaultAsync();
		}

		public async Task<TimeFunctionDto> GetTimeFunctionAsync(int modelId, int timeFunctionId)
		{
			var timeFunction = await Context.TimeFunctions.AsNoTracking()
												.Include(tf => tf.Values)
												.ThenInclude(tv => tv.TimeStep)
												.SingleOrDefaultAsync(tf => tf.ModelId == modelId && tf.Id == timeFunctionId);
			if (timeFunction == null)
				return null;

			// manually map entities to dtos
			switch (timeFunction.Name) // TODO: replace with type switch when C# 7 is available
			{
				case TimeFunctionNames.ConstantFunction:
					return new ConstantFunctionDto { Id = timeFunction.Id, ConstantValue = ((ConstantFunction)timeFunction).ConstantValue };
				case TimeFunctionNames.PeakFunction:
					var tfValue = timeFunction.Values.Single();
					return new PeakFunctionDto
					{
						Id = timeFunction.Id,
						TimeNumber = tfValue.TimeStep.Number,
						Value = tfValue.Value
					};
				case TimeFunctionNames.PiecewiseLinFunction:
					var timeValues = getTimeStepFunctionValuePairs(timeFunction);
					return new PiecewiseLinFunctionDto
					{
						Id = timeFunction.Id,
						TimeNumbers = timeValues.Keys.ToList(),
						Values = timeValues.Values.ToList()
					};
				default:
					throw new NotSupportedException($"Load time function of type '{timeFunction.Name}' is not supported");
			}
		}

		public async Task<MeshEntitySet> GetAttributeSetAsync(int modelId, int attributeId, int meshId)
		{
			var attribute = await Context.Attributes.FindAsync(modelId, attributeId);

			switch (attribute.Target)
			{
				case AttributeTarget.Node:
					{
						var vertexQuery = createVertexNodeAttributeQuery(modelId, attributeId, meshId, AttributeTarget.Node);
						var curveQuery = createCurveNodeAttributeQuery(modelId, attributeId, meshId, AttributeTarget.Node); // some attributes assigned to curves are meant to be applied to nodes (BoundaryCondition)
						var query = vertexQuery.Concat(curveQuery);
						var nodes = await query.ToListAsync();
						return MeshEntitySet.Empty.WithNodes(nodes);
					}
				case AttributeTarget.Edge:
					{
						var query = createCurveEdgeAttributeQuery(modelId, attributeId, meshId, AttributeTarget.Edge);
						var edges = await query.ToListAsync();
						return MeshEntitySet.Empty.WithElementEdges(edges);
					}
				case AttributeTarget.Surface:
					{
						var query = createSurfaceSurfaceAttributeQuery(modelId, attributeId, meshId, AttributeTarget.Surface);
						var surfaces = await query.ToListAsync();
						return MeshEntitySet.Empty.WithElementSurfaces(surfaces);
					}
				case AttributeTarget.Volume:
					{
						var elementsQuery = createElementAttributeQuery(modelId, attributeId, meshId, AttributeTarget.Volume);
						var elements = await elementsQuery.ToListAsync();
						return MeshEntitySet.Empty.WithElements(elements);
					}
				case AttributeTarget.Undefined:
					{
						var nodesQuery = createVertexNodeAttributeQuery(modelId, attributeId, meshId, AttributeTarget.Undefined);
						var elementsQuery = createElementAttributeQuery(modelId, attributeId, meshId, AttributeTarget.Undefined);
						var nodes = await nodesQuery.ToListAsync();
						var elements = await elementsQuery.ToListAsync();
						return MeshEntitySet.Empty.WithNodes(nodes).WithElements(elements);
					}
				default:
					throw new NotSupportedException($"Attribute target {attribute.Target} is not supported");
			}
		}

		public async Task<IReadOnlyDictionary<int, MeshEntitySet>> GetAttributeSetMapAsync(int modelId, int meshId)
		{
			var map = new Dictionary<int, MeshEntitySet>();

			// AttributeTarget.Node:
			{
				var nodeGroups1 = await createVertexNodeAttributeGroupQuery(modelId, meshId, AttributeTarget.Node).ToListAsync();
				var nodeGroups2 = await createCurveNodeAttributeGroupQuery(modelId, meshId, AttributeTarget.Node).ToListAsync(); // some attributes assigned to curves are meant to be applied to nodes (BoundaryCondition)
				addNodeGroupsToSetMap(map, nodeGroups1);
				addNodeGroupsToSetMap(map, nodeGroups2);
			}
			// AttributeTarget.Edge:
			{
				var edgeGroups = await createCurveEdgeAttributeGroupQuery(modelId, meshId, AttributeTarget.Edge).ToListAsync();
				addEdgeGroupsToSetMap(map, edgeGroups);
			}
			// AttributeTarget.Surface:
			{
				var surfaceGroups = await createSurfaceSurfaceAttributeGroupQuery(modelId, meshId, AttributeTarget.Surface).ToListAsync();
				addSurfaceGroupsToSetMap(map, surfaceGroups);
			}
			// AttributeTarget.Volume:
			{
				var elementGroups1 = await createCurveElementAttributeGroupQuery(modelId, meshId, AttributeTarget.Volume).ToListAsync();
				var elementGroups2 = await createSurfaceElementAttributeGroupQuery(modelId, meshId, AttributeTarget.Volume).ToListAsync();
				var elementGroups3 = await createVolumeElementAttributeGroupQuery(modelId, meshId, AttributeTarget.Volume).ToListAsync();
				addElementGroupsToSetMap(map, elementGroups1);
				addElementGroupsToSetMap(map, elementGroups2);
				addElementGroupsToSetMap(map, elementGroups3);
			}
			// AttributeTarget.Undefined:
			{
				var nodeGroups = await createVertexNodeAttributeGroupQuery(modelId, meshId, AttributeTarget.Undefined).ToListAsync();
				var elementGroups1 = await createCurveElementAttributeGroupQuery(modelId, meshId, AttributeTarget.Undefined).ToListAsync();
				var elementGroups2 = await createSurfaceElementAttributeGroupQuery(modelId, meshId, AttributeTarget.Undefined).ToListAsync();
				var elementGroups3 = await createVolumeElementAttributeGroupQuery(modelId, meshId, AttributeTarget.Undefined).ToListAsync();
				addNodeGroupsToSetMap(map, nodeGroups);
				addElementGroupsToSetMap(map, elementGroups1);
				addElementGroupsToSetMap(map, elementGroups2);
				addElementGroupsToSetMap(map, elementGroups3);
			}

			return map;
		}

		private static void addNodeGroupsToSetMap(Dictionary<int, MeshEntitySet> setMap, List<IGrouping<int, int>> nodeGroups)
		{
			foreach (var nodeGroup in nodeGroups)
			{
				MeshEntitySet set;
				if (!setMap.TryGetValue(nodeGroup.Key, out set))
					set = MeshEntitySet.Empty;
				setMap[nodeGroup.Key] = set.WithNodes(set.Nodes.Union(nodeGroup));
			}
		}

		private static void addElementGroupsToSetMap(Dictionary<int, MeshEntitySet> setMap, List<IGrouping<int, int>> elementGroups)
		{
			foreach (var elementGroup in elementGroups)
			{
				MeshEntitySet set;
				if (!setMap.TryGetValue(elementGroup.Key, out set))
					set = MeshEntitySet.Empty;
				setMap[elementGroup.Key] = set.WithElements(set.Elements.Union(elementGroup));
			}
		}

		private static void addEdgeGroupsToSetMap(Dictionary<int, MeshEntitySet> setMap, List<IGrouping<int, ElementEdge>> edgeGroups)
		{
			foreach (var edgeGroup in edgeGroups)
			{
				MeshEntitySet set;
				if (!setMap.TryGetValue(edgeGroup.Key, out set))
					set = MeshEntitySet.Empty;
				setMap[edgeGroup.Key] = set.WithElementEdges(set.ElementEdges.Union(edgeGroup));
			}
		}

		private static void addSurfaceGroupsToSetMap(Dictionary<int, MeshEntitySet> setMap, List<IGrouping<int, ElementSurface>> surfaceGroups)
		{
			foreach (var surfaceGroup in surfaceGroups)
			{
				MeshEntitySet set;
				if (!setMap.TryGetValue(surfaceGroup.Key, out set))
					set = MeshEntitySet.Empty;
				setMap[surfaceGroup.Key] = set.WithElementSurfaces(set.ElementSurfaces.Union(surfaceGroup));
			}
		}

		public async Task<IReadOnlyList<PartialAttributeApplication>> GetAllPartialAttributeOnCurveApplicationsAsync(int modelId, int meshId, AttributeType attributeType)
		{
			var query = from attribute in Context.Attributes.AsNoTracking()
						where attribute.ModelId == modelId
						where attribute.Type == attributeType
						from curveAttribute in attribute.CurveAttributes
						where (curveAttribute.RelativeStart != null && curveAttribute.RelativeStart > 0) || (curveAttribute.RelativeEnd != null && curveAttribute.RelativeEnd < 1)
						from curveElement in curveAttribute.Curve.CurveElements
						where curveElement.MeshId == meshId
						select new PartialAttributeApplication(attribute.Id, curveElement.ElementId, curveAttribute.RelativeStart, curveAttribute.RelativeEnd);
			return await query.ToListAsync();
		}

		#region Private methods

		#region Single attribute queries

		private IQueryable<int> createVertexNodeAttributeQuery(int modelId, int attributeId, int meshId, AttributeTarget attributeTarget)
		{
			return from vertexAttribute in Context.Set<VertexAttribute>().AsNoTracking()
				   where vertexAttribute.ModelId == modelId
				   where vertexAttribute.AttributeId == attributeId
				   where vertexAttribute.Attribute.Target == attributeTarget
				   from vertexNode in vertexAttribute.Vertex.VertexNodes
				   where vertexNode.MeshId == meshId
				   select vertexNode.NodeId;
		}

		private IQueryable<int> createCurveNodeAttributeQuery(int modelId, int attributeId, int meshId, AttributeTarget attributeTarget)
		{
			return from curveAttribute in Context.Set<CurveAttribute>().AsNoTracking()
				   where curveAttribute.ModelId == modelId
				   where curveAttribute.AttributeId == attributeId
				   where curveAttribute.Attribute.Target == attributeTarget
				   from curveNode in curveAttribute.Curve.CurveNodes
				   where curveNode.MeshId == meshId
				   select curveNode.NodeId;
		}

		private IQueryable<ElementEdge> createCurveEdgeAttributeQuery(int modelId, int attributeId, int meshId, AttributeTarget attributeTarget)
		{
			return from curveAttribute in Context.Set<CurveAttribute>().AsNoTracking()
				   where curveAttribute.ModelId == modelId
				   where curveAttribute.AttributeId == attributeId
				   where (curveAttribute.RelativeStart == null || curveAttribute.RelativeStart == 0) && (curveAttribute.RelativeEnd == null || curveAttribute.RelativeEnd == 1) // do not allow partially applied attributes here
				   where curveAttribute.Attribute.Target == attributeTarget
				   from curveElement in curveAttribute.Curve.CurveElements
				   where curveElement.MeshId == meshId
				   select new ElementEdge(curveElement.ElementId, curveElement.Rank);
		}

		private IQueryable<ElementSurface> createSurfaceSurfaceAttributeQuery(int modelId, int attributeId, int meshId, AttributeTarget attributeTarget)
		{
			return from surfaceAttribute in Context.Set<SurfaceAttribute>().AsNoTracking()
				   where surfaceAttribute.ModelId == modelId
				   where surfaceAttribute.AttributeId == attributeId
				   where surfaceAttribute.Attribute.Target == attributeTarget
				   from surfaceElement in surfaceAttribute.Surface.SurfaceElements
				   where surfaceElement.MeshId == meshId
				   select new ElementSurface(surfaceElement.ElementId, surfaceElement.Rank);
		}

		private IQueryable<int> createCurveElementAttributeQuery(int modelId, int attributeId, int meshId, AttributeTarget attributeTarget)
		{
			return from curveAttribute in Context.Set<CurveAttribute>().AsNoTracking()
				   where curveAttribute.ModelId == modelId
				   where curveAttribute.AttributeId == attributeId
				   where (curveAttribute.RelativeStart == null || curveAttribute.RelativeStart == 0) && (curveAttribute.RelativeEnd == null || curveAttribute.RelativeEnd == 1) // do not allow partially applied attributes here
				   where curveAttribute.Attribute.Target == attributeTarget
				   from curveElement in curveAttribute.Curve.CurveElements
				   where curveElement.MeshId == meshId
				   where curveElement.Rank == 0
				   select curveElement.ElementId;
		}

		private IQueryable<int> createSurfaceElementAttributeQuery(int modelId, int attributeId, int meshId, AttributeTarget attributeTarget)
		{
			return from surfaceAttribute in Context.Set<SurfaceAttribute>().AsNoTracking()
				   where surfaceAttribute.ModelId == modelId
				   where surfaceAttribute.AttributeId == attributeId
				   where surfaceAttribute.Attribute.Target == attributeTarget
				   from surfaceElement in surfaceAttribute.Surface.SurfaceElements
				   where surfaceElement.MeshId == meshId
				   where surfaceElement.Rank == 0
				   select surfaceElement.ElementId;
		}

		private IQueryable<int> createVolumeElementAttributeQuery(int modelId, int attributeId, int meshId, AttributeTarget attributeTarget)
		{
			return from volumeAttribute in Context.Set<VolumeAttribute>().AsNoTracking()
				   where volumeAttribute.ModelId == modelId
				   where volumeAttribute.AttributeId == attributeId
				   where volumeAttribute.Attribute.Target == attributeTarget
				   from volumeElement in volumeAttribute.Volume.VolumeElements
				   where volumeElement.MeshId == meshId
				   select volumeElement.ElementId;
		}

		private IQueryable<int> createElementAttributeQuery(int modelId, int attributeId, int meshId, AttributeTarget attributeTarget)
		{
			var elements1dQuery = createCurveElementAttributeQuery(modelId, attributeId, meshId, attributeTarget);
			var elements2dQuery = createSurfaceElementAttributeQuery(modelId, attributeId, meshId, attributeTarget);
			var elements3dQuery = createVolumeElementAttributeQuery(modelId, attributeId, meshId, attributeTarget);
			return elements1dQuery.Concat(elements2dQuery).Concat(elements3dQuery);
		}

		#endregion

		#region Attribute map queries

		private IQueryable<IGrouping<int, int>> createVertexNodeAttributeGroupQuery(int modelId, int meshId, AttributeTarget attributeTarget)
		{
			return from vertexAttribute in Context.Set<VertexAttribute>().AsNoTracking()
				   where vertexAttribute.ModelId == modelId
				   where vertexAttribute.Attribute.Target == attributeTarget
				   from vertexNode in vertexAttribute.Vertex.VertexNodes
				   where vertexNode.MeshId == meshId
				   group vertexNode.NodeId by vertexAttribute.AttributeId;
		}

		private IQueryable<IGrouping<int, int>> createCurveNodeAttributeGroupQuery(int modelId, int meshId, AttributeTarget attributeTarget)
		{
			return from curveAttribute in Context.Set<CurveAttribute>().AsNoTracking()
				   where curveAttribute.ModelId == modelId
				   where curveAttribute.Attribute.Target == attributeTarget
				   from curveNode in curveAttribute.Curve.CurveNodes
				   where curveNode.MeshId == meshId
				   group curveNode.NodeId by curveAttribute.AttributeId;
		}

		private IQueryable<IGrouping<int, ElementEdge>> createCurveEdgeAttributeGroupQuery(int modelId, int meshId, AttributeTarget attributeTarget)
		{
			return from curveAttribute in Context.Set<CurveAttribute>().AsNoTracking()
				   where curveAttribute.ModelId == modelId
				   where (curveAttribute.RelativeStart == null || curveAttribute.RelativeStart == 0) && (curveAttribute.RelativeEnd == null || curveAttribute.RelativeEnd == 1) // do not allow partially applied attributes here
				   where curveAttribute.Attribute.Target == attributeTarget
				   from curveElement in curveAttribute.Curve.CurveElements
				   where curveElement.MeshId == meshId
				   group new ElementEdge(curveElement.ElementId, curveElement.Rank) by curveAttribute.AttributeId;
		}

		private IQueryable<IGrouping<int, ElementSurface>> createSurfaceSurfaceAttributeGroupQuery(int modelId, int meshId, AttributeTarget attributeTarget)
		{
			return from surfaceAttribute in Context.Set<SurfaceAttribute>().AsNoTracking()
				   where surfaceAttribute.ModelId == modelId
				   where surfaceAttribute.Attribute.Target == attributeTarget
				   from surfaceElement in surfaceAttribute.Surface.SurfaceElements
				   where surfaceElement.MeshId == meshId
				   group new ElementSurface(surfaceElement.ElementId, surfaceElement.Rank) by surfaceAttribute.AttributeId;
		}

		private IQueryable<IGrouping<int, int>> createCurveElementAttributeGroupQuery(int modelId, int meshId, AttributeTarget attributeTarget)
		{
			return from curveAttribute in Context.Set<CurveAttribute>().AsNoTracking()
				   where curveAttribute.ModelId == modelId
				   where (curveAttribute.RelativeStart == null || curveAttribute.RelativeStart == 0) && (curveAttribute.RelativeEnd == null || curveAttribute.RelativeEnd == 1) // do not allow partially applied attributes here
				   where curveAttribute.Attribute.Target == attributeTarget
				   from curveElement in curveAttribute.Curve.CurveElements
				   where curveElement.MeshId == meshId
				   where curveElement.Rank == 0
				   group curveElement.ElementId by curveAttribute.AttributeId;
		}

		private IQueryable<IGrouping<int, int>> createSurfaceElementAttributeGroupQuery(int modelId, int meshId, AttributeTarget attributeTarget)
		{
			return from surfaceAttribute in Context.Set<SurfaceAttribute>().AsNoTracking()
				   where surfaceAttribute.ModelId == modelId
				   where surfaceAttribute.Attribute.Target == attributeTarget
				   from surfaceElement in surfaceAttribute.Surface.SurfaceElements
				   where surfaceElement.MeshId == meshId
				   where surfaceElement.Rank == 0
				   group surfaceElement.ElementId by surfaceAttribute.AttributeId;
		}

		private IQueryable<IGrouping<int, int>> createVolumeElementAttributeGroupQuery(int modelId, int meshId, AttributeTarget attributeTarget)
		{
			return from volumeAttribute in Context.Set<VolumeAttribute>().AsNoTracking()
				   where volumeAttribute.ModelId == modelId
				   where volumeAttribute.Attribute.Target == attributeTarget
				   from volumeElement in volumeAttribute.Volume.VolumeElements
				   where volumeElement.MeshId == meshId
				   group volumeElement.ElementId by volumeAttribute.AttributeId;
		}

		#endregion

		private ImmutableSortedDictionary<int, double> getTimeStepFunctionValuePairs(TimeFunction timeFunction)
		{
			// The particular time values in t array should be sorted according to time scale
			// Therefore use sorted dictionary
			var result = ImmutableSortedDictionary<int, double>.Empty;
			foreach (var tfValue in timeFunction.Values)
			{
				result = result.Add(tfValue.TimeStep.Number, tfValue.Value);
			}
			return result;
		}

		#endregion
	}
}
