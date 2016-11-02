using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Data.Entities;

namespace OofemLink.Data
{
    public class SurfaceElementMap
    {
		public int ModelId { get; set; }
		public int SurfaceId { get; set; }
		public virtual Surface Surface { get; set; }

		public int MeshId { get; set; }
		public int ElementId { get; set; }
		public virtual Element Element { get; set; }

		public byte Rank { get; set; }
	}
}
