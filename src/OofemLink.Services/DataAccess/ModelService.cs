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

		public async Task<Set> GetMeshEntitiesWithAttributeAsync(int modelId, int attributeId)
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
										  select vertexNode.NodeId;
						var curveQuery = from curveAttribute in Context.Set<CurveAttribute>() // some attributes assigned to curves are meant to be applied to nodes (BoundaryCondition)
										 where curveAttribute.ModelId == modelId
										 where curveAttribute.AttributeId == attributeId
										 where curveAttribute.Attribute.Target == AttributeTarget.Node
										 from curveNode in curveAttribute.Curve.CurveNodes
										 select curveNode.NodeId;
						var query = vertexQuery.Concat(curveQuery).OrderBy(id => id).Distinct();
						var nodes = await query.ToListAsync();
						return new Set().WithNodes(nodes);
					}
				case AttributeTarget.Edge:
					{
						var query = from curveAttribute in Context.Set<CurveAttribute>()
									where curveAttribute.ModelId == modelId
									where curveAttribute.AttributeId == attributeId
									where curveAttribute.Attribute.Target == AttributeTarget.Edge
									from curveElement in curveAttribute.Curve.CurveElements
									orderby curveElement.ElementId, curveElement.Rank
									select new ElementEdge(curveElement.ElementId, curveElement.Rank);
						var edges = await query.ToListAsync();
						return new Set().WithElementEdges(edges);
					}
				case AttributeTarget.Surface:
					{
						var query = from surfaceAttribute in Context.Set<SurfaceAttribute>()
									where surfaceAttribute.ModelId == modelId
									where surfaceAttribute.AttributeId == attributeId
									where surfaceAttribute.Attribute.Target == AttributeTarget.Surface
									from surfaceElement in surfaceAttribute.Surface.SurfaceElements
									orderby surfaceElement.ElementId, surfaceElement.Rank
									select new ElementSurface(surfaceElement.ElementId, surfaceElement.Rank);
						var surfaces = await query.ToListAsync();
						return new Set().WithElementSurfaces(surfaces);
					}
				case AttributeTarget.Volume:
					{
						var elements1dQuery = from curveAttribute in Context.Set<CurveAttribute>()
											  where curveAttribute.ModelId == modelId
											  where curveAttribute.Attribute.Target == AttributeTarget.Volume
											  from curveElement in curveAttribute.Curve.CurveElements
											  select curveElement.ElementId;
						var elements2dQuery = from surfaceAttribute in Context.Set<SurfaceAttribute>()
											  where surfaceAttribute.ModelId == modelId
											  where surfaceAttribute.Attribute.Target == AttributeTarget.Volume
											  from surfaceElement in surfaceAttribute.Surface.SurfaceElements
											  select surfaceElement.ElementId;
						var elements3dQuery = from volumeAttribute in Context.Set<VolumeAttribute>()
											  where volumeAttribute.ModelId == modelId
											  where volumeAttribute.Attribute.Target == AttributeTarget.Volume
											  from volumeElement in volumeAttribute.Volume.VolumeElements
											  select volumeElement.ElementId;
						var query = elements1dQuery.Concat(elements2dQuery).Concat(elements3dQuery).OrderBy(id => id).Distinct();
						var elements = await query.ToListAsync();
						return new Set().WithElements(elements);
					}
				case AttributeTarget.Undefined:
					{
						var nodesQuery = from vertexAttribute in Context.Set<VertexAttribute>()
										 where vertexAttribute.ModelId == modelId
										 where vertexAttribute.AttributeId == attributeId
										 where vertexAttribute.Attribute.Target == AttributeTarget.Undefined
										 from vertexNode in vertexAttribute.Vertex.VertexNodes
										 orderby vertexNode.NodeId
										 select vertexNode.NodeId;
						var elements1dQuery = from curveAttribute in Context.Set<CurveAttribute>()
											  where curveAttribute.ModelId == modelId
											  where curveAttribute.AttributeId == attributeId
											  where curveAttribute.Attribute.Target == AttributeTarget.Undefined
											  from curveElement in curveAttribute.Curve.CurveElements
											  select curveElement.ElementId;
						var elements2dQuery = from surfaceAttribute in Context.Set<SurfaceAttribute>()
											  where surfaceAttribute.ModelId == modelId
											  where surfaceAttribute.AttributeId == attributeId
											  where surfaceAttribute.Attribute.Target == AttributeTarget.Undefined
											  from surfaceElement in surfaceAttribute.Surface.SurfaceElements
											  select surfaceElement.ElementId;
						var elements3dQuery = from volumeAttribute in Context.Set<VolumeAttribute>()
											  where volumeAttribute.ModelId == modelId
											  where volumeAttribute.AttributeId == attributeId
											  where volumeAttribute.Attribute.Target == AttributeTarget.Undefined
											  from volumeElement in volumeAttribute.Volume.VolumeElements
											  select volumeElement.ElementId;
						var elementsQuery = elements1dQuery.Concat(elements2dQuery).Concat(elements3dQuery).OrderBy(id => id).Distinct();
						var nodes = await nodesQuery.ToListAsync();
						var elements = await elementsQuery.ToListAsync();
						return new Set().WithNodes(nodes).WithElements(elements);
					}
				default:
					throw new NotSupportedException($"Attribute target {attribute.Target} is not supported");
			}
		}

		#region Private methods

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
