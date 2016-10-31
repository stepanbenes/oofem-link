using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.ModelEntities
{
    public class Surface : ModelEntity
    {
		public int MacroId { get; set; }
		public virtual Macro Macro { get; set; }

		public virtual ICollection<SurfaceElementMap> SurfaceElementMap { get; set; } = new List<SurfaceElementMap>();
	}
}
