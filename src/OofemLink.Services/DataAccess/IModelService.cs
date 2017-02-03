﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Data.DataTransferObjects;
using OofemLink.Data.MeshEntities;

namespace OofemLink.Services.DataAccess
{
	public interface IModelService
	{
		Task<IReadOnlyList<VertexDto>> GetAllVerticesAsync(int modelId);
		Task<VertexDto> GetVertexAsync(int modelId, int vertexId);
		Task CreateVertexAsync(int modelId, VertexDto dto);
		Task<IReadOnlyList<CurveDto>> GetAllCurvesAsync(int modelId);

		// ---------------------------------------

		Task<IReadOnlyList<MeshDto>> GetAllMeshesAsync(int modelId);
		Task<IReadOnlyList<AttributeDto>> GetAllAttributesAsync(int modelId, Func<IQueryable<AttributeDto>, IQueryable<AttributeDto>> filter = null);
		Task<AttributeDto> GetAttributeAsync(int modelId, int attributeId);
		Task<TimeFunctionDto> GetTimeFunctionAsync(int modelId, int timeFunctionId);

		Task<Set> GetMeshEntitiesWithAttributeAsync(int modelId, int attributeId, int meshId);
	}
}
