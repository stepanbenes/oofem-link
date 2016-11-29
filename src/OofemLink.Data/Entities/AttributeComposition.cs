using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.Entities
{
    public class AttributeComposition
    {
		public int ModelId { get; set; }
		public int ParentAttributeId { get; set; }
		public int ChildAttributeId { get; set; }

		public virtual Model Model { get; set; }
		public virtual ModelAttribute ParentAttribute { get; set; }
		public virtual ModelAttribute ChildAttribute { get; set; }
	}
}
