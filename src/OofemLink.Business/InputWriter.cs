using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using OofemLink.Data;
using OofemLink.Data.MeshEntities;

namespace OofemLink.Business
{
    public class InputWriter
    {
		// TODO: see DynamicInputRecord in OOFEM source

		readonly StreamWriter writer;

		public InputWriter(StreamWriter writer)
		{
			this.writer = writer;
		}

		public InputWriter WriteNodeCount(int nodeCount)
		{
			writer.WriteLine("nodes " + nodeCount);
			return this;
		}

		public InputWriter WriteNode(Node node)
		{
			writer.WriteLine($"{node.Id} { node.X} { node.Y} { node.Z}");
			return this;
		}
    }
}
