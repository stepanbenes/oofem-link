using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.DataTransferObjects
{
    public class MeshDto
    {
		public int Id { get; set; }
		public IReadOnlyList<NodeDto> Nodes { get; set; }
		public IReadOnlyList<ElementDto> Elements { get; set; }
	}
}
