using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Common.Enumerations;

namespace OofemLink.Data.DataTransferObjects
{
    public class AttributeDto
    {
		public int Id { get; set; }

		public AttributeType Type { get; set; }

		public string Name { get; set; }

		public string Parameters { get; set; }

		public int TimeFunctionId { get; set; }

		public IReadOnlyList<int> ParentAttributeIds { get; set; }
		public IReadOnlyList<int> ChildAttributeIds { get; set; }
	}
}
