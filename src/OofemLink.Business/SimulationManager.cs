using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OofemLink.Business.Import;
using OofemLink.Data;

namespace OofemLink.Business
{
	public class SimulationManager
	{
		readonly int simulationId;

		public SimulationManager(int simulationId)
		{
			this.simulationId = simulationId;
		}

		public void BuildInputFile(string fileFullPath)
		{
			using (var stream = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write, FileShare.None))
			using (var streamWriter = new StreamWriter(stream))
			{
				var inputFile = new InputWriter(streamWriter);

				using (var db = new DataContext())
				{
					var nodes = db.Meshes.Include(m => m.Nodes).Single(mesh => mesh.ModelId == simulationId).Nodes;

					inputFile.WriteNodeCount(nodes.Count);

					foreach (var node in nodes)
					{
						inputFile.WriteNode(node);
					}
				}
			}
		}

		public void Run()
		{
			throw new NotImplementedException();
		}
	}
}
