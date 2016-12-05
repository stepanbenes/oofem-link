using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Common.Enumerations;
using OofemLink.Data.Entities;

namespace OofemLink.Data.Entities
{
	public class Simulation
	{
		public int Id { get; set; }

		public int ProjectId { get; set; }
		public virtual Project Project { get; set; }

		public virtual ICollection<TimeStep> TimeSteps { get; set; } = new List<TimeStep>();

		public virtual ICollection<Model> Models { get; set; } = new List<Model>(); // should be only one or zero

		public SimulationState State { get; set; }

		public string TaskName { get; set; }
		public ModelDimensions DimensionFlags { get; set; }
		public bool ZAxisUp { get; set; }
	}
}
