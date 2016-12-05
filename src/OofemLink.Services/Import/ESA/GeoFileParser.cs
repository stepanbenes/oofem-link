using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OofemLink.Common.Enumerations;
using OofemLink.Common.Extensions;
using OofemLink.Data.Entities;

namespace OofemLink.Services.Import.ESA
{
	class GeoFileParser : EsaFileParserBase
	{
		public GeoFileParser(string location, string taskName, ILoggerFactory loggerFactory)
			: base(location, taskName, loggerFactory)
		{ }

		public override string Extension => "geo";

		public Model Parse(out ModelDimensions dimensions)
		{
			LogStart();

			var modelBuilder = new ModelBuilder();
			dimensions = ModelDimensions.None;
			foreach (var line in File.ReadLines(FileFullPath).MergeIfStartsWith(" "))
			{
				string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (tokens.Length < 2)
				{
					throw new FormatException("Wrong GEO file format, line: " + line);
				}

				string value = tokens[1].Trim();

				switch (tokens[0])
				{
					case Codes.PROG:
						{
							int progCode = ParseInt32(tokens[1]);
							switch (progCode)
							{
								case 10:
									dimensions = ModelDimensions.XY;
									break;
								case 14:
									dimensions = ModelDimensions.XYZ;
									break;
								case 15:
									dimensions = ModelDimensions.XZ;
									break;
								default:
									throw new NotSupportedException("Unexpected PROG code '" + progCode + "'");
							}
						}
						break;
					case Codes.NODE:
						{
							int id = ParseInt32(tokens[1]);
							int index = 2;
							double x = 0.0, y = 0.0, z = 0.0;
							if (dimensions.HasFlag(ModelDimensions.X))
							{
								x = ParseFloat64(tokens[index]);
								index += 1;
							}
							if (dimensions.HasFlag(ModelDimensions.Y))
							{
								y = ParseFloat64(tokens[index]);
								index += 1;
							}
							if (dimensions.HasFlag(ModelDimensions.Z))
							{
								z = ParseFloat64(tokens[index]);
								index += 1;
							}
							modelBuilder.AddVertex(id, x, y, z);
						}
						break;
					case Codes.LINE:
						{
							int lineId = ParseInt32(tokens[1]);
							CurveType type;
							if (!Enum.TryParse(tokens[2], out type))
								throw new NotSupportedException($"Line code '{tokens[2]}' is not supported");
							var vertexIds = new List<int>();
							for (int index = 3; index < tokens.Length; index++)
							{
								int vertexId = ParseInt32(tokens[index]);
								vertexIds.Add(vertexId);
							}
							modelBuilder.AddCurve(lineId, type, vertexIds);
						}
						break;
					case Codes.MACR:
						{
							int macroId = ParseInt32(tokens[1]);
							switch (tokens[2])
							{
								case Codes.MacroTypeCodes.BEAM:
									{
										var lineIds = new List<int>();
										for (int index = 3; index < tokens.Length; index++)
										{
											int lineId = ParseInt32(tokens[index]);
											lineIds.Add(lineId);
										}
										modelBuilder.AddBeamMacro(macroId, lineIds);
									}
									break;
								case Codes.MacroTypeCodes.GEN:
									{
										var boundaryLineIds = new List<int>();
										var openingLineIds = new List<int>();
										var internalLineIds = new List<int>();
										var internalVertexIds = new List<int>();
										List<int> currentList = boundaryLineIds;
										for (int index = 3; index < tokens.Length; index++)
										{
											int? number = TryParseInt32(tokens[index]);
											if (number.HasValue)
											{
												currentList.Add(number.Value);
											}
											else
											{
												switch (tokens[index])
												{
													case Codes.MacroDetailCodes.OPEN:
														currentList = openingLineIds;
														break;
													case Codes.MacroDetailCodes.LINE:
														currentList = internalLineIds;
														break;
													case Codes.MacroDetailCodes.NODE:
														currentList = internalVertexIds;
														break;
													default:
														throw new NotSupportedException($"Unsupported macro detail code '{tokens[2]}'");
												}
											}
											index += 1;
										}
										modelBuilder.AddGeneralSurfaceMacro(macroId, boundaryLineIds, openingLineIds, internalLineIds, internalVertexIds);
									}
									break;
								default:
									throw new NotSupportedException($"Macro code {tokens[2]} is not supported");
							}
						}
						break;
					default:
						Logger.LogWarning("Ignoring token '{0}'", tokens[0]);
						break;
				}
			}
			return modelBuilder.Model;
		}

		private static class Codes
		{
			/// <summary>
			/// Identifikátor “NAME“ - jméno úlohy. Tento identifikátor musí být na začátku prvního řádku souboru. Za ním následuje od 6. pozice řetězec představující jméno úlohy. Řetězec slouží pouze pro dokumentaci, vzhledem k tomu, že jména souborů jsou předána v konstruktoru GBase, jej Genex ignoruje.
			/// </summary>
			public const string NAME = nameof(NAME);

			/// <summary>
			/// Identifikátor “PROG“ - číslo programu NEXX. Tento identifikátor musí být na začátku druhého řádku souboru. Na 6. - 10. pozici je pak zapsáno číslo programu NEXX, které může nabývat hodnoty 10 (deska), 15 (stěna) nebo 14 (skořepina). Z hlediska generátoru sítě je význam čísla programu NEXX hlavně v tom, že definuje dimenzi úlohy – pro čísla 10 a 15 je dimenze 2, pro číslo 14 je dimenze 3.
			/// </summary>
			public const string PROG = nameof(PROG);

			/// <summary>
			/// Identifikátor “NODE“ - uzel: následuje číslo uzlu zapsané na 6. - 10. pozici  a tři souřadnice zarovnané k 40., 60. a 80. pozici. Pro rovinné úlohy odpovídající programům NEXX 10 a 15 se třetí souřadnice nezapisuje. 
			/// Čísla uzlů, jakož i čísla linií a makroprvků mohou být libovolná a nemusí být nijak uspořádaná(např.uzel číslo 1 se v souboru.GEO nemusí vůbec vyskytovat).
			/// </summary>
			public const string NODE = nameof(NODE);

			/// <summary>
			/// Identifikátor “LINE“ - linie: následuje číslo linie zapsané na 6. - 10. pozici a tří- nebo čtyřznakový identifikátor typu linie na 12. - 15. pozici (na 11. pozici je mezera).
			/// </summary>
			public const string LINE = nameof(LINE);

			/// <summary>
			/// Identifikátor “MACR“ – plošný nebo prutový makroprvek: syntaxe zápisu makroprvku je stejná jako v případě linie. Na 6. - 10. pozici je číslo makorprvku, na pozici 12. - 15. opět identifikátor typu.
			/// </summary>
			public const string MACR = nameof(MACR);

			/// <summary>
			/// Identifikátor “SURF“ – plocha: za tímto identifikátorem můžeme zapsat plochu se stejnou syntaxí jako v případě “MACR”, plocha ale nebude vygenerována. Může se jednat např. o ohraničující plochu nějakého tělesa. 
			/// Poznámka: Plochy a plošné makroprvky musí mít navzájem různá čísla, prutové makroprvky jsou ale číslovány nezávisle.Pokud tedy zapíšeme např.plošný makroprvek jako “MACR” s číslem 15, pak nelze zapsat plochu “SURF” s číslem 15, můžeme ale pod tímto číslem zapsat prutový makroprvek.
			/// </summary>
			public const string SURF = nameof(SURF);

			public static class LineTypeCodes
			{
				/// <summary>
				/// úsečka (STRaight Line)
				/// </summary>
				public const string STRL = nameof(STRL);

				/// <summary>
				/// polygon (lomená čára)
				/// </summary>
				public const string POLY = nameof(POLY);

				/// <summary>
				/// kruhový oblouk
				/// </summary>
				public const string ARCH = nameof(ARCH);

				/// <summary>
				/// kružnice
				/// </summary>
				public const string CIRC = nameof(CIRC);

				/// <summary>
				/// lokální kubický splajn
				/// </summary>
				public const string LINE = nameof(LINE);

				/// <summary>
				/// B - spline křivka
				/// </summary>
				public const string BSPL = nameof(BSPL);

				/// <summary>
				/// Bezierova křivka
				/// </summary>
				public const string BEZ = nameof(BEZ);

				/// <summary>
				/// linie ležící na povrchu plochy
				/// </summary>
				public const string ONSF = nameof(ONSF);

				/// <summary>
				/// povrchová linie PIPE
				/// </summary>
				public const string PIPE = nameof(PIPE);

				/// <summary>
				/// křivka průniku dvou ploch (interSECTion)
				/// </summary>
				public const string SECT = nameof(SECT);
			}

			public static class MacroTypeCodes
			{
				/// <summary>
				/// zobecněný čtyřúhelník
				/// </summary>
				public const string QUAD = nameof(QUAD);

				/// <summary>
				/// B - spline plocha
				/// </summary>
				public const string BSPL = nameof(BSPL);

				/// <summary>
				/// rotační makroprvek
				/// </summary>
				public const string ROT = nameof(ROT);

				/// <summary>
				/// pipe makroprvek (trubka)
				/// </summary>
				public const string PIPE = nameof(PIPE);

				/// <summary>
				/// obecnější verze trubky
				/// </summary>
				public const string PIPL = nameof(PIPL);

				/// <summary>
				/// rovinný makroprvek (obecný - general)
				/// </summary>
				public const string GEN = nameof(GEN);

				/// <summary>
				/// čtyřúhelníkový sloup
				/// </summary>
				public const string COL_Q = nameof(COL_Q);

				/// <summary>
				/// kruhový sloup
				/// </summary>
				public const string COL_C = nameof(COL_C);

				/// <summary>
				/// prutový makroprvek
				/// </summary>
				public const string BEAM = nameof(BEAM);
			}

			public static class MacroDetailCodes
			{
				/// <summary>
				/// Seznam obvodových linií otvoru
				/// </summary>
				public const string OPEN = nameof(OPEN);

				/// <summary>
				/// Seznam vnitřních linií makroprvku
				/// </summary>
				public const string LINE = nameof(LINE);

				/// <summary>
				/// Seznam vnitřních uzlů makroprvku
				/// </summary>
				public const string NODE = nameof(NODE);
			}
		}
	}
}
