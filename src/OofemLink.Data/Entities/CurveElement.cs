using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.Entities
{
	/// <summary>
	/// Curve to element edge mapping
	/// </summary>
    public class CurveElement
    {
		public int ModelId { get; set; }
		public int CurveId { get; set; }

		public int MeshId { get; set; }
		public int ElementId { get; set; }

		public virtual Model Model { get; set; }
		public virtual Mesh Mesh { get; set; }
		public virtual Curve Curve { get; set; }
		public virtual Element Element { get; set; }

		public short Rank { get; set; }
	}
}
