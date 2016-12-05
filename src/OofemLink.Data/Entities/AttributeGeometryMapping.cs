using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.Entities
{
	public abstract class AttributeGeometryMapping
	{
		public int ModelId { get; set; }
		public int AttributeId { get; set; }
		public int MacroId { get; set; }
		public int TimeFunctionId { get; set; }

		public virtual Model Model { get; set; }
		public virtual ModelAttribute Attribute { get; set; }
		public virtual Macro Macro { get; set; }
		public virtual TimeFunction TimeFunction { get; set; }
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
	}

	public class SurfaceAttribute : AttributeGeometryMapping
	{
		public int SurfaceId { get; set; }
		public virtual Surface Surface { get; set; }
	}

	public class VolumeAttribute : AttributeGeometryMapping
	{
		public int VolumeId { get; set; }
		public virtual Volume Volume { get; set; }
	}
}
