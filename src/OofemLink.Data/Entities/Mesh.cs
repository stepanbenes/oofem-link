using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.Entities
{
    public class Mesh
    {
		public int Id { get; set; }

		public int ModelId { get; set; }
		public virtual Model Model { get; set; }

		public virtual ICollection<Node> Nodes { get; set; } = new List<Node>();
		public virtual ICollection<Element> Elements { get; set; } = new List<Element>();

		public virtual ICollection<VertexNodeMapping> VertexNodes { get; set; } = new List<VertexNodeMapping>();
		public virtual ICollection<Edge> Edges { get; set; } = new List<Edge>();
		public virtual ICollection<Face> Faces { get; set; } = new List<Face>();
		public virtual ICollection<VolumeElementMapping> VolumeElements { get; set; } = new List<VolumeElementMapping>();
	}
}
