using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.MeshEntities
{
    public class Beam : Element
    {
		public int Node1Id { get; set; }
		public int Node2Id { get; set; }
	}
}
