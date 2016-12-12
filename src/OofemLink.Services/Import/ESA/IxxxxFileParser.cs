using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OofemLink.Data.Entities;
using OofemLink.Common.Extensions;
using OofemLink.Common.Enumerations;
using OofemLink.Common;
using static System.FormattableString;

namespace OofemLink.Services.Import.ESA
{
	class IxxxxFileParser : AttributeFileParserBase
	{
		readonly int loadCaseNumber;

		public IxxxxFileParser(int loadCaseNumber, string location, string taskName, ILoggerFactory loggerFactory)
			: base(location, taskName, loggerFactory)
		{
			if (loadCaseNumber < 1 || loadCaseNumber > 9999)
				throw new ArgumentOutOfRangeException(nameof(loadCaseNumber), "Argument must be in range <1, 9999>");
			this.loadCaseNumber = loadCaseNumber;
		}

		public override string Extension => $"I{loadCaseNumber:D4}";

		public IEnumerable<ModelAttribute> Parse()
		{
			LogStart();

			using (var streamReader = File.OpenText(FileFullPath))
			{
				string line = streamReader.ReadLine(); // ignore first line ("LCx")
				while ((line = streamReader.ReadLine()) != null)
				{
					if (line == "")
						continue;
					if (line.Length != 160)
						throw new FormatException($"Wrong {Extension} file format. Each row (except the first) is expected to have exactly 160 characters.");

					LineTokens lineTokens = ParseLine(line);

					switch (lineTokens.ItemType)
					{
						case Codes.MULT:
							{
								double loadCaseCoefficient = ParseFloat64(lineTokens[3]);
								
								// TODO: handle loadCaseCoefficient

							}
							break;
						case Codes.OWN:
							{
								yield return parseDeadWeightAttribute(direction: lineTokens[1], coefficient: ParseFloat64(lineTokens[3]));
							}
							break;
						default:
							Logger.LogWarning("Ignoring token '{0}'", lineTokens.ItemType);
							break;
					}

					// TODO: generate time function for each load step and add it to attribute-macro mappings

				}
			}
		}

		#region Private methods

		private ModelAttribute parseDeadWeightAttribute(string direction, double coefficient)
		{
			double x = 0, y = 0, z = 0;
			switch (direction)
			{
				case Codes.XG:
					x = coefficient * PhysicalConstants.g; // TODO: check correctness
					break;
				case Codes.YG:
					y = coefficient * PhysicalConstants.g;
					break;
				case Codes.ZG:
					z = coefficient * PhysicalConstants.g;
					break;
				default:
					throw new NotSupportedException($"Unknown direction parameter '{direction}'");
			}

			var deadWeight = new ModelAttribute
			{
				Type = AttributeType.BoundaryCondition,
				Name = BoundaryConditionNames.DeadWeight,
				Parameters = Invariant($"components 6 {x} {y} {z} 0 0 0")
			};

			return deadWeight;
		}

		#endregion

		#region File codes

		private static class Codes
		{
			// File section names
			public const string MODEL = nameof(MODEL);
			public const string MULT = nameof(MULT);
			public const string OWN = nameof(OWN);

			// DEAD WEIGHT DIRECTIONS
			public const string XG = nameof(XG);
			public const string YG = nameof(YG);
			public const string ZG = nameof(ZG);
		}

		#endregion
	}
}
