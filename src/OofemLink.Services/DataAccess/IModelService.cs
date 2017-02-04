using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Common.Enumerations;
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

		Task<MeshEntitySet> GetAttributeSetAsync(int modelId, int attributeId, int meshId);
		Task<IReadOnlyDictionary<int, MeshEntitySet>> GetAttributeSetMapAsync(int modelId, int meshId);
		Task<IReadOnlyList<PartialAttributeApplication>> GetAllPartialAttributeOnCurveApplicationsAsync(int modelId, int meshId, AttributeType attributeType);
	}
}
