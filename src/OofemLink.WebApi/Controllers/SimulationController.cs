using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OofemLink.Data.DataTransferObjects;
using OofemLink.Services.DataAccess;

namespace OofemLink.WebApi.Controllers
{
	[Route("api/simulation")]
	public class SimulationController : Controller
    {
		readonly ISimulationService service;

		public SimulationController(ISimulationService service)
		{
			this.service = service;
		}

		// GET api/simulation
		[HttpGet]
		public async Task<IEnumerable<ViewSimulationDto>> Get() => await service.GetAllAsync();

		// GET api/simulation/4
		[HttpGet("{id}")]
		public Task<ViewSimulationDto> Get(int id) => service.GetOneAsync(id);

		// POST api/simulation
		[HttpPost]
		public Task Post([FromBody]EditSimulationDto simulation) => service.CreateAsync(simulation);

		// PUT api/simulation/4
		[HttpPut("{id}")]
		public Task Put(int id, [FromBody]EditSimulationDto simulation) => service.UpdateAsync(id, simulation);

		// DELETE api/simulation/4
		[HttpDelete("{id}")]
		public Task Delete(int id) => service.DeleteAsync(id);
	}
}
