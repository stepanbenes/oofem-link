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

	public class MacroCurve : MacroGeometryMapping
	{
		public int CurveId { get; set; }
		public virtual Curve Curve { get; set; }
	}

	public class MacroSurface : MacroGeometryMapping
	{
		public int SurfaceId { get; set; }
		public virtual Surface Surface { get; set; }
	}

	public class MacroVolume : MacroGeometryMapping
	{
		public int VolumeId { get; set; }
		public virtual Volume Volume { get; set; }
	}

	public class MacroInternalCurve : MacroGeometryMapping
	{
		public int InternalCurveId { get; set; }
		public virtual Curve InternalCurve { get; set; }
		public short Rank { get; set; }
		public bool IsInversed { get; set; }
	}

	public class MacroInternalVertex : MacroGeometryMapping
	{
		public int InternalVertexId { get; set; }
		public virtual Vertex InternalVertex { get; set; }
	}

	public class MacroOpeningCurve : MacroGeometryMapping
	{
		public int OpeningCurveId { get; set; }
		public virtual Curve OpeningCurve { get; set; }
		public short Rank { get; set; }
		public bool IsInversed { get; set; }
	}
}
