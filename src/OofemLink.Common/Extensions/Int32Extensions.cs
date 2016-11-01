using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Common.Extensions
{
    public static class Int32Extensions
    {
		public static uint BitCount(this int number)
		{
			uint v = (uint)number;
			v = v - ((v >> 1) & 0x55555555); // reuse input as temporary
			v = (v & 0x33333333) + ((v >> 2) & 0x33333333); // temp
			uint c = ((v + (v >> 4) & 0xF0F0F0F) * 0x1010101) >> 24; // count
			return c;
		}
	}
}
