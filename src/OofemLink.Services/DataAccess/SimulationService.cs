using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using OofemLink.Data.DataTransferObjects;
using OofemLink.Services.Export;
using OofemLink.Data;
using OofemLink.Data.Entities;

namespace OofemLink.Services.DataAccess
{
	public class SimulationService : DataService, ISimulationService
	{
		public SimulationService(DataContext context)
			: base(context)
		{ }

		public void Export(int simulationId, IExportService exportService)
		{
			exportService.ExportSimulation(simulationId);
		}

		public void Run()
		{
			throw new NotImplementedException();
		}

		public async Task<IReadOnlyList<ViewSimulationDto>> GetAllAsync(Func<IQueryable<ViewSimulationDto>, IQueryable<ViewSimulationDto>> filter = null)
		{
			return await GetQuery<Simulation, ViewSimulationDto>(filter).ToListAsync();
		}

		public async Task<ViewSimulationDto> GetOneAsync(int primaryKey)
		{
			return await Context.Simulations.Where(p => p.Id == primaryKey).ProjectTo<ViewSimulationDto>().SingleOrDefaultAsync();
		}

		public async Task CreateAsync(EditSimulationDto dto)
		{
			Context.Simulations.Add(Mapper.Map<Simulation>(dto));
			await Context.SaveChangesAsync();
		}

		public async Task UpdateAsync(int primaryKey, EditSimulationDto dto)
		{
			var entityToUpdate = Mapper.Map<Simulation>(dto);
			entityToUpdate.Id = primaryKey;
			Context.Simulations.Update(entityToUpdate);
			await Context.SaveChangesAsync();
		}

		public async Task DeleteAsync(int primaryKey)
		{
			var entityToDelete = new Simulation { Id = primaryKey };
			Context.Simulations.Remove(entityToDelete);
			await Context.SaveChangesAsync();
		}
	}
}
