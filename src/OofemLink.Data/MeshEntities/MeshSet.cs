using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.MeshEntities
{
    public class Set
    {
		public Set()
		{
			Nodes = Array.Empty<int>();
			Elements = Array.Empty<int>();
			ElementEdges = Array.Empty<ElementEdge>();
			ElementSurfaces = Array.Empty<ElementSurface>();
		}

		private Set(IReadOnlyList<int> nodes, IReadOnlyList<int> elements, IReadOnlyList<ElementEdge> elementEdges, IReadOnlyList<ElementSurface> elementSurfaces)
		{
			Nodes = nodes;
			Elements = elements;
			ElementEdges = elementEdges;
			ElementSurfaces = elementSurfaces;
		}

		public Set WithNodes(IReadOnlyList<int> nodeIds) => new Set(nodeIds, Elements, ElementEdges, ElementSurfaces);
		public Set WithElements(IReadOnlyList<int> elementIds) => new Set(Nodes, elementIds, ElementEdges, ElementSurfaces);
		public Set WithElementEdges(IReadOnlyList<ElementEdge> elementEdges) => new Set(Nodes, Elements, elementEdges, ElementSurfaces);
		public Set WithElementSurfaces(IReadOnlyList<ElementSurface> elementSurfaces) => new Set(Nodes, Elements, ElementEdges, elementSurfaces);

		public IReadOnlyList<int> Nodes { get; }
		public IReadOnlyList<int> Elements { get; }
		public IReadOnlyList<ElementEdge> ElementEdges { get; }
		public IReadOnlyList<ElementSurface> ElementSurfaces { get; }
	}
}
