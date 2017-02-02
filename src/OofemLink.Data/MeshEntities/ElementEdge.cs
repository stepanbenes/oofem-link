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
			EdgeRank = edgeRank;
		}
		public int ElementId { get; }
		public short EdgeRank { get; }
	}
}
