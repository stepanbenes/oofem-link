using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.Entities
{
    public abstract class MeshEntity
    {
		public int MeshId { get; set; }
		public virtual Mesh Mesh { get; set; }
		public int Id { get; set; }
	}
}
