using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OofemLink.Business.Dto;
using OofemLink.Data;

namespace OofemLink.Business.Services
{
	public class SimulationService : DataService<Simulation>, IService<ViewSimulationDto, int>
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

		public void Create(ViewSimulationDto dto)
		{
			throw new NotImplementedException();
		}

		public void Delete(int primaryKey)
		{
			throw new NotImplementedException();
		}

		public ViewSimulationDto Get(int primaryKey)
		{
			throw new NotImplementedException();
		}

		public IReadOnlyList<ViewSimulationDto> GetAll(Func<IQueryable<ViewSimulationDto>, IQueryable<ViewSimulationDto>> filter = null)
		{
			throw new NotImplementedException();
		}

		public void Update(int primaryKey, ViewSimulationDto dto)
		{
			throw new NotImplementedException();
		}
	}
}
