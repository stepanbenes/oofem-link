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
			return await Context.Attributes.Where(a => a.ModelId == modelId && a.Id == attributeId).ProjectTo<AttributeDto>().SingleOrDefaultAsync();
		}

		public async Task<TimeFunctionDto> GetTimeFunctionAsync(int modelId, int timeFunctionId)
		{
			var timeFunction = await Context.TimeFunctions
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
						TimeNumbers = timeValues.Select(tv => tv.Key).ToList(),
						Values = timeValues.Select(tv => tv.Value).ToList()
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
						var vertexQuery = from vertexAttribute in Context.Set<VertexAttribute>()
										  where vertexAttribute.ModelId == modelId
										  where vertexAttribute.AttributeId == attributeId
										  where vertexAttribute.Attribute.Target == AttributeTarget.Node
										  from vertexNode in vertexAttribute.Vertex.VertexNodes
										  where vertexNode.MeshId == meshId
										  select vertexNode.NodeId;
						var curveQuery = from curveAttribute in Context.Set<CurveAttribute>() // some attributes assigned to curves are meant to be applied to nodes (BoundaryCondition)
										 where curveAttribute.ModelId == modelId
										 where curveAttribute.AttributeId == attributeId
										 where curveAttribute.Attribute.Target == AttributeTarget.Node
										 from curveNode in curveAttribute.Curve.CurveNodes
										 where curveNode.MeshId == meshId
										 select curveNode.NodeId;
						var query = vertexQuery.Concat(curveQuery).OrderBy(id => id).Distinct();
						var nodes = await query.ToListAsync();
						return new MeshEntitySet().WithNodes(nodes);
					}
				case AttributeTarget.Edge:
					{
						var query = from curveAttribute in Context.Set<CurveAttribute>()
									where curveAttribute.ModelId == modelId
									where curveAttribute.AttributeId == attributeId
									where (curveAttribute.RelativeStart == null || curveAttribute.RelativeStart == 0) && (curveAttribute.RelativeEnd == null || curveAttribute.RelativeEnd == 1) // do not allow partially applied attributes here
									where curveAttribute.Attribute.Target == AttributeTarget.Edge
									from curveElement in curveAttribute.Curve.CurveElements
									where curveElement.MeshId == meshId
									orderby curveElement.ElementId, curveElement.Rank
									select new ElementEdge(curveElement.ElementId, curveElement.Rank);
						var edges = await query.ToListAsync();
						return new MeshEntitySet().WithElementEdges(edges);
					}
				case AttributeTarget.Surface:
					{
						var query = from surfaceAttribute in Context.Set<SurfaceAttribute>()
									where surfaceAttribute.ModelId == modelId
									where surfaceAttribute.AttributeId == attributeId
									where surfaceAttribute.Attribute.Target == AttributeTarget.Surface
									from surfaceElement in surfaceAttribute.Surface.SurfaceElements
									where surfaceElement.MeshId == meshId
									orderby surfaceElement.ElementId, surfaceElement.Rank
									select new ElementSurface(surfaceElement.ElementId, surfaceElement.Rank);
						var surfaces = await query.ToListAsync();
						return new MeshEntitySet().WithElementSurfaces(surfaces);
					}
				case AttributeTarget.Volume:
					{
						var elementsQuery = createElementAttributeQuery(modelId, attributeId, meshId, AttributeTarget.Volume);
						var elements = await elementsQuery.ToListAsync();
						return new MeshEntitySet().WithElements(elements);
					}
				case AttributeTarget.Undefined:
					{
						var nodesQuery = from vertexAttribute in Context.Set<VertexAttribute>()
										 where vertexAttribute.ModelId == modelId
										 where vertexAttribute.AttributeId == attributeId
										 where vertexAttribute.Attribute.Target == AttributeTarget.Undefined
										 from vertexNode in vertexAttribute.Vertex.VertexNodes
										 where vertexNode.MeshId == meshId
										 orderby vertexNode.NodeId
										 select vertexNode.NodeId;
						var elementsQuery = createElementAttributeQuery(modelId, attributeId, meshId, AttributeTarget.Undefined);
						var nodes = await nodesQuery.ToListAsync();
						var elements = await elementsQuery.ToListAsync();
						return new MeshEntitySet().WithNodes(nodes).WithElements(elements);
					}
				default:
					throw new NotSupportedException($"Attribute target {attribute.Target} is not supported");
			}
		}

		public async Task<IReadOnlyList<PartialAttributeApplication>> GetAllPartialAttributeOnCurveApplicationsAsync(int modelId, int meshId, AttributeType attributeType)
		{
			var query = from attribute in Context.Attributes
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

		private IQueryable<int> createElementAttributeQuery(int modelId, int attributeId, int meshId, AttributeTarget attributeTarget)
		{
			var elements1dQuery = from curveAttribute in Context.Set<CurveAttribute>()
								  where curveAttribute.ModelId == modelId
								  where curveAttribute.AttributeId == attributeId
								  where (curveAttribute.RelativeStart == null || curveAttribute.RelativeStart == 0) && (curveAttribute.RelativeEnd == null || curveAttribute.RelativeEnd == 1) // do not allow partially applied attributes here
								  where curveAttribute.Attribute.Target == attributeTarget
								  from curveElement in curveAttribute.Curve.CurveElements
								  where curveElement.MeshId == meshId
								  //where curveElement.Rank == 0
								  select curveElement.ElementId;
			var elements2dQuery = from surfaceAttribute in Context.Set<SurfaceAttribute>()
								  where surfaceAttribute.ModelId == modelId
								  where surfaceAttribute.AttributeId == attributeId
								  where surfaceAttribute.Attribute.Target == attributeTarget
								  from surfaceElement in surfaceAttribute.Surface.SurfaceElements
								  where surfaceElement.MeshId == meshId
								  //where surfaceElement.Rank == 0
								  select surfaceElement.ElementId;
			var elements3dQuery = from volumeAttribute in Context.Set<VolumeAttribute>()
								  where volumeAttribute.ModelId == modelId
								  where volumeAttribute.AttributeId == attributeId
								  where volumeAttribute.Attribute.Target == attributeTarget
								  from volumeElement in volumeAttribute.Volume.VolumeElements
								  where volumeElement.MeshId == meshId
								  //where volumeElement.Rank == 0
								  select volumeElement.ElementId;
			return elements1dQuery.Concat(elements2dQuery).Concat(elements3dQuery).OrderBy(id => id).Distinct();
		}

		private List<KeyValuePair<int, double>> getTimeStepFunctionValuePairs(TimeFunction timeFunction)
		{
			var resultQuery = from tfValue in timeFunction.Values
							  orderby tfValue.TimeStep.Number // The particular time values in t array should be sorted according to time scale
							  select new KeyValuePair<int, double>(tfValue.TimeStep.Number, tfValue.Value);
			return resultQuery.ToList();
		}

		#endregion
	}
}
