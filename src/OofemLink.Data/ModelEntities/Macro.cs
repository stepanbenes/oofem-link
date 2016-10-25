using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.ModelEntities
{
    public class Macro : ModelEntity
    {
		public virtual ICollection<Vertex> Vertices { get; set; } = new List<Vertex>();
		public virtual ICollection<Curve> Curves { get; set; } = new List<Curve>();
		public virtual ICollection<Surface> Surfaces { get; set; } = new List<Surface>();

		// TODO: add attributes: BoundaryConditions, Materials, CrossSections, LoadTimeFunctions, ...
	}
}
