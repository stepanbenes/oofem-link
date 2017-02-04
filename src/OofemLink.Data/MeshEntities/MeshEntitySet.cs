using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.MeshEntities
{
    public class MeshEntitySet
    {
		public MeshEntitySet()
		{
			Nodes = ImmutableSortedSet<int>.Empty;
			Elements = ImmutableSortedSet<int>.Empty;
			ElementEdges = ImmutableSortedSet<ElementEdge>.Empty;
			ElementSurfaces = ImmutableSortedSet<ElementSurface>.Empty;
		}

		private MeshEntitySet(ImmutableSortedSet<int> nodes, ImmutableSortedSet<int> elements, ImmutableSortedSet<ElementEdge> elementEdges, ImmutableSortedSet<ElementSurface> elementSurfaces)
		{
			Nodes = nodes;
			Elements = elements;
			ElementEdges = elementEdges;
			ElementSurfaces = elementSurfaces;
		}

		public MeshEntitySet WithNodes(IEnumerable<int> nodeIds) => new MeshEntitySet(ImmutableSortedSet.CreateRange(nodeIds), Elements, ElementEdges, ElementSurfaces);
		public MeshEntitySet WithElements(IEnumerable<int> elementIds) => new MeshEntitySet(Nodes, ImmutableSortedSet.CreateRange(elementIds), ElementEdges, ElementSurfaces);
		public MeshEntitySet WithElementEdges(IEnumerable<ElementEdge> elementEdges) => new MeshEntitySet(Nodes, Elements, ImmutableSortedSet.CreateRange(elementEdges), ElementSurfaces);
		public MeshEntitySet WithElementSurfaces(IEnumerable<ElementSurface> elementSurfaces) => new MeshEntitySet(Nodes, Elements, ElementEdges, ImmutableSortedSet.CreateRange(elementSurfaces));


		public MeshEntitySet WithNodes(ImmutableSortedSet<int> nodeIds) => new MeshEntitySet(nodeIds, Elements, ElementEdges, ElementSurfaces);
		public MeshEntitySet WithElements(ImmutableSortedSet<int> elementIds) => new MeshEntitySet(Nodes, elementIds, ElementEdges, ElementSurfaces);
		public MeshEntitySet WithElementEdges(ImmutableSortedSet<ElementEdge> elementEdges) => new MeshEntitySet(Nodes, Elements, elementEdges, ElementSurfaces);
		public MeshEntitySet WithElementSurfaces(ImmutableSortedSet<ElementSurface> elementSurfaces) => new MeshEntitySet(Nodes, Elements, ElementEdges, elementSurfaces);

		public ImmutableSortedSet<int> Nodes { get; }
		public ImmutableSortedSet<int> Elements { get; }
		public ImmutableSortedSet<ElementEdge> ElementEdges { get; }
		public ImmutableSortedSet<ElementSurface> ElementSurfaces { get; }
	}
}
