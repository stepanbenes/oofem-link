using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.Entities
{
    public class Attribute : IModelEntity
    {
		public int ModelId { get; set; }
		public virtual Model Model { get; set; }
		public int Id { get; set; }

		public virtual ICollection<AttributeMap> AttributeMapping { get; set; } = new List<AttributeMap>();
	}

	public class AttributeMap
	{
		public int ModelId { get; set; }
		public int MacroId { get; set; }
		public int? GeometryEntityId { get; set; }
		public int TimeFunctionId { get; set; }
		public int AttributeId { get; set; }
	}

	public class TimeFunction /* : IModelENtity */
	{
		public int Id { get; set; }

		public virtual ICollection<AttributeMap> AttributeMapping { get; set; } = new List<AttributeMap>();
	}
}
