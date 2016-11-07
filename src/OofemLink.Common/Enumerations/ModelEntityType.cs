using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Common.Enumerations
{
    public enum ModelEntityType
    {
		Undefined = 0,
		Macro, // Group
		Vertex,
		Line,
		Surface,
		Volume,
    }
}
