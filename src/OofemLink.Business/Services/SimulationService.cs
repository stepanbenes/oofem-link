using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using OofemLink.Business.Dto;
using OofemLink.Data;

namespace OofemLink.Business.Services
{
	public class SimulationService : DataService, ISimulationService
	{
		public SimulationService(DataContext context)
			: base(context)
		{ }

		public void BuildInputFile(int simulationId, string fileFullPath)
		{
			using (var stream = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write, FileShare.None))
			using (var streamWriter = new StreamWriter(stream))
			{
				var inputFile = new InputWriter(streamWriter);

				var nodes = Context.Meshes.Include(m => m.Nodes).Single(mesh => mesh.ModelId == simulationId).Nodes;

				inputFile.WriteNodeCount(nodes.Count);

				foreach (var node in nodes)
				{
					inputFile.WriteNode(node);
				}
			}
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
