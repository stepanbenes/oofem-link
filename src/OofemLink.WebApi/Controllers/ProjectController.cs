using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OofemLink.Business.Dto;
using OofemLink.Business.Services;

namespace OofemLink.WebApi.Controllers
{
	[Route("api/project")]
	public class ProjectController : Controller
	{
		readonly IProjectService service;

		public ProjectController(IProjectService service)
		{
			this.service = service;
		}

		// GET api/project
		[HttpGet]
		public Task<IReadOnlyList<ProjectDto>> Get() => service.GetAllAsync();

		// GET api/project/4
		[HttpGet("{id}")]
		public Task<ProjectDto> Get(int id) => service.GetOneAsync(id);

		// POST api/project
		[HttpPost]
		public Task Post([FromBody]ProjectDto project) => service.CreateAsync(project);

		// PUT api/project/4
		[HttpPut("{id}")]
		public Task Put(int id, [FromBody]ProjectDto project) => service.UpdateAsync(id, project);

		// DELETE api/project/4
		[HttpDelete("{id}")]
		public Task Delete(int id) => service.DeleteAsync(id);
	}
}
