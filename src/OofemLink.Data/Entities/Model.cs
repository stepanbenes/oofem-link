using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.Entities
{
    public class Model
    {
		public int Id { get; set; }

		public virtual Simulation Simulation { get; set; }

		public virtual ICollection<Mesh> Meshes { get; set; } = new List<Mesh>();

		public virtual ICollection<Macro> Macros { get; set; } = new List<Macro>();
		public virtual ICollection<GeometryEntity> GeometryEntities { get; set; } = new List<GeometryEntity>();
	}
}
