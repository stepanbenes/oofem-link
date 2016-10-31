using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Common.Enumerations;
using OofemLink.Data.ModelEntities;

namespace OofemLink.Data
{
	public class Simulation
	{
		public int Id { get; set; }

		public string TaskName { get; set; }

		public int ProjectId { get; set; }
		public virtual Project Project { get; set; }

		public int? ModelId { get; set; }
		public virtual Model Model { get; set; }

		public SimulationState State { get; set; }
	}
}
