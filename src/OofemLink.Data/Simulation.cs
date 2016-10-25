using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Common.Enumerations;

namespace OofemLink.Data
{
	public class Simulation
	{
		public int Id { get; set; }

		public virtual int ProjectId { get; set; }
		public virtual Project Project { get; set; }

		public SimulationState State { get; set; }
	}
}
