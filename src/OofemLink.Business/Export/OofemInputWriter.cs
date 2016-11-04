using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using OofemLink.Data;
using OofemLink.Data.Entities;

namespace OofemLink.Business.Export
{
    public class OofemInputWriter
    {
		// TODO: see DynamicInputRecord in OOFEM source

		readonly StreamWriter writer;

		public OofemInputWriter(StreamWriter writer)
		{
			this.writer = writer;
		}

		public OofemInputWriter WriteNodeCount(int nodeCount)
		{
			writer.WriteLine("nodes " + nodeCount);
			return this;
		}

		public OofemInputWriter WriteNode(Node node)
		{
			writer.WriteLine($"{node.Id} { node.X} { node.Y} { node.Z}");
			return this;
		}
    }
}
