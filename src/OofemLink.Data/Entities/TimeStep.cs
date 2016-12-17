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

		public int Number { get; set; }

		public double? Time { get; set; }

		public int SimulationId { get; set; }
		public Simulation Simulation { get; set; }
	}
}
