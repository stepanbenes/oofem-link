using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Business.Dto
{
	public class CurveDto
	{
		public int Id { get; set; }
		public IEnumerable<int> VertexIds { get; set; }
    }
}
