using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.ModelEntities.Attributes
{
    public abstract class Attribute : ModelEntity
    {
		public virtual int MacroId { get; set; }
		public virtual Macro Macro { get; set; }
	}
}
