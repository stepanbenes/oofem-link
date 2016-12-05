using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.Entities
{
    public class TimeFunctionValue
    {
		public int ModelId { get; set; }
		public int TimeFunctionId { get; set; }
		public long TimeStepId { get; set; }
		
		public virtual Model Model { get; set; }
		public virtual TimeFunction TimeFunction { get; set; }
		public virtual TimeStep TimeStep { get; set; }

		public double Value { get; set; }
	}
}
