using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.MeshEntities
{
	public struct ElementSurface
	{
		public ElementSurface(int elementId, short surfaceRank)
		{
			ElementId = elementId;
			SurfaceRank = (surfaceRank != 0) ? surfaceRank : (short)1; // 0 is special case for single-surface elements, change it to 1
		}
		public int ElementId { get; }
		public short SurfaceRank { get; }
	}
}
