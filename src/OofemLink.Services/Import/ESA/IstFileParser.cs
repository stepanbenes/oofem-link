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
	class IstFileParser : EsaFileParserBase
	{
		public IstFileParser(string location, string taskName, ILoggerFactory loggerFactory)
			: base(location, taskName, loggerFactory)
		{ }

		public override string Extension => "IST";

		public IEnumerable<ModelAttribute> Parse()
		{
			LogStart();

			using (var stream = new FileStream(FileFullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
			using (var streamReader = new StreamReader(stream))
			{
				string line = streamReader.ReadLine(); // read Directory path
				while ((line = streamReader.ReadLine()) != null)
				{
					if (line == "")
						continue;
					if (line.Length != 160)
						throw new FormatException("Wrong IST file format. Each row (except the first) is expected to have exactly 160 characters.");

					// druh_polozky, dimenze, typ_veliciny, smer, material_#, podlozi_#, typ_vyberu, cislo, x, y, z, hodnota
					string[] tokens = line.Substring(0, 80).Split(chunkSize: 10).Select(chunk => chunk.TrimStart()).ToArray();
					Debug.Assert(tokens.Length == 8);
					double?[] values = line.Substring(80).Split(chunkSize: 20).Select(chunk => TryParseFloat64(chunk.TrimStart())).ToArray();
					Debug.Assert(values.Length == 4);
					// TODO: pass values to parse methods
					switch (tokens[0])
					{
						case Codes.MAT:
							yield return parseMaterialSection(streamReader, dimensionType: tokens[1], materialType: tokens[2], number: ParseInt32(tokens[7]));
							break;
					}
				}
			}
		}

		#region Private methods

		private ModelAttribute parseMaterialSection(StreamReader streamReader, string dimensionType, string materialType, int number)
		{
			switch (dimensionType)
			{
				case Codes.LIN:
					switch (materialType)
					{
						case Codes.SECT:
							{
								string line1 = streamReader.ReadLine();
								double?[] line1_values = line1.Split(chunkSize: 20).Select(chunk => TryParseFloat64(chunk.TrimStart())).ToArray();
								Debug.Assert(line1_values.Length == 8);
								string line2 = streamReader.ReadLine();
								double?[] line2_values = line2.Split(chunkSize: 20).Select(chunk => TryParseFloat64(chunk.TrimStart())).ToArray();

								return createAttributeFromBeamCrossSectionCharacteristics(number,
										Ix: line1_values[3] ?? 0.0, // TODO: is it ok to replace missing values with zeroes?
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
							throw new NotSupportedException($"material type '{materialType}' is not supported");
					}
				case Codes.SURF:
					switch (materialType)
					{
						case Codes.ISO:
							throw new NotImplementedException();
						case Codes.ORT:
							throw new NotImplementedException();
						case Codes.STIF:
							throw new NotImplementedException();
						default:
							throw new NotSupportedException($"material type '{materialType}' is not supported");
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

		#region Keywords

		private static class Codes
		{
			public const string MODEL = nameof(MODEL);
			public const string MAT = nameof(MAT);
			public const string SURF = nameof(SURF);
			public const string LIN = nameof(LIN);
			public const string SECT = nameof(SECT);
			public const string STIF = nameof(STIF);
			public const string ISO = nameof(ISO);
			public const string ORT = nameof(ORT);
		}

		public static class CrossSectionNames
		{
			public const string SimpleCS = nameof(SimpleCS);
		}

		public static class MaterialNames
		{
			public const string IsoLE = nameof(IsoLE);
		}

		#endregion
	}
}
