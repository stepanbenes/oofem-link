using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OofemLink.Common.Enumerations;
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

		// GET api/model/1/mesh
		[HttpGet("{modelId}/mesh")]
		public Task<IReadOnlyList<MeshDto>> GetMeshes(int modelId) => service.GetAllMeshesAsync(modelId);

		// GET api/model/1/attribute
		[HttpGet("{modelId}/attribute")]
		public Task<IReadOnlyList<AttributeDto>> GetAttributes([FromRoute] int modelId, [FromQuery] AttributeType? type)
		{
			if (type != null)
				return service.GetAllAttributesAsync(modelId, attributes => attributes.Where(a => a.Type == type.Value));
			return service.GetAllAttributesAsync(modelId);
		}

		// GET api/model/1/attribute/4
		[HttpGet("{modelId}/attribute/{attributeId}")]
		public Task<AttributeDto> GetAttributes(int modelId, int attributeId) => service.GetAttributeAsync(modelId, attributeId);
	}
}
