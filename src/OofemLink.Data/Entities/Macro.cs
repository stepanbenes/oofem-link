using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.Entities
{
    public class Macro : IModelEntity
    {
		public int ModelId { get; set; }
		public virtual Model Model { get; set; }
		public int Id { get; set; }

		public virtual ICollection<MacroCurve> MacroCurves { get; set; } = new List<MacroCurve>();
		public virtual ICollection<MacroSurface> MacroSurfaces { get; set; } = new List<MacroSurface>();
		public virtual ICollection<MacroVolume> MacroVolumes { get; set; } = new List<MacroVolume>();

		public virtual ICollection<MacroInternalCurve> MacroInternalCurves { get; set; } = new List<MacroInternalCurve>();
		public virtual ICollection<MacroInternalVertex> MacroInternalVertices { get; set; } = new List<MacroInternalVertex>();
		public virtual ICollection<MacroOpeningCurve> MacroOpeningCurves { get; set; } = new List<MacroOpeningCurve>();
	}
}
