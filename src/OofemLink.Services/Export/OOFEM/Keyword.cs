using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Services.Export.OOFEM
{
    public static class Keyword
    {
		public const string node = nameof(node);
		public const string coords = nameof(coords);
		public const string nodes = nameof(nodes);
		public const string material = nameof(material);
		public const string set = nameof(set);
		public const string elements = nameof(elements);
		public const string elementedges = nameof(elementedges);
		public const string allElements = nameof(allElements);

		public const string nsteps = nameof(nsteps);
		public const string nmodules = nameof(nmodules);
		public const string domain = nameof(domain);

		public const string ndofman = nameof(ndofman);
		public const string nelem = nameof(nelem);
		public const string ncrosssect = nameof(ncrosssect);
		public const string nmat = nameof(nmat);
		public const string nbc = nameof(nbc);
		public const string nic = nameof(nic);
		public const string nltf = nameof(nltf);
		public const string nset = nameof(nset);

		public const string loadTimeFunction = nameof(loadTimeFunction);
		public const string nPoints = nameof(nPoints);
	}
}
