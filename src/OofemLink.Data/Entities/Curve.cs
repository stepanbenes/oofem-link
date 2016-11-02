using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.Entities
{
    public class Curve : ModelEntity
    {
		public virtual int MacroId { get; set; }
		public virtual Macro Macro { get; set; }

		public virtual ICollection<CurveElementMap> CurveElementMap { get; set; } = new List<CurveElementMap>();
	}
}
