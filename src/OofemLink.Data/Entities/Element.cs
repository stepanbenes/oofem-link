using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Common.Enumerations;

namespace OofemLink.Data.Entities
{
    public class Element : MeshEntity
    {
		public CellType Type { get; set; }
		public int Tag { get; set; }

		public virtual ICollection<ElementNode> ElementNodes { get; set; } = new List<ElementNode>();
	}
}
