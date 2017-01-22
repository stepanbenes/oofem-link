using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Common.Enumerations;

namespace OofemLink.Data.Entities
{
    public abstract class GeometryEntity : IModelEntity
    {
		public int ModelId { get; set; }
		public virtual Model Model { get; set; }
		public int Id { get; set; }
	}

	public class Vertex : GeometryEntity
	{
		public double X { get; set; }
		public double Y { get; set; }
		public double Z { get; set; }

		public virtual ICollection<VertexNode> VertexNodes { get; set; } = new List<VertexNode>();
		public virtual ICollection<VertexCurve> VertexCurves { get; set; } = new List<VertexCurve>();

		public virtual ICollection<VertexAttribute> VertexAttributes { get; set; } = new List<VertexAttribute>();
	}

	public class Curve : GeometryEntity
	{
		public CurveType Type { get; set; }

		public virtual ICollection<VertexCurve> CurveVertices { get; set; } = new List<VertexCurve>();
		public virtual ICollection<SurfaceCurve> CurveSurfaces { get; set; } = new List<SurfaceCurve>();
		public virtual ICollection<CurveElement> CurveElements { get; set; } = new List<CurveElement>();
		public virtual ICollection<CurveNode> CurveNodes { get; set; } = new List<CurveNode>();

		public virtual ICollection<CurveAttribute> CurveAttributes { get; set; } = new List<CurveAttribute>();
	}

	public class Surface : GeometryEntity
	{
		public SurfaceType Type { get; set; }

		public virtual ICollection<SurfaceCurve> SurfaceCurves { get; set; } = new List<SurfaceCurve>();
		public virtual ICollection<SurfaceVolume> SurfaceVolumes { get; set; } = new List<SurfaceVolume>();
		public virtual ICollection<SurfaceElement> SurfaceElements { get; set; } = new List<SurfaceElement>();

		public virtual ICollection<SurfaceAttribute> SurfaceAttributes { get; set; } = new List<SurfaceAttribute>();
	}

	public class Volume : GeometryEntity
	{
		public virtual ICollection<SurfaceVolume> VolumeSurfaces { get; set; } = new List<SurfaceVolume>();
		public virtual ICollection<VolumeElement> VolumeElements { get; set; } = new List<VolumeElement>();

		public virtual ICollection<VolumeAttribute> VolumeAttributes { get; set; } = new List<VolumeAttribute>();
	}

	public class VertexCurve
	{
		public int ModelId { get; set; }
		public int VertexId { get; set; }
		public int CurveId { get; set; }

		public virtual Model Model { get; set; }
		public virtual Vertex Vertex { get; set; }
		public virtual Curve Curve { get; set; }

		public short Rank { get; set; }
	}

	public class SurfaceCurve
	{
		public int ModelId { get; set; }
		public int CurveId { get; set; }
		public int SurfaceId { get; set; }

		public virtual Model Model { get; set; }
		public virtual Curve Curve { get; set; }
		public virtual Surface Surface { get; set; }

		public short Rank { get; set; }
		public bool IsInversed { get; set; }
	}

	public class SurfaceVolume
	{
		public int ModelId { get; set; }
		public int SurfaceId { get; set; }
		public int VolumeId { get; set; }

		public virtual Model Model { get; set; }
		public virtual Surface Surface { get; set; }
		public virtual Volume Volume { get; set; }

		public short Rank { get; set; }
		public bool IsInversed { get; set; }
	}
}
