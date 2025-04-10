﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Extensions.Logging;
using OofemLink.Data.DbEntities;
using OofemLink.Common.OofemNames;
using OofemLink.Common.Enumerations;
using OofemLink.Common.MathPhys;
using static System.FormattableString;

namespace OofemLink.Services.Import.ESA
{
	class IxxxxFileParser : AttributeFileParserBase
	{
		readonly int loadCaseNumber;
		readonly AttributeMapper attributeMapper;

		public IxxxxFileParser(int loadCaseNumber, AttributeMapper attributeMapper, string location, string taskName, ILoggerFactory loggerFactory)
			: base(location, taskName, loggerFactory)
		{
			if (loadCaseNumber < 1 || loadCaseNumber > 9999)
				throw new ArgumentOutOfRangeException(nameof(loadCaseNumber), "Argument must be in range <1, 9999>");
			this.loadCaseNumber = loadCaseNumber;
			this.attributeMapper = attributeMapper;
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
				case Codes.MODEL:
					// ignore this line
					lineBuffer.Clear();
					break;
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
				case Directions.XG:
					x = coefficient * PhysicalConstants.g; // TODO: check correctness
					break;
				case Directions.YG:
					y = coefficient * PhysicalConstants.g;
					break;
				case Directions.ZG:
					z = coefficient * PhysicalConstants.g;
					break;
				default:
					throw new NotSupportedException($"Unknown direction parameter '{direction}'");
			}

			var deadWeight = new ModelAttribute
			{
				Type = AttributeType.BoundaryCondition,
				Name = BoundaryConditionNames.DeadWeight,
				Target = AttributeTarget.Volume, // dead weight is applied to element volume
				Parameters = Invariant($"components 6 {x} {y} {z} 0 0 0")
			};

			while (lineBuffer.Count > 0)
			{
				var line = lineBuffer.Dequeue();
				// TODO: process parameters
				Logger.LogWarning($"Ignoring line with values in section {Codes.OWN}");
			}

			attributeMapper.MapToAllMacros(deadWeight);

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
						bool isLcs;
						int dofId = ParseDofId(lineTokens.Direction, out isLcs);
						if (isLcs)
							throw new InvalidDataException($"Local coordinate system not supported in {Codes.MULT} {Codes.POIN} {Codes.FORC} section");
						double value = lineTokens.Value.Value;
						var pointLoadAttribute = new ModelAttribute
						{
							Type = AttributeType.BoundaryCondition,
							Name = BoundaryConditionNames.NodalLoad,
							Target = AttributeTarget.Node,
							Parameters = Invariant($"components 1 {value} dofs 1 {dofId}")
						};
						switch (lineTokens.SelectionType)
						{
							case Codes.NODE:
								{
									int vertexId = lineTokens.Number.Value;
									attributeMapper.MapToVertex(pointLoadAttribute, vertexId);
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
						bool isLcs;
						int dofId = ParseDofId(firstLine.Direction, out isLcs);
						int csType = isLcs ? 1 : 0;
						double value = firstLine.Value.Value;
						IEnumerable<string> components = Enumerable.Range(1, 6).Select(id => id == dofId ? value.ToString(CultureInfo.InvariantCulture) : "0");
						var lineLoadAttribute = new ModelAttribute
						{
							Type = AttributeType.BoundaryCondition,
							Name = BoundaryConditionNames.ConstantEdgeLoad,
							Target = AttributeTarget.Edge,
							Parameters = Invariant($"loadType 3 ndofs 6 components 6 {string.Join(" ", components)} csType {csType}")
						};

						int? macroId = null, lineId = null;
						switch (firstLine.SelectionType)
						{
							case Codes.MACR:
								macroId = firstLine.Number.Value;
								lineId = null;
								if (lines.Count > 1)
								{
									var secondLine = lines[1];
									if (secondLine.SelectionType == Codes.LINE)
									{
										lineId = secondLine.Number.Value;
									}
								}
								break;
							case Codes.LINE:
								lineId = firstLine.Number.Value;
								break;
							default:
								throw new NotSupportedException($"selection type '{firstLine.SelectionType}' is not supported");
						}

						double? relativeStart = null, relativeEnd = null;
						if (firstLine[4] == Codes.VAR)
						{
							switch (firstLine[8])
							{
								case Codes.RELAT:
									{
										var secondLine = lines[1];
										relativeStart = ParseFloat64(firstLine[9]) * 0.01; // transform from percentage
										relativeEnd = ParseFloat64(secondLine[9]) * 0.01;
									}
									break;
								case Codes.ABS:
									{
										var secondLine = lines[1];
										double absoluteStart = ParseFloat64(firstLine[9]);
										double absoluteEnd = ParseFloat64(secondLine[9]);
										double lineLength = attributeMapper.GetLengthOfBeam(macroId, lineId);
										relativeStart = absoluteStart / lineLength;
										relativeEnd = absoluteEnd / lineLength;
									}
									break;
								default:
									throw new NotSupportedException($"Force application using {firstLine[8]} is not supported");
							}
						}

						if (macroId.HasValue && lineId.HasValue)
						{
							attributeMapper.MapToCurve(lineLoadAttribute, lineId.Value, macroId.Value, relativeStart, relativeEnd);
						}
						else if (macroId.HasValue)
						{
							attributeMapper.MapToBeamMacro(lineLoadAttribute, macroId.Value, relativeStart, relativeEnd);
						}
						else if (lineId.HasValue)
						{
							attributeMapper.MapToCurve(lineLoadAttribute, lineId.Value, relativeStart, relativeEnd);
						}
						else
						{
							throw new InvalidDataException($"{Codes.LIN} {Codes.FORC} location was not specified");
						}

						return lineLoadAttribute;
					}
				default:
					throw new NotSupportedException($"quantity type '{firstLine.QuantityType}' is not supported");
			}
		}

		private ModelAttribute parseSurfaceAttribute(IReadOnlyList<LineTokens> lines)
		{
			if (lines.Count > 1)
				throw new NotImplementedException("Too many lines for surface load definition");

			var firstLine = lines[0];

			switch (firstLine.QuantityType)
			{
				case Codes.FORC:
					{
						// The load can be defined in global coordinate system (csType = 0, default) or in entity - specific local coordinate system (csType = 1).
						bool isLcs;
						int dofId = ParseDofId(firstLine.Direction, out isLcs);
						if (isLcs)
							throw new InvalidDataException($"Local coordinate system not supported in {Codes.MULT} {Codes.SURF} {Codes.FORC} section");
						double value = firstLine.Value.Value;
						IEnumerable<string> components = Enumerable.Range(1, 6).Select(id => id == dofId ? value.ToString(CultureInfo.InvariantCulture) : "0");
						var surfaceLoadAttribute = new ModelAttribute
						{
							Type = AttributeType.BoundaryCondition,
							Name = BoundaryConditionNames.ConstantSurfaceLoad,
							Target = AttributeTarget.Surface,
							Parameters = Invariant($"loadType 3 ndofs 6 components 6 {string.Join(" ", components)}")
						};
						switch (firstLine.SelectionType)
						{
							case Codes.MACR:
								{
									int macroId = firstLine.Number.Value;
									attributeMapper.MapToSurfaceMacro(surfaceLoadAttribute, macroId);
								}
								break;
							default:
								throw new NotSupportedException($"selection type '{firstLine.SelectionType}' is not supported");
						}
						return surfaceLoadAttribute;
					}
				default:
					throw new NotSupportedException($"quantity type '{firstLine.QuantityType}' is not supported");
			}
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

			public const string UNIF = nameof(UNIF);
			public const string VAR = nameof(VAR);

			public const string ABS = nameof(ABS);
			public const string RELAT = nameof(RELAT);
		}

		#endregion
	}
}
