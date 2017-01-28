using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Common.Enumerations;

namespace OofemLink.Data.DataTransferObjects
{
    public class ElementDto
    {
		public int Id { get; set; }
		public CellType Type { get; set; }

		public IReadOnlyList<int> NodeIds { get; set; }
	}
}
