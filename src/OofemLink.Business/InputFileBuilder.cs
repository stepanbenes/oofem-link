using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using OofemLink.Data;

namespace OofemLink.Business
{
    public class InputFileBuilder
    {
		readonly string fileFullPath;

		public InputFileBuilder(string fileFullPath)
		{
			this.fileFullPath = fileFullPath;
		}

		public void Build(int modelId)
		{
			StringBuilder text = new StringBuilder();

			using (var db = new DataContext())
			{
				var nodes = db.Meshes.Include(m => m.Nodes).Single(mesh => mesh.ModelId == modelId).Nodes;
				text.AppendLine("nodes " + nodes.Count);
				foreach (var node in nodes)
				{
					text.AppendLine($"{node.Id} { node.X} { node.Y} { node.Z}");
				}
			}

			File.WriteAllText(fileFullPath ?? Path.Combine(Directory.GetCurrentDirectory(), "test.in"), text.ToString());
		}
    }
}
