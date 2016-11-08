using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.Entities
{
    public class MacroAttribute : IModelEntity
    {
		public int ModelId { get; set; }
		public virtual Model Model { get; set; }
		public int Id { get; set; }

		public virtual ICollection<AttributeMapping> AttributeMappings { get; set; } = new List<AttributeMapping>();
	}

	public class AttributeMapping
	{
		public int ModelId { get; set; }
		public int MacroId { get; set; }
		public int AttributeId { get; set; }
		public int TimeFunctionId { get; set; }

		public virtual Model Model { get; set; }
		public virtual Macro Macro { get; set; }
		public virtual MacroAttribute Attribute { get; set; }
		public virtual TimeFunction TimeFunction { get; set; }
	}

	public class TimeFunction /* : IModelEntity */
	{
		public int Id { get; set; }

		public virtual ICollection<AttributeMapping> AttributeMappings { get; set; } = new List<AttributeMapping>();
	}
}
