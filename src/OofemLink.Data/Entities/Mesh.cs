using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.Entities
{
    public class Mesh
    {
		public int Id { get; set; }

		public int ModelId { get; set; }
		public virtual Model Model { get; set; }

		public virtual ICollection<Node> Nodes { get; set; } = new List<Node>();
		public virtual ICollection<Element> Elements { get; set; } = new List<Element>();

		public virtual ICollection<VertexNode> VertexNodes { get; set; } = new List<VertexNode>();
		public virtual ICollection<CurveElement> CurveElements { get; set; } = new List<CurveElement>();
		public virtual ICollection<SurfaceElement> SurfaceElements { get; set; } = new List<SurfaceElement>();
		public virtual ICollection<VolumeElement> VolumeElements { get; set; } = new List<VolumeElement>();
		public virtual ICollection<CurveNode> CurveNodes { get; set; } = new List<CurveNode>();
	}
}
