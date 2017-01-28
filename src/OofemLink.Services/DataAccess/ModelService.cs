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

namespace OofemLink.Services.DataAccess
{
	public class ModelService : IModelService
	{
		readonly DataContext context;

		public ModelService(DataContext context)
		{
			this.context = context;
		}

		public async Task<IReadOnlyList<VertexDto>> GetAllVerticesAsync(int modelId)
		{
			return await context.Vertices.AsNoTracking().Where(v => v.ModelId == modelId).ProjectTo<VertexDto>().ToListAsync();
		}

		public async Task<VertexDto> GetVertexAsync(int modelId, int vertexId)
		{
			return await context.Vertices.Where(v => v.ModelId == modelId && v.Id == vertexId).ProjectTo<VertexDto>().SingleOrDefaultAsync();
		}

		public async Task CreateVertexAsync(int modelId, VertexDto dto)
		{
			var vertex = Mapper.Map<Vertex>(dto);
			vertex.ModelId = modelId;
			context.Vertices.Add(vertex);
			await context.SaveChangesAsync();
		}

		public async Task<IReadOnlyList<CurveDto>> GetAllCurvesAsync(int modelId)
		{
			return await context.Curves.AsNoTracking().Where(c => c.ModelId == modelId).ProjectTo<CurveDto>().ToListAsync();
		}

		public async Task<IReadOnlyList<MeshDto>> GetAllMeshesAsync(int modelId)
		{
			return await context.Meshes.AsNoTracking().Where(m => m.ModelId == modelId).ProjectTo<MeshDto>().ToListAsync();
		}
	}
}
