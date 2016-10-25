using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Data.ModelEntities;

namespace OofemLink.Data.MeshEntities
{
    public class Mesh
    {
		public int Id { get; set; }

		public virtual int ModelId { get; set; }
		public virtual Model Model { get; set; }

		public virtual ICollection<Node> Nodes { get; set; } = new List<Node>();
		public virtual ICollection<Beam> Beams { get; set; } = new List<Beam>();
	}
}
