using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.Entities
{
    public class AttributeComposition
    {
		public int ModelId { get; set; }
		public int ParentId { get; set; }
		public int ChildId { get; set; }

		public virtual Model Model { get; set; }
		public virtual Attribute Parent { get; set; }
		public virtual Attribute Child { get; set; }
	}
}
