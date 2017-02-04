using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.MeshEntities
{
	public class PartialAttributeApplication
	{
		public PartialAttributeApplication(int attributeId, int elementId, double? relativeStart, double? relativeEnd)
		{
			AttributeId = attributeId;
			ElementId = elementId;
			RelativeStart = relativeStart;
			RelativeEnd = relativeEnd;
		}
		public int AttributeId { get; }
		public int ElementId { get; }
		public double? RelativeStart { get; }
		public double? RelativeEnd { get; }
	}
}
