using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Common.Enumerations;

namespace OofemLink.Data.Entities
{
    public class Element : MeshEntity
    {
		public CellType Type { get; set; }
		public int LocalNumber { get; set; }

		public virtual ICollection<ElementNode> ElementNodes { get; set; } = new List<ElementNode>();
		public virtual ICollection<CurveElement> Edges { get; set; } = new List<CurveElement>();
		public virtual ICollection<SurfaceElement> Faces { get; set; } = new List<SurfaceElement>();

		public virtual ICollection<VolumeElement> Volumes { get; set; } = new List<VolumeElement>();
	}
}
