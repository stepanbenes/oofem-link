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

		public virtual ICollection<VertexCurveMapping> Curves { get; set; } = new List<VertexCurveMapping>();

		public virtual ICollection<VertexNodeMapping> Nodes { get; set; } = new List<VertexNodeMapping>();
	}

	public class Curve : GeometryEntity
	{
		public CurveType Type { get; set; }

		public virtual ICollection<VertexCurveMapping> Vertices { get; set; } = new List<VertexCurveMapping>();
		public virtual ICollection<CurveSurfaceMapping> Surfaces { get; set; } = new List<CurveSurfaceMapping>();

		public virtual ICollection<Edge> Edges { get; set; } = new List<Edge>();
	}

	public class Surface : GeometryEntity
	{
		public SurfaceType Type { get; set; }

		public virtual ICollection<CurveSurfaceMapping> Curves { get; set; } = new List<CurveSurfaceMapping>();
		public virtual ICollection<SurfaceVolumeMapping> Volumes { get; set; } = new List<SurfaceVolumeMapping>();

		public virtual ICollection<Face> Faces { get; set; } = new List<Face>();
	}

	public class Volume : GeometryEntity
	{
		public virtual ICollection<SurfaceVolumeMapping> Surfaces { get; set; } = new List<SurfaceVolumeMapping>();

		public virtual ICollection<VolumeElementMapping> Elements { get; set; } = new List<VolumeElementMapping>();
	}

	public class VertexCurveMapping
	{
		public int ModelId { get; set; }
		public int VertexId { get; set; }
		public int CurveId { get; set; }

		public virtual Model Model { get; set; }
		public virtual Vertex Vertex { get; set; }
		public virtual Curve Curve { get; set; }

		public short Rank { get; set; }
	}

	public class CurveSurfaceMapping
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

	public class SurfaceVolumeMapping
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
