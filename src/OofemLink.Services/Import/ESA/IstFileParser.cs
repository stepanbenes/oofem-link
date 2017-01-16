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
			var pointFixRecords = new List<FixRecord>();
			var lineFixRecords = new List<FixRecord>();
			var pointSpringRecords = new List<SpringRecord>();
			var lineSpringRecords = new List<SpringRecord>();

			using (var streamReader = File.OpenText(FileFullPath))
			{
				string line = streamReader.ReadLine(); // ignore first line: Directory path
				while ((line = streamReader.ReadLine()) != null)
				{
					if (line.Trim() == "") // ignore empty line
						continue;
					if (line.Length != 160)
						throw new FormatException($"Wrong {Extension} file format. Each row (except the first) is expected to have exactly 160 characters.");

					LineTokens lineTokens = ParseLineTokens(line);

					switch (lineTokens.ItemType)
					{
						case Codes.MODEL:
							// ignore this line
							break;
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
									subsoilId: lineTokens.SubsoilId,
									selectionType: lineTokens.SelectionType,
									number: lineTokens.Number.Value
								);
							break;
						case Codes.FIX:
							switch (lineTokens.DimensionType)
							{
								case Codes.POIN:
									{
										if (lineTokens.SelectionType != Codes.NODE)
											throw new InvalidDataException($"Selection type '{Codes.NODE}' was expected instead of '{lineTokens.SelectionType}'");
										int dofId = parseDofId(lineTokens.QuantityType, lineTokens.Direction);
										pointFixRecords.Add(new FixRecord(dofId, targetId: lineTokens.Number.Value));
									}
									break;
								case Codes.LIN:
									{
										if (lineTokens.SelectionType != Codes.LINE)
											throw new InvalidDataException($"Selection type '{Codes.LINE}' was expected instead of '{lineTokens.SelectionType}'");
										int dofId = parseDofId(lineTokens.QuantityType, lineTokens.Direction);
										lineFixRecords.Add(new FixRecord(dofId, targetId: lineTokens.Number.Value));
									}
									break;
								default:
									throw new NotSupportedException($"dimension type '{lineTokens.DimensionType}' is not supported");
							}
							break;
						case Codes.SPR:
							switch (lineTokens.DimensionType)
							{
								case Codes.POIN:
									{
										if (lineTokens.SelectionType != Codes.NODE)
											throw new InvalidDataException($"Selection type '{Codes.NODE}' was expected instead of '{lineTokens.SelectionType}'");
										int dofId = parseDofId(lineTokens.QuantityType, lineTokens.Direction);
										pointSpringRecords.Add(new SpringRecord(dofId, targetId: lineTokens.Number.Value, value: lineTokens.Value.Value));
									}
									break;
								case Codes.LIN:
									{
										if (lineTokens.SelectionType != Codes.LINE)
											throw new InvalidDataException($"Selection type '{Codes.LINE}' was expected instead of '{lineTokens.SelectionType}'");
										int dofId = parseDofId(lineTokens.QuantityType, lineTokens.Direction);
										lineSpringRecords.Add(new SpringRecord(dofId, targetId: lineTokens.Number.Value, value: lineTokens.Value.Value));
									}
									break;
								default:
									throw new NotSupportedException($"dimension type '{lineTokens.DimensionType}' is not supported");
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
											{
												var lcsAttribute = parseLocalCoordinateSystemForLine( // LCS is defined using point in direction of Y or Z axis
																		direction: lineTokens.Direction,
																		lcsType: lineTokens[4],
																		selectionType: lineTokens.SelectionType,
																		number: lineTokens.Number.Value,
																		x: lineTokens.X.Value, y: lineTokens.Y.Value, z: lineTokens.Z.Value);
												attributes.Add(lcsAttribute);
											}
											break;
										case Codes.ALFA:
											{
												var lcsAttribute = parseLocalCoordinateSystemForLine( // LCS is defined using rotation around local X axis by angle alpha
																		selectionType: lineTokens.SelectionType,
																		number: lineTokens.Number.Value,
																		alpha: lineTokens.Value.Value);
												attributes.Add(lcsAttribute);
											}
											break;
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
							Logger.LogWarning("Ignoring token '{0}', line: {1}", lineTokens.ItemType, line);
							break;
					}
				}
			}

			// group point supports
			{
				var pointFixGroups = from fixRecord in pointFixRecords
									 group fixRecord by fixRecord.TargetId;
				foreach (var pointFixGroup in pointFixGroups)
				{
					var fixes = pointFixGroup.OrderBy(f => f.DofId).ToList();
					var bc = new ModelAttribute
					{
						Type = AttributeType.BoundaryCondition,
						Name = BoundaryConditionNames.BoundaryCondition,
						Target = AttributeTarget.Node,
						Parameters = Invariant($"values {fixes.Count} {string.Join(" ", Enumerable.Repeat(0, fixes.Count))} dofs {fixes.Count} {string.Join(" ", fixes.Select(f => f.DofId.ToString(CultureInfo.InvariantCulture)))}")
					};
					attributeMapper.MapToVertex(bc, pointFixGroup.Key);
					attributes.Add(bc);
				}
			}

			// group line supports
			{
				var lineFixGroups = from fixRecord in lineFixRecords
									group fixRecord by fixRecord.TargetId;
				foreach (var lineFixGroup in lineFixGroups)
				{
					var fixes = lineFixGroup.OrderBy(f => f.DofId).ToList();
					var bc = new ModelAttribute
					{
						Type = AttributeType.BoundaryCondition,
						Name = BoundaryConditionNames.BoundaryCondition,
						Target = AttributeTarget.Node,
						Parameters = Invariant($"values {fixes.Count} {string.Join(" ", Enumerable.Repeat(0, fixes.Count))} dofs {fixes.Count} {string.Join(" ", fixes.Select(f => f.DofId.ToString(CultureInfo.InvariantCulture)))}")
					};
					attributeMapper.MapToCurve(bc, lineFixGroup.Key);
					attributes.Add(bc);
				}
			}

			// group nodal springs
			{
				var pointSpringGroups = from springRecord in pointSpringRecords
										group springRecord by springRecord.TargetId;
				foreach (var pointSpringGroup in pointSpringGroups)
				{
					var springs = pointSpringGroup.OrderBy(s => s.DofId).ToList();
					var springAttribute = new ModelAttribute
					{
						Type = AttributeType.Spring,
						Name = ElementNames.NodalSpring,
						Target = AttributeTarget.Node,
						Parameters = Invariant($"dofmask {springs.Count} {string.Join(" ", springs.Select(s => s.DofId.ToString(CultureInfo.InvariantCulture)))} k {springs.Count} {string.Join(" ", springs.Select(f => f.Value.ToString(CultureInfo.InvariantCulture)))}")
					};
					attributeMapper.MapToVertex(springAttribute, vertexId: pointSpringGroup.Key);
					attributes.Add(springAttribute);
				}
			}

			// group line springs
			{
				var lineSpringGroups = from springRecord in lineSpringRecords
									   group springRecord by springRecord.TargetId;
				foreach (var lineSpringGroup in lineSpringGroups)
				{
					var springs = lineSpringGroup.OrderBy(s => s.DofId).ToList();
					var springAttribute = new ModelAttribute
					{
						Type = AttributeType.Spring,
						Name = ElementNames.LineDistributedSpring,
						Target = AttributeTarget.Volume,
						Parameters = Invariant($"dofs {springs.Count} {string.Join(" ", springs.Select(s => s.DofId.ToString(CultureInfo.InvariantCulture)))} k {springs.Count} {string.Join(" ", springs.Select(f => f.Value.ToString(CultureInfo.InvariantCulture)))}")
					};
					attributeMapper.MapToCurve(springAttribute, curveId: lineSpringGroup.Key);
					attributes.Add(springAttribute);
				}
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
							{
								LineValues lineValues = ParseLineValues(streamReader.ReadLine());
								return createAttributeFromSurfaceIsoMaterialCharacteristics(number,
										E: lineValues[0] ?? 0.0,
										h: lineValues[2] ?? 0.0,
										G: lineValues[3] ?? 0.0,
										alpha: lineValues[5] ?? 0.0,
										gamma: lineValues[6] ?? 0.0
									);
							}
						case Codes.SOIL:
							{
								LineValues line1_Values = ParseLineValues(streamReader.ReadLine());
								LineValues line2_Values = ParseLineValues(streamReader.ReadLine());
								// TODO: take into account all values
								return createAttributeFromSurfaceSoilMaterialCharacteristics(number,
										c1: line1_Values[0] ?? 0.0,
										c2x: line1_Values[2] ?? 0.0,
										c2y: line1_Values[3] ?? 0.0
									);
							}
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

		private ModelAttribute createAttributeFromSurfaceIsoMaterialCharacteristics(int number, double E, double h, double G, double alpha, double gamma)
		{
			var crossSection = new ModelAttribute
			{
				Type = AttributeType.CrossSection,
				LocalNumber = number,
				Name = CrossSectionNames.SimpleCS,
				Target = AttributeTarget.Volume, // cross-section is applied to element volume
				Parameters = Invariant($"thick {h}")
			};

			var material = new ModelAttribute
			{
				Type = AttributeType.Material,
				LocalNumber = number,
				Name = MaterialNames.IsoLE,
				Target = AttributeTarget.Volume, // material is applied to element volume
				Parameters = Invariant($"d {gamma / PhysicalConstants.g} E {E} n {E / G / 2 - 1} tAlpha {alpha}")
			};

			// create cross-section - material relation
			var relation = new AttributeComposition { ParentAttribute = crossSection, ChildAttribute = material };
			crossSection.ChildAttributes.Add(relation);
			material.ParentAttributes.Add(relation);

			return crossSection;
		}

		private ModelAttribute createAttributeFromSurfaceSoilMaterialCharacteristics(int number, double c1, double c2x, double c2y)
		{
			var dummyCrossSection = new ModelAttribute
			{
				Type = AttributeType.CrossSection,
				LocalNumber = number,
				Name = CrossSectionNames.SimpleCS, // TODO: use some dummy cross-section instead if exists
				Target = AttributeTarget.Volume,
			};

			var material = new ModelAttribute
			{
				Type = AttributeType.Material,
				LocalNumber = number,
				Name = MaterialNames.WinklerPasternak,
				Target = AttributeTarget.Volume,
				Parameters = Invariant($"c1 {c1} c2x {c2x} c2y {c2y}") // TODO: add 'd' parameter
			};

			// create cross-section - material relation
			var relation = new AttributeComposition { ParentAttribute = dummyCrossSection, ChildAttribute = material };
			dummyCrossSection.ChildAttributes.Add(relation);
			material.ParentAttributes.Add(relation);

			return dummyCrossSection;
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

		private void parsePhysicalDataSection(StreamReader streamReader, Dictionary<int, ModelAttribute> materialMap, string dimensionType, string quantityType, int? materialId, int? subsoilId, string selectionType, int number)
		{
			switch (dimensionType)
			{
				case Codes.BEAM:
					switch (quantityType)
					{
						case Codes.MAT:
							{
								if (selectionType != Codes.MACR)
									throw new InvalidDataException($"{Codes.PHYS} {Codes.BEAM} {Codes.MAT} can be applied only to {Codes.MACR} selection");
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
							// however, read next line, that should contain line id
							var ignore = streamReader.ReadLine();
							break;
						case Codes.VARL:
							throw new NotImplementedException();
						default:
							throw new NotSupportedException($"quantity type '{quantityType}' is not supported");
					}
					break;
				case Codes.FLAT:
					{
						if (quantityType != "")
							throw new NotImplementedException($"quantity type '{quantityType}' is not implemented in section {Codes.PHYS} {Codes.FLAT}");
						if (selectionType != Codes.MACR)
							throw new InvalidDataException($"{Codes.PHYS} {Codes.FLAT} can be applied only to {Codes.MACR} selection");
						int matId = (materialId.Value != 0) ? materialId.Value : subsoilId.Value; // standard material or subsoil?
						var materialAttribute = materialMap[matId];
						attributeMapper.MapToMacro(materialAttribute, macroId: number);
					}
					break;
				case Codes.PLAT:
				case Codes.PLAN:
					throw new NotImplementedException();
				default:
					throw new NotSupportedException($"dimension type '{dimensionType}' is not supported");
			}
		}

		#endregion

		#region Parsing supports & springs

		private static int parseDofId(string quantityType, string direction)
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
			return dofId;
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

		private ModelAttribute parseLocalCoordinateSystemForLine(string selectionType, int number, double alpha)
		{
			if (selectionType == Codes.LINE)
			{
				Vector3d localCoordinates = coordinateTransformService.CalculateLocalZAxisForLineFromAngleAroundLocalXAxis(lineId: number, angle: alpha);
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
			public const string SOIL = nameof(SOIL);

			// directions
			public const string X = nameof(X);
			public const string Y = nameof(Y);
			public const string Z = nameof(Z);
			public const string ALFA = nameof(ALFA);

			// selection types
			public const string MACR = nameof(MACR);
			public const string LINE = nameof(LINE);
			public const string NODE = nameof(NODE);

			public const string GLOB = nameof(GLOB);
		}

		#endregion

		#region Helper structs

		private struct FixRecord
		{
			public FixRecord(int dofId, int targetId)
			{
				DofId = dofId;
				TargetId = targetId;
			}
			public int DofId { get; }
			public int TargetId { get; }
		}

		private struct SpringRecord
		{
			public SpringRecord(int dofId, int targetId, double value)
			{
				DofId = dofId;
				TargetId = targetId;
				Value = value;
			}
			public int DofId { get; }
			public int TargetId { get; }
			public double Value { get; }
		}

		#endregion
	}
}
