using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OofemLink.Common.Enumerations;
using OofemLink.Common.Extensions;
using OofemLink.Data.Entities;

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
						case IstFileCodes.MAT:
							yield return parseMaterialSection(streamReader, dimensionType: tokens[1], materialType: tokens[2], number: ParseInt32(tokens[7]));
							break;
					}
				}
			}
		}

		private ModelAttribute parseMaterialSection(StreamReader streamReader, string dimensionType, string materialType, int number)
		{
			switch (dimensionType)
			{
				case IstFileCodes.SURF:
					throw new NotImplementedException();
				case IstFileCodes.LIN:
					switch (materialType)
					{
						case IstFileCodes.SECT:
							{
								string line1 = streamReader.ReadLine();
								double?[] line1_values = line1.Split(chunkSize: 20).Select(chunk => TryParseFloat64(chunk.TrimStart())).ToArray();
								Debug.Assert(line1_values.Length == 8);
								string line2 = streamReader.ReadLine();
								double?[] line2_values = line2.Split(chunkSize: 20).Select(chunk => TryParseFloat64(chunk.TrimStart())).ToArray();

								return createAttributeFromBeamCrossSectionCharacteristics(number);
							}
						case IstFileCodes.STIF:
							{
								// TODO: add line reading and parsing
								return createAttributeFromBeamStiffnessCharacteristics();
							}
						default:
							throw new NotSupportedException($"material type '{materialType}' is not supported");
					}
				default:
					throw new NotSupportedException($"dimension type '{dimensionType}' is not supported");
			}
		}

		private static ModelAttribute createAttributeFromBeamCrossSectionCharacteristics(int number)
		{
			var crossSection = new ModelAttribute
			{
				Type = AttributeType.CrossSection,
				LocalNumber = number,
				Name = OofemCrossSectionNames.SimpleCS,
				//Parameters = 
			};
			var material = new ModelAttribute
			{
				Type = AttributeType.Material,
				LocalNumber = number,
				Name = OofemMaterialNames.IsoLE,
				// Parameters = 
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

		private static class IstFileCodes
		{
			public const string MODEL = nameof(MODEL);
			public const string MAT = nameof(MAT);
			public const string SURF = nameof(SURF);
			public const string LIN = nameof(LIN);
			public const string SECT = nameof(SECT);
			public const string STIF = nameof(STIF);
		}

		private static class OofemCrossSectionNames
		{
			public const string SimpleCS = nameof(SimpleCS);
		}

		private static class OofemMaterialNames
		{
			public const string IsoLE = nameof(IsoLE);
		}
	}
}
