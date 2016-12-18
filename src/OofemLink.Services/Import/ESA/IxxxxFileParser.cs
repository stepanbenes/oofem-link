using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OofemLink.Data.Entities;
using OofemLink.Common.OofemNames;
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

			var lineBuffer = new Queue<LineTokens>();

			using (var streamReader = File.OpenText(FileFullPath))
			{
				string line = streamReader.ReadLine(); // ignore first line ("LCx")
				while ((line = streamReader.ReadLine()) != null)
				{
					if (line == "")
						continue;
					if (line.Length != 160)
						throw new FormatException($"Wrong {Extension} file format. Each row (except the first) is expected to have exactly 160 characters.");

					LineTokens currentLineTokens = ParseLineTokens(line);

					if (currentLineTokens.ItemType != "" && lineBuffer.Count > 0)
					{
						foreach (var attribute in parseSection(lineBuffer))
							yield return attribute;
						Debug.Assert(lineBuffer.Count == 0);
					}
					lineBuffer.Enqueue(currentLineTokens);
				}
			}

			foreach (var attribute in parseSection(lineBuffer)) // process rest of file
				yield return attribute;

			Debug.Assert(lineBuffer.Count == 0);
		}

		#region Private methods

		private IEnumerable<ModelAttribute> parseSection(Queue<LineTokens> lineBuffer)
		{
			Debug.Assert(lineBuffer.Count > 0);
			LineTokens sectionHeader = lineBuffer.Peek();
			switch (sectionHeader.ItemType)
			{
				case Codes.MULT:
					{
						lineBuffer.Dequeue();
						double loadCaseCoefficient = ParseFloat64(sectionHeader[3]);

						// TODO: handle loadCaseCoefficient
						Debug.Assert(loadCaseCoefficient == 1);

						while (lineBuffer.Count > 0)
						{
							string attributeDimension;
							List<LineTokens> attributeLines = dequeueLinesForDimensionType(lineBuffer, out attributeDimension);
							switch (attributeDimension)
							{
								case Codes.POIN:
									yield return parsePointAttribute(attributeLines);
									break;
								case Codes.LIN:
									yield return parseLineAttribute(attributeLines);
									break;
								case Codes.SURF:
									yield return parseSurfaceAttribute(attributeLines);
									break;
								default:
									throw new NotSupportedException($"Unknown dimension specifier '{attributeDimension}'");
							}
						}
					}
					break;
				case Codes.OWN:
					{
						yield return parseDeadWeightAttribute(lineBuffer);
					}
					break;
				default:
					Logger.LogWarning("Ignoring token '{0}'", sectionHeader.ItemType);
					lineBuffer.Clear();
					break;
			}

			// TODO: generate time function for each load step and add it to attribute-macro mappings
		}

		private List<LineTokens> dequeueLinesForDimensionType(Queue<LineTokens> lineBuffer, out string dimensionType)
		{
			var lines = new List<LineTokens>();
			dimensionType = null;
			while (lineBuffer.Count > 0)
			{
				var line = lineBuffer.Peek();
				if (dimensionType == null) // first iteration
				{
					dimensionType = line.DimensionType;
				}
				else if (line.DimensionType != "")
				{
					Debug.Assert(lines.Count > 0);
					return lines;
				}
				lines.Add(lineBuffer.Dequeue());
			}
			return lines; // return remaining
		}

		private ModelAttribute parseDeadWeightAttribute(Queue<LineTokens> lineBuffer)
		{
			LineTokens sectionHeader = lineBuffer.Dequeue();
			string direction = sectionHeader[1];
			double coefficient = ParseFloat64(sectionHeader[3]);
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

			while (lineBuffer.Count > 0)
			{
				var line = lineBuffer.Dequeue();
				// TODO: process parameters
				Logger.LogWarning($"Ignoring line with values in section {Codes.OWN}");
			}

			if (deadWeight.MacroAttributes.Count == 0)
			{
				deadWeight.AppliesToAllMacros = true;
			}

			return deadWeight;
		}

		private ModelAttribute parsePointAttribute(IReadOnlyList<LineTokens> lines)
		{
			if (lines.Count > 1)
				throw new NotSupportedException("Too many lines for Point load definition");

			var lineTokens = lines.Single();

			switch (lineTokens.QuantityType)
			{
				case Codes.FORC:
					{
						int dofId;
						switch (lineTokens.Direction)
						{
							case Codes.X:
								dofId = 1;
								break;
							case Codes.Y:
								dofId = 2;
								break;
							case Codes.Z:
								dofId = 3;
								break;
							case Codes.XG:
							case Codes.YG:
							case Codes.ZG:
								throw new NotImplementedException($"direction '{lineTokens.Direction}' is not implemented yet");
							default:
								throw new NotSupportedException($"direction '{lineTokens.Direction}' is not supported");
						}
						double value = lineTokens.Value.Value;
						var pointLoadAttribute = new ModelAttribute
						{
							Type = AttributeType.BoundaryCondition,
							Name = BoundaryConditionNames.NodalLoad,
							Parameters = Invariant($"components 1 {value} dofs 1 {dofId}")
						};
						switch (lineTokens.SelectionType)
						{
							case Codes.NODE:
								{
									int vertexId = lineTokens.Number.Value;
									pointLoadAttribute.VertexAttributes.Add(new VertexAttribute { VertexId = vertexId });
								}
								break;
							default:
								throw new NotSupportedException($"selection type '{lineTokens.SelectionType}' is not supported");
						}
						return pointLoadAttribute;
					}
				case Codes.MOM:
					throw new NotImplementedException("Moment load is not implemented");
				default:
					throw new NotSupportedException($"quantity type '{lineTokens.QuantityType}' is not supported");
			}
		}

		private ModelAttribute parseLineAttribute(IReadOnlyList<LineTokens> lines)
		{
			if (lines.Count > 2)
				throw new NotSupportedException("Too many lines for Line load definition");

			var firstLine = lines[0];

			switch (firstLine.QuantityType)
			{
				case Codes.FORC:
					{
						// The load can be defined in global coordinate system (csType = 0, default) or in entity - specific local coordinate system (csType = 1).
						int csType;
						int dofId;
						switch (firstLine.Direction)
						{
							case Codes.XG:
								csType = 0;
								dofId = 1;
								break;
							case Codes.YG:
								csType = 0;
								dofId = 2;
								break;
							case Codes.ZG:
								csType = 0;
								dofId = 3;
								break;
							case Codes.XE:
								csType = 1;
								dofId = 1;
								break;
							case Codes.YE:
								csType = 1;
								dofId = 2;
								break;
							case Codes.ZE:
								csType = 1;
								dofId = 3;
								break;
							default:
								throw new NotSupportedException($"direction '{firstLine.Direction}' is not supported");
						}
						double value = firstLine.Value.Value;
						var lineLoadAttribute = new ModelAttribute
						{
							Type = AttributeType.BoundaryCondition,
							Name = BoundaryConditionNames.ConstantEdgeLoad,
							Parameters = Invariant($"loadType 3 components 1 {value} dofs 1 {dofId} csType {csType}")
						};
						switch (firstLine.SelectionType)
						{
							case Codes.MACR:
								{
									int macroId = firstLine.Number.Value;
									int? lineId = null;
									if (lines.Count > 1)
									{
										var secondLine = lines[1];
										if (secondLine.SelectionType == Codes.LINE)
										{
											lineId = secondLine.Number.Value;
										}
										// TODO: parse rest of lines
									}
									if (lineId.HasValue)
									{
										lineLoadAttribute.CurveAttributes.Add(new CurveAttribute { MacroId = macroId, CurveId = lineId.Value });
									}
									else
									{
										lineLoadAttribute.MacroAttributes.Add(new MacroAttribute { MacroId = macroId });
									}
								}
								break;
							default:
								throw new NotSupportedException($"selection type '{firstLine.SelectionType}' is not supported");
						}
						return lineLoadAttribute;
					}
				default:
					throw new NotSupportedException($"quantity type '{firstLine.QuantityType}' is not supported");
			}
		}

		private ModelAttribute parseSurfaceAttribute(IReadOnlyList<LineTokens> lines)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region File codes

		private static class Codes
		{
			// File section names
			public const string MODEL = nameof(MODEL);
			public const string MULT = nameof(MULT);
			public const string OWN = nameof(OWN);

			// Dimension types
			public const string POIN = nameof(POIN);
			public const string LIN = nameof(LIN);
			public const string SURF = nameof(SURF);

			// Quantity types
			public const string FORC = nameof(FORC);
			public const string MOM = nameof(MOM);

			// Selection types
			public const string MACR = nameof(MACR);
			public const string LINE = nameof(LINE);
			public const string NODE = nameof(NODE);

			// Directions
			public const string X = nameof(X);
			public const string Y = nameof(Y);
			public const string Z = nameof(Z);
			public const string XG = nameof(XG);
			public const string YG = nameof(YG);
			public const string ZG = nameof(ZG);
			public const string XL = nameof(XL);
			public const string YL = nameof(YL);
			public const string ZL = nameof(ZL);
			public const string XPG = nameof(XPG);
			public const string YPG = nameof(YPG);
			public const string ZPG = nameof(ZPG);
			public const string XE = nameof(XE);
			public const string YE = nameof(YE);
			public const string ZE = nameof(ZE);
		}

		#endregion
	}
}
