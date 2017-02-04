using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Common.Enumerations
{
    public enum AttributeType : byte
    {
		Material = 1,
		CrossSection = 2,
		BoundaryCondition = 3,
		InitialCondition = 4,
		LocalCoordinateSystem = 5,
		Spring = 6,
		Hinge = 7
	}
}
