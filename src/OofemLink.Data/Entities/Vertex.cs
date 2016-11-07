using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.Entities
{
	public class Vertex : IModelEntity
	{
		public int ModelId { get; set; }
		public virtual Model Model { get; set; }

		public int Id { get; set; }

		public double X { get; set; }
		public double Y { get; set; }
		public double Z { get; set; }

		public virtual ICollection<VertexCurveMapping> Curves { get; set; } = new List<VertexCurveMapping>();
	}
}
