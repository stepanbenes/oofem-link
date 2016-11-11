using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Business.Dto;

namespace OofemLink.Business.Services
{
	public interface IModelService
	{
		Task<IReadOnlyList<VertexDto>> GetAllVerticesAsync(int modelId);
		Task<VertexDto> GetVertexAsync(int modelId, int vertexId);
		Task CreateVertexAsync(int modelId, VertexDto dto);

		Task<IReadOnlyList<CurveDto>> GetAllCurvesAsync(int modelId);
	}
}
