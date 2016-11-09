using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.Entities
{
    public class VertexNodeMapping
    {
		public int ModelId { get; set; }
		public int VertexId { get; set; }

		public int MeshId { get; set; }
		public int NodeId { get; set; }

		public virtual Model Model { get; set; }
		public virtual Mesh Mesh { get; set; }
		public virtual Vertex Vertex { get; set; }
		public virtual Node Node { get; set; }
	}
}
