using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.Entities
{
    public class TimeStep
    {
		/// <summary>
		/// unique identifier
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// time step value
		/// </summary>
		public double Number { get; set; }
    }
}
