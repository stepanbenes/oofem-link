using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OofemLink.Business.Dto;
using OofemLink.Business.Services;

namespace OofemLink.WebApi.Controllers
{
	[Route("api/[controller]")]
	public class ModelController : Controller
    {
		readonly IModelService service;

		public ModelController(IModelService service)
		{
			this.service = service;
		}

		// POST api/model/1/vertex
		[HttpGet("{modelId}/vertex")]
		public Task<IReadOnlyList<VertexDto>> Get(int modelId) => service.GetAllVerticesAsync(modelId);

		// POST api/model/1/vertex/4
		[HttpGet("{modelId}/vertex/{vertexId}")]
		public Task<VertexDto> Get(int modelId, int vertexId) => service.GetVertexAsync(modelId, vertexId);

		// POST api/model/1/vertex
		[HttpPost("{modelId}/vertex")]
		public Task Post(int modelId, [FromBody]VertexDto vertex) => service.CreateVertexAsync(modelId, vertex);
	}
}
