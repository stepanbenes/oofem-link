using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.Entities
{
    public class Model
    {
		public int Id { get; set; }

		public int SimulationId { get; set; }
		public virtual Simulation Simulation { get; set; }

		public virtual ICollection<Mesh> Meshes { get; set; } = new List<Mesh>();

		public virtual ICollection<Vertex> Vertices { get; set; } = new List<Vertex>();
		public virtual ICollection<Curve> Curves { get; set; } = new List<Curve>();
		public virtual ICollection<Surface> Surfaces { get; set; } = new List<Surface>();
		public virtual ICollection<Volume> Volumes { get; set; } = new List<Volume>();

		public virtual ICollection<Macro> Macros { get; set; } = new List<Macro>();

		public virtual ICollection<ModelAttribute> Attributes { get; set; } = new List<ModelAttribute>();
	}
}
