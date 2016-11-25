using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Common.Enumerations
{
    public enum AttributeType : byte
    {
		Material = 1,
		CrossSection,
		BoundaryCondition,
		InitialCondition,
    }
}
