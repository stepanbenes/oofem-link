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
	public class ProjectController : Controller
	{
		readonly IService<ProjectDto, int> service;

		public ProjectController(IService<ProjectDto, int> service)
		{
			this.service = service;
		}

		// GET api/project
		[HttpGet]
		public IEnumerable<ProjectDto> Get()
		{
			return service.GetAll();
		}

		// GET api/project/4
		[HttpGet("{id}")]
		public ProjectDto Get(int id)
		{
			return service.Get(id);
		}

		// POST api/project
		[HttpPost]
		public void Post([FromBody]ProjectDto project)
		{
			service.Create(project);
		}

		// PUT api/project/4
		[HttpPut("{id}")]
		public void Put(int id, [FromBody]ProjectDto project)
		{
			service.Update(id, project);
		}

		// DELETE api/project/4
		[HttpDelete("{id}")]
		public void Delete(int id)
		{
			service.Delete(id);
		}
	}
}
