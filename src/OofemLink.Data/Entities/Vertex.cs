using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.Entities
{
	public class Vertex : ModelEntity
	{
		public int MacroId { get; set; }
		public virtual Macro Macro { get; set; }

		public double X { get; set; }
		public double Y { get; set; }
		public double Z { get; set; }

		public virtual ICollection<VertexNodeMap> VertexNodeMap { get; set; } = new List<VertexNodeMap>();
	}
}
