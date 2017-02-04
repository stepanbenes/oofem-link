using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.MeshEntities
{
	public struct ElementSurface : IEquatable<ElementSurface>, IComparable<ElementSurface>
	{
		public ElementSurface(int elementId, short surfaceRank)
		{
			ElementId = elementId;
			SurfaceRank = (surfaceRank != 0) ? surfaceRank : (short)1; // 0 is special case for single-surface elements, change it to 1
		}

		public int ElementId { get; }
		public short SurfaceRank { get; }

		public bool Equals(ElementSurface other) => this.ElementId == other.ElementId && this.SurfaceRank == other.SurfaceRank;
		public override bool Equals(object obj) => (obj is ElementSurface) ? this.Equals((ElementSurface)obj) : false;
		public override int GetHashCode() => unchecked((17 * 23 + ElementId.GetHashCode()) * 23 + SurfaceRank.GetHashCode());
		public override string ToString() => ElementId + " " + SurfaceRank;

		public int CompareTo(ElementSurface other)
		{
			int elementIdComparison = this.ElementId.CompareTo(other.ElementId);
			if (elementIdComparison == 0)
				return this.SurfaceRank.CompareTo(other.SurfaceRank);
			return elementIdComparison;
		}
	}
}
