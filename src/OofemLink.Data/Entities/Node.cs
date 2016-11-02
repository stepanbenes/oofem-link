using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.Entities
{
    public class Node : MeshEntity
    {
		public double X { get; set; }
		public double Y { get; set; }
		public double Z { get; set; }

		public virtual ICollection<ElementNode> ElementNodes { get; set; } = new List<ElementNode>();

		public virtual ICollection<VertexNodeMap> VertexNodeMap { get; set; } = new List<VertexNodeMap>();
	}
}
