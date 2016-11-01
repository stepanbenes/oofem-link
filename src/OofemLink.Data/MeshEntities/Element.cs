using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Common.Enumerations;

namespace OofemLink.Data.MeshEntities
{
    public class Element : MeshEntity
    {
		public CellType Type { get; set; }
		public int LocalNumber { get; set; }

		public virtual ICollection<ElementNode> ElementNodes { get; set; } = new List<ElementNode>();

		public virtual ICollection<CurveElementMap> CurveElementMap { get; set; } = new List<CurveElementMap>();
		public virtual ICollection<SurfaceElementMap> SurfaceElementMap { get; set; } = new List<SurfaceElementMap>();
	}
}
