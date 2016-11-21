using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OofemLink.Data.DataTransferObjects;
using OofemLink.Services.DataAccess;

namespace OofemLink.WebApi.Controllers
{
	[Route("api/model")]
	public class ModelController : Controller
    {
		readonly IModelService service;

		public ModelController(IModelService service)
		{
			this.service = service;
		}

		// GET api/model/1/vertex
		[HttpGet("{modelId}/vertex")]
		public Task<IReadOnlyList<VertexDto>> GetVertices(int modelId) => service.GetAllVerticesAsync(modelId);

		// GET api/model/1/vertex/4
		[HttpGet("{modelId}/vertex/{vertexId}")]
		public Task<VertexDto> GetVertex(int modelId, int vertexId) => service.GetVertexAsync(modelId, vertexId);

		// POST api/model/1/vertex
		[HttpPost("{modelId}/vertex")]
		public Task PostVertex(int modelId, [FromBody]VertexDto vertex) => service.CreateVertexAsync(modelId, vertex);

		// GET api/model/1/curve
		[HttpGet("{modelId}/curve")]
		public Task<IReadOnlyList<CurveDto>> GetCurves(int modelId) => service.GetAllCurvesAsync(modelId);
	}
}
