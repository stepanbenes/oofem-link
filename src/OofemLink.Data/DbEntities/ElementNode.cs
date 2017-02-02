using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.DbEntities
{
    public class ElementNode
    {
		public int MeshId { get; set; }

		public int ElementId { get; set; }
		public int NodeId { get; set; }

		public virtual Element Element { get; set; }
		public virtual Node Node { get; set; }

		public byte Rank { get; set; }
    }
}
