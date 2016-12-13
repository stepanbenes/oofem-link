using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OofemLink.Common;
using OofemLink.Common.Enumerations;
using OofemLink.Common.Extensions;
using OofemLink.Data.Entities;
using static System.FormattableString;

namespace OofemLink.Services.Import.ESA
{
	class IstFileParser : AttributeFileParserBase
	{
		Dictionary<int, ModelAttribute> materialMap/*, supportsMap*/;

		public IstFileParser(string location, string taskName, ILoggerFactory loggerFactory)
			: base(location, taskName, loggerFactory)
		{ }

		public override string Extension => "IST";

		public IEnumerable<ModelAttribute> Parse()
		{
			LogStart();
			materialMap = new Dictionary<int, ModelAttribute>();
			using (var streamReader = File.OpenText(FileFullPath))
			{
				string line = streamReader.ReadLine(); // ignore first line: Directory path
				while ((line = streamReader.ReadLine()) != null)
				{
					if (line == "")
						continue;
					if (line.Length != 160)
						throw new FormatException("Wrong IST file format. Each row (except the first) is expected to have exactly 160 characters.");

					LineTokens lineTokens = ParseLine(line);

					switch (lineTokens.ItemType)
					{
						case Codes.MAT:
							var materialAttribute = parseMaterialSection(streamReader,
									dimensionType: lineTokens.DimensionType,
									quantityType: lineTokens.QuantityType,
									number: lineTokens.Number.Value
								);
							materialMap.Add(materialAttribute.LocalNumber.Value, materialAttribute);
							yield return materialAttribute;
							break;
						case Codes.PHYS:
							parsePhysicalDataSection(streamReader,
									dimensionType: lineTokens.DimensionType,
									quantityType: lineTokens.QuantityType,
									materialId: lineTokens.MaterialId,
									subgradeId: lineTokens.SubgradeId,
									selectionType: lineTokens.SelectionType,
									number: lineTokens.Number.Value
								);
							break;
						//case Codes.FIX:
						//	throw new NotImplementedException();
						default:
							Logger.LogWarning("Ignoring token '{0}'", lineTokens.ItemType);
							break;
					}
				}
			}
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
								string line1 = streamReader.ReadLine();
								double?[] line1_values = line1.Split(chunkSize: 20).Select(chunk => TryParseFloat64(chunk.TrimStart())).ToArray();
								Debug.Assert(line1_values.Length == 8);
								string line2 = streamReader.ReadLine();
								double?[] line2_values = line2.Split(chunkSize: 20).Select(chunk => TryParseFloat64(chunk.TrimStart())).ToArray();

								return createAttributeFromBeamCrossSectionCharacteristics(number,
										Ix: line1_values[3] ?? 0.0, // TODO: is it ok to replace missing values with zeroes? Or should throw exception if some important parameter is missing?
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

		private static ModelAttribute createAttributeFromBeamCrossSectionCharacteristics(int number, double Ix, double Iy, double Iz, double E, double G, double gamma, double area)
		{
			const double beamShearCoeff = 1.0e18;
			const double tAlpha = 0.0;

			var crossSection = new ModelAttribute
			{
				Type = AttributeType.CrossSection,
				LocalNumber = number,
				Name = CrossSectionNames.SimpleCS,
				Parameters = Invariant($"area {area} Iy {Iy} Iz {Iz} Ik {Ix} beamShearCoeff {beamShearCoeff}")
			};

			var material = new ModelAttribute
			{
				Type = AttributeType.Material,
				LocalNumber = number,
				Name = MaterialNames.IsoLE,
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

		private void parsePhysicalDataSection(StreamReader streamReader, string dimensionType, string quantityType, int? materialId, int? subgradeId, string selectionType, int number)
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
								string line1 = streamReader.ReadLine();
								string geometryEntitySelectionName = line1.Substring(startIndex: 60, length: 10).TrimStart();
								if (geometryEntitySelectionName != Codes.LINE)
									throw new InvalidDataException($"Selection type '{geometryEntitySelectionName}' was not expected. '{Codes.LINE}' was expected instead.");
								int lineId = ParseInt32(line1.Substring(startIndex: 70, length: 10).TrimStart());
								var curveAttribute = new CurveAttribute
								{
									MacroId = number,
									CurveId = lineId,
								};
								var materialAttribute = materialMap[materialId.Value];
								materialAttribute.CurveAttributes.Add(curveAttribute);
							}
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

		#region File codes

		private static class Codes
		{
			// File section names
			public const string MODEL = nameof(MODEL);
			public const string MAT = nameof(MAT);
			public const string PHYS = nameof(PHYS);
			public const string FIX = nameof(FIX);
			public const string LCS = nameof(LCS);
			public const string SPR = nameof(SPR);
			public const string REL = nameof(REL);

			// dimension types
			public const string SURF = nameof(SURF);
			public const string LIN = nameof(LIN);
			public const string BEAM = nameof(BEAM);
			public const string PLAT = nameof(PLAT);
			public const string PLAN = nameof(PLAN);
			public const string FLAT = nameof(FLAT);

			// quantity types
			public const string SECT = nameof(SECT);
			public const string STIF = nameof(STIF);
			public const string ISO = nameof(ISO);
			public const string ORT = nameof(ORT);
			public const string VARL = nameof(VARL);

			// selection types
			public const string MACR = nameof(MACR);
			public const string LINE = nameof(LINE);
		}

		#endregion
	}
}
