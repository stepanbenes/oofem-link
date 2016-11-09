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
		public virtual ICollection<Edge> Edges { get; set; } = new List<Edge>();
		public virtual ICollection<Face> Faces { get; set; } = new List<Face>();

		public virtual ICollection<VolumeElementMapping> Volumes { get; set; } = new List<VolumeElementMapping>();
	}
}
