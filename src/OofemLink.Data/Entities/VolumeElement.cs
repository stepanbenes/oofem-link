using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.Entities
{
    public class VolumeElement
    {
		public int ModelId { get; set; }
		public int VolumeId { get; set; }

		public int MeshId { get; set; }
		public int ElementId { get; set; }

		public virtual Model Model { get; set; }
		public virtual Mesh Mesh { get; set; }
		public virtual Volume Volume { get; set; }
		public virtual Element Element { get; set; }
	}
}
