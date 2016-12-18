using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Common.Enumerations;
using OofemLink.Common.OofemNames;

namespace OofemLink.Data.Entities
{
	public abstract class TimeFunction : IModelEntity
	{
		public int ModelId { get; set; }
		public int Id { get; set; }

		public virtual Model Model { get; set; }
				
		public virtual ICollection<TimeFunctionValue> Values { get; set; } = new List<TimeFunctionValue>();

		public abstract string Name { get; }
	}

	public class ConstantFunction : TimeFunction
	{
		public override string Name => TimeFunctionNames.ConstantFunction;
		public double ConstantValue { get; set; }
	}

	public class PeakFunction : TimeFunction
	{
		public override string Name => TimeFunctionNames.PeakFunction;
	}

	public class PiecewiseLinFunction : TimeFunction
	{
		public override string Name => TimeFunctionNames.PiecewiseLinFunction;
	}
}
