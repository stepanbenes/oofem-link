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
	}
}
