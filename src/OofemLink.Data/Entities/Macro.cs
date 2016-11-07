using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.Entities
{
    public class Macro : IModelEntity
    {
		public int ModelId { get; set; }
		public virtual Model Model { get; set; }
		public int Id { get; set; }

		public int GeometryEntityId { get; set; }
		public virtual GeometryEntity GeometryEntity { get; set; }

		//public virtual ICollection<AttributeMap> AttributeMappings { get; set; } = new List<AttributeMap>();
	}
}
