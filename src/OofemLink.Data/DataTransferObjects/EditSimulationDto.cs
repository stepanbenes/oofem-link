using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Common.Enumerations;

namespace OofemLink.Data.DataTransferObjects
{
    public class EditSimulationDto
    {
		public int ProjectId { get; set; }

		public SimulationState State { get; set; }

		public string TaskName { get; set; }
		public ModelDimensions DimensionFlags { get; set; }
		public bool ZAxisUp { get; set; }
	}
}
