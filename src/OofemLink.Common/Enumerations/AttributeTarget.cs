using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Common.Enumerations
{
    public enum AttributeTarget : byte
    {
		Undefined = 0,
		Node = 1,
		Edge = 2,
		Surface = 3,
		Volume = 4
    }
}
