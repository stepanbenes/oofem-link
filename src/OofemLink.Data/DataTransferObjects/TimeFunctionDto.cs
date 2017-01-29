using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Common.OofemNames;

namespace OofemLink.Data.DataTransferObjects
{
	public abstract class TimeFunctionDto
	{
		public int Id { get; set; }
		
		public abstract string Name { get; }
	}

	public class ConstantFunctionDto : TimeFunctionDto
	{
		public override string Name => TimeFunctionNames.ConstantFunction;
		public double ConstantValue { get; set; }
	}

	public class PeakFunctionDto : TimeFunctionDto
	{
		public override string Name => TimeFunctionNames.PeakFunction;
		public double Time { get; set; }
		public double Value { get; set; }
	}

	public class PiecewiseLinFunctionDto : TimeFunctionDto
	{
		public override string Name => TimeFunctionNames.PiecewiseLinFunction;
		public IReadOnlyList<double> Times { get; set; }
		public IReadOnlyList<double> Values { get; set; }
	}
}
