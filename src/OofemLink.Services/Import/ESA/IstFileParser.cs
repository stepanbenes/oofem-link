using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OofemLink.Common.MathPhys;
using OofemLink.Common.Enumerations;
using OofemLink.Common.OofemNames;
using OofemLink.Data.Entities;
using static System.FormattableString;

namespace OofemLink.Services.Import.ESA
{
	class IstFileParser : AttributeFileParserBase
	{
		readonly AttributeMapper attributeMapper;
		readonly CoordinateTransformService coordinateTransformService;

		public IstFileParser(AttributeMapper attributeMapper, CoordinateTransformService coordinateTransformService, string location, string taskName, ILoggerFactory loggerFactory)
			: base(location, taskName, loggerFactory)
		{
			this.attributeMapper = attributeMapper;
			this.coordinateTransformService = coordinateTransformService;
		}

		public override string Extension => "IST";

		public IEnumerable<ModelAttribute> Parse()
		{
			LogStart();

			var attributes = new List<ModelAttribute>();
			var materialMap = new Dictionary<int, ModelAttribute>();
			var pointFixRecords = new List<PointFixRecord>();

			using (var streamReader = File.OpenText(FileFullPath))
			{
				string line = streamReader.ReadLine(); // ignore first line: Directory path
				while ((line = streamReader.ReadLine()) != null)
				{
					if (line == "")
						continue;
					if (line.Length != 160)
						throw new FormatException($"Wrong {Extension} file format. Each row (except the first) is expected to have exactly 160 characters.");

					LineTokens lineTokens = ParseLineTokens(line);

					switch (lineTokens.ItemType)
					{
						case Codes.MAT:
							var materialAttribute = parseMaterialSection(streamReader,
									dimensionType: lineTokens.DimensionType,
									quantityType: lineTokens.QuantityType,
									number: lineTokens.Number.Value
								);
							materialMap.Add(materialAttribute.LocalNumber.Value, materialAttribute);
							attributes.Add(materialAttribute);
							break;
						case Codes.PHYS:
							parsePhysicalDataSection(streamReader, materialMap,
									dimensionType: lineTokens.DimensionType,
									quantityType: lineTokens.QuantityType,
									materialId: lineTokens.MaterialId,
									subgradeId: lineTokens.SubgradeId,
									selectionType: lineTokens.SelectionType,
									number: lineTokens.Number.Value
								);
							break;
						case Codes.FIX:
							{
								var fixRecords = parseFixSection(lineTokens.DimensionType, lineTokens.QuantityType, lineTokens.Direction, lineTokens.SelectionType, lineTokens.Number.Value);
								pointFixRecords.AddRange(fixRecords);
							}
							break;
						case Codes.LCS:
							{
								if (lineTokens.QuantityType == Codes.ROT)
								{
									switch (lineTokens.Direction)
									{
										case Codes.Y:
										case Codes.Z:
											var lcsAttribute = parseLocalCoordinateSystemForLine(
																	direction: lineTokens.Direction,
																	lcsType: lineTokens[4],
																	selectionType: lineTokens.SelectionType,
																	number: lineTokens.Number.Value,
																	x: lineTokens.X.Value, y: lineTokens.Y.Value, z: lineTokens.Z.Value);
											attributes.Add(lcsAttribute);
											break;
										case Codes.ALPHA:
										case Codes.X:
										default:
											throw new NotSupportedException($"direction {lineTokens.Direction} is not supported in section {Codes.LCS}");
									}
								}
								else
									throw new NotSupportedException($"quantity type {lineTokens.QuantityType} is not supported in section {Codes.LCS}");
							}
							break;
						default:
							Logger.LogWarning("Ignoring token '{0}'", lineTokens.ItemType);
							break;
					}
				}
			}

			var pointFixGroups = from fixRecord in pointFixRecords
								 group fixRecord by fixRecord.VertexId;
			foreach (var pointFixGroup in pointFixGroups)
			{
				var fs = pointFixGroup.ToList();
				var bc = new ModelAttribute
				{
					Type = AttributeType.BoundaryCondition,
					Name = BoundaryConditionNames.BoundaryCondition,
					Target = AttributeTarget.Node,
					Parameters = Invariant($"values {fs.Count} {string.Join(" ", Enumerable.Repeat(0, fs.Count))} dofs {fs.Count} {string.Join(" ", fs.Select(f => f.DofId))}")
				};
				attributeMapper.MapToVertex(bc, pointFixGroup.Key);
				attributes.Add(bc);
			}

			return attributes;
		}

		#region Parsing materials

		private ModelAttribute parseMaterialSection(StreamReader streamReader, string dimensionType, string quantityType, int number)
		{
			switch (dimensionType)
			{
				case Codes.LIN:
					switch (quantityType)
					{
						case Codes.SECT:
							{
								LineValues line1_values = ParseLineValues(streamReader.ReadLine());
								LineValues line2_values = ParseLineValues(streamReader.ReadLine());

								return createAttributeFromBeamCrossSectionCharacteristics(number,
										Ax: line1_values[0] ?? 0.0, // TODO: is it ok to replace missing values with zeroes? Or should throw exception if some important parameter is missing?
										Ay: line1_values[1] ?? 0.0,
										Az: line1_values[2] ?? 0.0,
										Ix: line1_values[3] ?? 0.0,
										Iy: line1_values[4] ?? 0.0,
										Iz: line1_values[5] ?? 0.0,
										E: line1_values[6] ?? 0.0,
										G: line1_values[7] ?? 0.0,
										gamma: line2_values[6] ?? 0.0,
										area: line2_values[7] ?? 0.0
									);
							}
						case Codes.STIF:
							{
								// TODO: add line reading and parsing
								return createAttributeFromBeamStiffnessCharacteristics();
							}
						default:
							throw new NotSupportedException($"quantity type '{quantityType}' is not supported");
					}
				case Codes.SURF:
					switch (quantityType)
					{
						case Codes.ISO:
							throw new NotImplementedException();
						case Codes.ORT:
							throw new NotImplementedException();
						case Codes.STIF:
							throw new NotImplementedException();
						default:
							throw new NotSupportedException($"quantity type '{quantityType}' is not supported");
					}
				default:
					throw new NotSupportedException($"dimension type '{dimensionType}' is not supported");
			}
		}

		private static ModelAttribute createAttributeFromBeamCrossSectionCharacteristics(int number, double Ax /*ignored*/, double Ay, double Az, double Ix, double Iy, double Iz, double E, double G, double gamma, double area)
		{
			const double tAlpha = 0.0;

			var crossSection = new ModelAttribute
			{
				Type = AttributeType.CrossSection,
				LocalNumber = number,
				Name = CrossSectionNames.SimpleCS,
				Target = AttributeTarget.Volume, // cross-section is applied to element volume
				Parameters = Invariant($"area {area} Iy {Iy} Iz {Iz} Ik {Ix} shearareay {Ay} shearareaz {Az}")
			};

			var material = new ModelAttribute
			{
				Type = AttributeType.Material,
				LocalNumber = number,
				Name = MaterialNames.IsoLE,
				Target = AttributeTarget.Volume, // material is applied to element volume
				Parameters = Invariant($"d {gamma / PhysicalConstants.g} E {E} n {E / G / 2 - 1} tAlpha {tAlpha}")
			};

			// create cross-section - material relation
			var relation = new AttributeComposition { ParentAttribute = crossSection, ChildAttribute = material };
			crossSection.ChildAttributes.Add(relation);
			material.ParentAttributes.Add(relation);

			return crossSection;
		}

		private static ModelAttribute createAttributeFromBeamStiffnessCharacteristics(/**/)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Parsing attribute-macro mapping

		private void parsePhysicalDataSection(StreamReader streamReader, Dictionary<int, ModelAttribute> materialMap, string dimensionType, string quantityType, int? materialId, int? subgradeId, string selectionType, int number)
		{
			switch (dimensionType)
			{
				case Codes.BEAM:
					switch (quantityType)
					{
						case Codes.MAT:
							{
								if (selectionType != Codes.MACR)
									throw new InvalidDataException($"Physical data {Codes.BEAM} {Codes.MAT} can be applied only to {Codes.MACR} selection");
								LineTokens lineTokens = ParseLineTokens(streamReader.ReadLine());
								if (lineTokens.SelectionType != Codes.LINE)
									throw new InvalidDataException($"Selection type '{lineTokens.SelectionType}' was not expected. '{Codes.LINE}' was expected instead.");
								int lineId = lineTokens.Number.Value;
								var materialAttribute = materialMap[materialId.Value];
								attributeMapper.MapToCurve(materialAttribute, curveId: lineId, macroId: number);
							}
							break;
						case Codes.ROT:
							// ignore PHYS BEAM ROT, information should be duplicated in LCS section
							break;
						case Codes.VARL:
							throw new NotImplementedException();
						default:
							throw new NotSupportedException($"quantity type '{quantityType}' is not supported");
					}
					break;
				case Codes.PLAT:
				case Codes.PLAN:
				case Codes.FLAT:
					throw new NotImplementedException();
				default:
					throw new NotSupportedException($"dimension type '{dimensionType}' is not supported");
			}
		}

		#endregion

		#region Parsing supports

		private static IEnumerable<PointFixRecord> parseFixSection(string dimensionType, string quantityType, string direction, string selectionType, int number)
		{
			int dofId;
			switch (direction)
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
				default:
					throw new NotSupportedException($"direction '{direction}' is not supported");
			}
			switch (quantityType)
			{
				case Codes.DISP:
					dofId += 0;
					break;
				case Codes.ROT:
					dofId += 3;
					break;
				default:
					throw new NotSupportedException($"quantity type '{quantityType}' is not supported");
			}
			switch (dimensionType)
			{
				case Codes.POIN:
					if (selectionType != Codes.NODE)
						throw new InvalidDataException($"Selection type '{Codes.NODE}' was expected instead of '{selectionType}'");
					return new[] { new PointFixRecord(dofId, number) };
				case Codes.LIN:
					if (selectionType != Codes.LINE)
						throw new InvalidDataException($"Selection type '{Codes.LINE}' was expected instead of '{selectionType}'");

					throw new NotImplementedException(); // TODO: return array of two records - one for each node of the line

				default:
					throw new NotSupportedException($"dimension type '{dimensionType}' is not supported");
			}
		}

		#endregion

		#region LCS parsing

		private ModelAttribute parseLocalCoordinateSystemForLine(string direction, string lcsType, string selectionType, int number, double x, double y, double z)
		{
			Debug.Assert(direction == Codes.Y || direction == Codes.Z);
			if (lcsType == Codes.GLOB)
			{
				if (selectionType == Codes.LINE)
				{
					Vector3d localCoordinates;
					if (direction == Codes.Y)
						localCoordinates = coordinateTransformService.CalculateLocalZAxisForLineFromGlobalYAxisTargetPoint(lineId: number, yTargetPoint: new Vector3d(x, y, z));
					else
						localCoordinates = coordinateTransformService.CalculateLocalZAxisForLineFromGlobalZAxisTargetPoint(lineId: number, zTargetPoint: new Vector3d(x, y, z));
					var lcsAttribute = new ModelAttribute
					{
						Type = AttributeType.LocalCoordinateSystem,
						Name = "zaxis",
						Target = AttributeTarget.Volume,
						Parameters = Invariant($"3 {localCoordinates.X} {localCoordinates.Y} {localCoordinates.Z}")
					};
					attributeMapper.MapToCurve(lcsAttribute, curveId: number);
					return lcsAttribute;
				}
				else
					throw new NotSupportedException($"Selection type '{selectionType}' is not supported in LCS section");
			}
			else
				throw new NotSupportedException($"Lcs type '{lcsType}' is not supported in LCS section");
		}

		#endregion

		#region File codes

		private static class Codes
		{
			// File section names
			public const string MODEL = nameof(MODEL);
			public const string MAT = nameof(MAT);
			public const string PHYS = nameof(PHYS);
			public const string FIX = nameof(FIX);
			public const string SUPR = nameof(SUPR);
			public const string CON = nameof(CON);
			public const string LCS = nameof(LCS);
			public const string SPR = nameof(SPR);
			public const string REL = nameof(REL);

			// dimension types
			public const string POIN = nameof(POIN);
			public const string SURF = nameof(SURF);
			public const string LIN = nameof(LIN);
			public const string BEAM = nameof(BEAM);
			public const string PLAT = nameof(PLAT);
			public const string PLAN = nameof(PLAN);
			public const string FLAT = nameof(FLAT);

			// quantity types
			public const string DISP = nameof(DISP);
			public const string ROT = nameof(ROT);
			public const string SECT = nameof(SECT);
			public const string STIF = nameof(STIF);
			public const string ISO = nameof(ISO);
			public const string ORT = nameof(ORT);
			public const string VARL = nameof(VARL);

			// directions
			public const string X = nameof(X);
			public const string Y = nameof(Y);
			public const string Z = nameof(Z);
			public const string ALPHA = nameof(ALPHA);

			// selection types
			public const string MACR = nameof(MACR);
			public const string LINE = nameof(LINE);
			public const string NODE = nameof(NODE);

			public const string GLOB = nameof(GLOB);
		}

		#endregion

		#region FixInfo struct

		private struct PointFixRecord
		{
			public PointFixRecord(int dofId, int targetId)
			{
				DofId = dofId;
				VertexId = targetId;
			}
			public int DofId { get; }
			public int VertexId { get; }
		}

		#endregion
	}
}
