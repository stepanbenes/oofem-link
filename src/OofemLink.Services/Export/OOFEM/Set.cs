using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Services.Export.OOFEM
{
    public class Set
    {
		public Set()
		{
			Nodes = Array.Empty<int>();
			Elements = Array.Empty<int>();
			ElementEdges = Array.Empty<KeyValuePair<int, short>>();
			ElementSurfaces = Array.Empty<KeyValuePair<int, short>>();
		}

		private Set(IReadOnlyList<int> nodes, IReadOnlyList<int> elements, IReadOnlyList<KeyValuePair<int, short>> elementEdges, IReadOnlyList<KeyValuePair<int, short>> elementSurfaces)
		{
			Nodes = nodes;
			Elements = elements;
			ElementEdges = elementEdges;
			ElementSurfaces = elementSurfaces;
		}

		public Set WithNodes(params int[] nodeIds) => new Set(nodeIds, Elements, ElementEdges, ElementSurfaces);
		public Set WithElements(params int[] elementIds) => new Set(Nodes, elementIds, ElementEdges, ElementSurfaces);
		public Set WithElementEdges(params KeyValuePair<int, short>[] elementEdgeIdPairs) => new Set(Nodes, Elements, elementEdgeIdPairs, ElementSurfaces);
		public Set WithElementSurfaces(params KeyValuePair<int, short>[] elementSurfaceIdPairs) => new Set(Nodes, Elements, ElementEdges, elementSurfaceIdPairs);

		// TODO: use SortedSet<> class

		public IReadOnlyList<int> Nodes { get; }
		public IReadOnlyList<int> Elements { get; }
		public IReadOnlyList<KeyValuePair<int, short>> ElementEdges { get; }
		public IReadOnlyList<KeyValuePair<int, short>> ElementSurfaces { get; }
	}
}
