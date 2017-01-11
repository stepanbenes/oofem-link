using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OofemLink.Common.Extensions;

namespace OofemLink.Services.Import.ESA
{
	class LinFileParser : EsaFileParserBase
	{
		public LinFileParser(string location, string taskName, ILoggerFactory loggerFactory)
			: base(location, taskName, loggerFactory)
		{ }

		public override string Extension => "LIN";

		public void Parse(ModelMeshMapper modelMeshMapper)
		{
			LogStart();

			// O souboru .LIN lze s trochou nadsázky říct, že je v něm zapsáno vše, co se nevešlo jinam.
			// Jedná se o textový soubor, v němž každý řádek začíná jedno- nebo dvou-znakovým identifíkátorem,
			// pokud začíná dvěma mezerami, tak je pokračovací.

			foreach (var line in File.ReadLines(FileFullPath).MergeIfStartsWith("  "))
			{
				string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (tokens.Length < 1)
					throw new FormatException($"Wrong {Extension} file format, line: " + line);

				switch (tokens[0])
				{
					case Codes.LI:
						if (tokens.Length < 2)
							throw new FormatException($"Wrong {Extension} file format, {Codes.LI} section lacks line id token");
						int lineId = ParseInt32(tokens[1]);
						if (tokens.Length > 2)
						{
							int firstNodeId = ParseInt32(tokens[2]);
							for (int i = 3; i < tokens.Length; i++)
							{
								int secondNodeId = ParseInt32(tokens[i]);
								modelMeshMapper.MapCurveTo2dOr3dElementEdge(lineId, firstNodeId, secondNodeId);
								firstNodeId = secondNodeId;
							}
						}
						break;
					default:
						// ignore other records
						break;
				}
			}
		}

		private static class Codes
		{
			/// <summary>
			/// následuje číslo linie a seznam čísel uzlů vygenerovaných na této linii. V seznamu může být 0, např. je –li linie přerušena z důvodu průniku.
			/// </summary>
			public const string LI = nameof(LI);

			/// <summary>
			/// následuje číslo prutu a seznam významných uzlů na tomto prutu (pouze pro 3D konstrukce).
			/// </summary>
			public const string B = nameof(B);

			/// <summary>
			/// následuje seznam postrádaných (missing) uzlů – tedy uzlů, které byly zapsány do .GEO jako součást nějakého makroprvku, ale nejsou obsaženy v souboru .XYZ (obvykle z důvodu průniku).
			/// </summary>
			public const string M = nameof(M);

			/// <summary>
			/// následuje seznam zakřivených linií (kružnice, splajny, apod.).
			/// </summary>
			public const string CU = nameof(CU);

			/// <summary>
			/// zápis generovaných uzlů na prutech – pouze pro Nexis.
			/// </summary>
			public const string PO = nameof(PO);

			/// <summary>
			/// následuje číslo sloupu a seznam makrorpvků, které jsou tímto sloupem ovlivněny.
			/// </summary>
			public const string CL = nameof(CL);
		}
	}
}
