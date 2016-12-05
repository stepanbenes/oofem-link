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
		}

		private Set(int id, IReadOnlyList<int> nodes, IReadOnlyList<int> elements)
		{
			Id = id;
			Nodes = nodes;
			Elements = elements;
		}

		public Set WithNodes(params int[] nodeIds) => new Set(Id, nodeIds, Elements);
		public Set WithElements(params int[] elementIds) => new Set(Id, Nodes, elementIds);

		public int Id { get; }
		public IReadOnlyList<int> Nodes { get; }
		public IReadOnlyList<int> Elements { get; }
	}
}
