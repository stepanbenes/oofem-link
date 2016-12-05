using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Common.Enumerations;

namespace OofemLink.Data.Entities
{
	public class TimeFunction : IModelEntity
	{
		public int ModelId { get; set; }
		public int Id { get; set; }
		public TimeFunctionType Type { get; set; }

		public virtual Model Model { get; set; }

		public virtual ICollection<TimeFunctionValue> Values { get; set; } = new List<TimeFunctionValue>();
	}
}
