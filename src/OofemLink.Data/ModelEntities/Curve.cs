using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.ModelEntities
{
    public class Curve : ModelEntity
    {
		public virtual int MacroId { get; set; }
		public virtual Macro Macro { get; set; }
	}
}
