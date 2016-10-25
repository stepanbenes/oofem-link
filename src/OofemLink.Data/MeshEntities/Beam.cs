using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.MeshEntities
{
    public class Beam : Element
    {
		public int StartNodeId { get; set; }
		public int EndNodeId { get; set; }
	}
}
