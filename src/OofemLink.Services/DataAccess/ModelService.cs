using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using OofemLink.Data.DataTransferObjects;
using OofemLink.Data;
using OofemLink.Data.Entities;
using Microsoft.Extensions.Logging;

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
	}
}
