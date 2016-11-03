using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Common.Enumerations;

namespace OofemLink.Business.Dto
{
    public class ViewSimulationDto
    {
		public int Id { get; set; }
		public int ProjectId { get; set; }
		public string ProjectName { get; set; }
		public SimulationState State { get; set; }
		public string TaskName { get; set; }
		public ModelDimensions DimensionFlags { get; set; }
		public bool ZAxisUp { get; set; }

		public int? ModelId { get; set; }
	}
}
