using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Data.Entities;

namespace OofemLink.Data
{
    public class CurveElementMap
    {
		public int ModelId { get; set; }
		public int CurveId { get; set; }
		public Curve Curve { get; set; }

		public int MeshId { get; set; }
		public int ElementId { get; set; }
		public Element Element { get; set; }

		public byte Rank { get; set; }
	}
}
