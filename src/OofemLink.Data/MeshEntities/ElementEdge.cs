using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.MeshEntities
{
	public struct ElementEdge : IEquatable<ElementEdge>, IComparable<ElementEdge>
	{
		public ElementEdge(int elementId, short edgeRank)
		{
			ElementId = elementId;
			EdgeRank = (edgeRank != 0) ? edgeRank : (short)1; // 0 is special case for single-edge elements, change it to 1
		}

		public int ElementId { get; }
		public short EdgeRank { get; }

		public bool Equals(ElementEdge other) => this.ElementId == other.ElementId && this.EdgeRank == other.EdgeRank;
		public override bool Equals(object obj) => (obj is ElementEdge) ? this.Equals((ElementEdge)obj) : false;
		public override int GetHashCode() => unchecked((17 * 23 + ElementId.GetHashCode()) * 23 + EdgeRank.GetHashCode());
		public override string ToString() => ElementId + " " + EdgeRank;

		public int CompareTo(ElementEdge other)
		{
			int elementIdComparison = this.ElementId.CompareTo(other.ElementId);
			if (elementIdComparison == 0)
				return this.EdgeRank.CompareTo(other.EdgeRank);
			return elementIdComparison;
		}
	}
}
