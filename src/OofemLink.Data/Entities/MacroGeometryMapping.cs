using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.Entities
{
    public abstract class MacroGeometryMapping
    {
		public int ModelId { get; set; }
		public int MacroId { get; set; }

		public virtual Model Model { get; set; }
		public virtual Macro Macro { get; set; }
	}

	public class MacroBoundaryCurveMapping : MacroGeometryMapping
	{
		public int BoundaryCurveId { get; set; }
		public virtual Curve BoundaryCurve { get; set; }
		public short Rank { get; set; }
		public bool IsInversed { get; set; }
	}

	public class MacroInternalCurveMapping : MacroGeometryMapping
	{
		public int InternalCurveId { get; set; }
		public virtual Curve InternalCurve { get; set; }
		public short Rank { get; set; }
		public bool IsInversed { get; set; }
	}

	public class MacroInternalVertexMapping : MacroGeometryMapping
	{
		public int InternalVertexId { get; set; }
		public virtual Vertex InternalVertex { get; set; }
	}

	public class MacroOpeningCurveMapping : MacroGeometryMapping
	{
		public int OpeningCurveId { get; set; }
		public virtual Curve OpeningCurve { get; set; }
		public short Rank { get; set; }
		public bool IsInversed { get; set; }
	}

	public class MacroBoundarySurfaceMapping : MacroGeometryMapping
	{
		public int BoundarySurfaceId { get; set; }
		public virtual Surface BoundarySurface { get; set; }
		public short Rank { get; set; }
		public bool IsInversed { get; set; }
	}
}
