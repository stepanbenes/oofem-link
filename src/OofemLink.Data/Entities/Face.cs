using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.Entities
{
	/// <summary>
	/// Surface-Element mapping
	/// </summary>
    public class Face
    {
		public int ModelId { get; set; }
		public int SurfaceId { get; set; }

		public int MeshId { get; set; }
		public int ElementId { get; set; }

		public virtual Model Model { get; set; }
		public virtual Mesh Mesh { get; set; }
		public virtual Surface Surface { get; set; }
		public virtual Element Element { get; set; }

		public short Rank { get; set; }
	}
}
