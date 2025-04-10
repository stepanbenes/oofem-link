﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Common.Enumerations;

namespace OofemLink.Data.DataTransferObjects
{
	public class CurveDto
	{
		public int Id { get; set; }
		public CurveType Type { get; set; }
		public IReadOnlyList<int> VertexIds { get; set; }
    }
}
