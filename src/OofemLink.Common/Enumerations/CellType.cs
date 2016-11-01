using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Common.Enumerations
{
    public enum CellType : byte
    {
		Undefined = 0,
		// 0D
		Point = 1,
		// 1D
		LineLinear = 3,
		LineQuadratic = 21,
		// 2D
		TriangleLinear = 5,
		TriangleQuadratic = 22,
		QuadLinear = 9,
		QuadQuadratic = 23,
		// 3D
		TetraLinear = 10,
		TetraQuadratic = 24,
		WedgeLinear = 13,
		WedgeQuadratic = 26,
		HexaLinear = 12,
		HexaQuadratic = 25,
	}
}
