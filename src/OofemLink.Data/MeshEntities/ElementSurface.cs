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
			SurfaceRank = surfaceRank;
		}
		public int ElementId { get; }
		public short SurfaceRank { get; }
	}
}
