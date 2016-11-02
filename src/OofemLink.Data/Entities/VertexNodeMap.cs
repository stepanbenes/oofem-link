using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Data.Entities;

namespace OofemLink.Data
{
    public class VertexNodeMap
    {
		public int ModelId { get; set; }
		public int VertexId { get; set; }
		public Vertex Vertex { get; set; }

		public int MeshId { get; set; }
		public int NodeId { get; set; }
		public Node Node { get; set; }
	}
}
