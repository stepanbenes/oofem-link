using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Data.ModelEntities
{
    public class Model
    {
		public int Id { get; set; }

		//public virtual int SimulationId { get; }
		//public virtual Simulation Simulation { get; }

		public virtual ICollection<Macro> Macros { get; set; } = new List<Macro>();
	}
}
