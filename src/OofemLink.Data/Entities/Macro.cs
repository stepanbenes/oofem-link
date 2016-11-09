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

		public virtual ICollection<MacroCurveMapping> Curves { get; set; } = new List<MacroCurveMapping>();
		public virtual ICollection<MacroSurfaceMapping> Surfaces { get; set; } = new List<MacroSurfaceMapping>();
		public virtual ICollection<MacroVolumeMapping> Volumes { get; set; } = new List<MacroVolumeMapping>();

		public virtual ICollection<MacroInternalCurveMapping> InternalCurves { get; set; } = new List<MacroInternalCurveMapping>();
		public virtual ICollection<MacroInternalVertexMapping> InternalVertices { get; set; } = new List<MacroInternalVertexMapping>();
		public virtual ICollection<MacroOpeningCurveMapping> OpeningCurves { get; set; } = new List<MacroOpeningCurveMapping>();


		public virtual ICollection<AttributeMapping> Attributes { get; set; } = new List<AttributeMapping>();
	}
}
