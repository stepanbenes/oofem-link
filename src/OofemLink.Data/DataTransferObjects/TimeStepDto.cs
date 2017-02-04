using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.DataTransferObjects
{
    public class TimeStepDto
    {
		public long Id { get; set; }
		public int Number { get; set; }
		public double? Time { get; set; }
	}
}
