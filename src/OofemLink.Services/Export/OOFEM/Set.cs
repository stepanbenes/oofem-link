using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Services.Export.OOFEM
{
    public class Set
    {
		public Set(int id)
		{
			Id = id;
			Nodes = Array.Empty<int>();
			Elements = Array.Empty<int>();
			ElementEdges = Array.Empty<KeyValuePair<int, short>>();
			ElementSurfaces = Array.Empty<KeyValuePair<int, short>>();
		}

		private Set(int id, IReadOnlyList<int> nodes, IReadOnlyList<int> elements, IReadOnlyList<KeyValuePair<int, short>> elementEdges, IReadOnlyList<KeyValuePair<int, short>> elementSurfaces)
		{
			Id = id;
			Nodes = nodes;
			Elements = elements;
			ElementEdges = elementEdges;
			ElementSurfaces = elementSurfaces;
		}

		public Set WithNodes(params int[] nodeIds) => new Set(Id, nodeIds, Elements, ElementEdges, ElementSurfaces);
		public Set WithElements(params int[] elementIds) => new Set(Id, Nodes, elementIds, ElementEdges, ElementSurfaces);
		public Set WithElementEdges(params KeyValuePair<int, short>[] elementEdgeIdPairs) => new Set(Id, Nodes, Elements, elementEdgeIdPairs, ElementSurfaces);
		public Set WithElementSurfaces(params KeyValuePair<int, short>[] elementSurfaceIdPairs) => new Set(Id, Nodes, Elements, ElementEdges, elementSurfaceIdPairs);

		public int Id { get; }
		public IReadOnlyList<int> Nodes { get; }
		public IReadOnlyList<int> Elements { get; }
		public IReadOnlyList<KeyValuePair<int, short>> ElementEdges { get; }
		public IReadOnlyList<KeyValuePair<int, short>> ElementSurfaces { get; }

		public override int GetHashCode() => Id;

		public override bool Equals(object obj)
		{
			var other = obj as Set;
			if (other == null)
				return false;
			return this.Id == other.Id;
		}
	}
}
