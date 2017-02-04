using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.MeshEntities
{
    public class MeshEntitySet
    {
		public MeshEntitySet()
		{
			Nodes = Array.Empty<int>();
			Elements = Array.Empty<int>();
			ElementEdges = Array.Empty<ElementEdge>();
			ElementSurfaces = Array.Empty<ElementSurface>();
		}

		private MeshEntitySet(IReadOnlyList<int> nodes, IReadOnlyList<int> elements, IReadOnlyList<ElementEdge> elementEdges, IReadOnlyList<ElementSurface> elementSurfaces)
		{
			Nodes = nodes;
			Elements = elements;
			ElementEdges = elementEdges;
			ElementSurfaces = elementSurfaces;
		}

		public MeshEntitySet WithNodes(IReadOnlyList<int> nodeIds) => new MeshEntitySet(nodeIds, Elements, ElementEdges, ElementSurfaces);
		public MeshEntitySet WithElements(IReadOnlyList<int> elementIds) => new MeshEntitySet(Nodes, elementIds, ElementEdges, ElementSurfaces);
		public MeshEntitySet WithElementEdges(IReadOnlyList<ElementEdge> elementEdges) => new MeshEntitySet(Nodes, Elements, elementEdges, ElementSurfaces);
		public MeshEntitySet WithElementSurfaces(IReadOnlyList<ElementSurface> elementSurfaces) => new MeshEntitySet(Nodes, Elements, ElementEdges, elementSurfaces);

		// TODO: use immutable collections and optimize With method calls

		public IReadOnlyList<int> Nodes { get; }
		public IReadOnlyList<int> Elements { get; }
		public IReadOnlyList<ElementEdge> ElementEdges { get; }
		public IReadOnlyList<ElementSurface> ElementSurfaces { get; }
	}
}
