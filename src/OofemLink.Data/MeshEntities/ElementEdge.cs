using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.MeshEntities
{
	public struct ElementEdge
	{
		public ElementEdge(int elementId, short edgeRank)
		{
			ElementId = elementId;
			EdgeRank = (edgeRank != 0) ? edgeRank : (short)1; // 0 is special case for single-edge elements, change it to 1
		}
		public int ElementId { get; }
		public short EdgeRank { get; }
	}
}
