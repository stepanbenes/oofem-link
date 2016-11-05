using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Data;

namespace OofemLink.Business.Export
{
	class OofemInputFileExportService : IExportService
	{
		readonly string fileFullPath;

		public OofemInputFileExportService(string fileFullPath)
		{
			this.fileFullPath = fileFullPath;
		}

		public void ExportSimulation(Simulation simulation)
		{
			using (var stream = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write, FileShare.None))
			using (var streamWriter = new StreamWriter(stream))
			{
				var inputWriter = new OofemInputWriter(streamWriter);

				var nodes = simulation.Models.Single().Meshes.Single().Nodes;

				inputWriter.WriteNodeCount(nodes.Count);

				foreach (var node in nodes)
				{
					inputWriter.WriteNode(node);
				}
			}
		}
	}
}
