using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.DbEntities
{
	public abstract class AttributeGeometryMapping
	{
		public long Id { get; set; }

		public int ModelId { get; set; }
		public int AttributeId { get; set; }

		public virtual Model Model { get; set; }
		public virtual ModelAttribute Attribute { get; set; }
	}

	public class VertexAttribute : AttributeGeometryMapping
	{
		public int VertexId { get; set; }
		public virtual Vertex Vertex { get; set; }
	}

	public class CurveAttribute : AttributeGeometryMapping
	{
		public int CurveId { get; set; }
		public virtual Curve Curve { get; set; }
		public int MacroId { get; set; }
		public virtual Macro Macro { get; set; }

		public double? RelativeStart { get; set; }
		public double? RelativeEnd { get; set; }
	}

	public class SurfaceAttribute : AttributeGeometryMapping
	{
		public int SurfaceId { get; set; }
		public virtual Surface Surface { get; set; }
		public int MacroId { get; set; }
		public virtual Macro Macro { get; set; }
	}

	public class VolumeAttribute : AttributeGeometryMapping
	{
		public int VolumeId { get; set; }
		public virtual Volume Volume { get; set; }
		public int MacroId { get; set; }
		public virtual Macro Macro { get; set; }
	}
}
