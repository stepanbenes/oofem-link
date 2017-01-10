using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OofemLink.Common.Extensions;
using OofemLink.Data.Entities;

namespace OofemLink.Services.Import.ESA
{
	class ProFileParser : EsaFileParserBase
	{
		public ProFileParser(string location, string taskName, ILoggerFactory loggerFactory)
			: base(location, taskName, loggerFactory)
		{ }

		public override string Extension => "PRO";

		public Simulation Parse(IEnumerable<int> loadCasesToIgnore)
		{
			LogStart();

			var simulation = new Simulation { TaskName = taskName };
			foreach (var line in File.ReadLines(FileFullPath).Select(l => l.TrimEnd('!')).MergeIfStartsWith(" "))
			{
				string[] tokens = line.Split('=');
				if (tokens.Length != 2)
				{
					throw new FormatException($"Wrong {Extension} file format, line: " + line);
				}

				string code = tokens[0].Trim();
				string value = tokens[1].Trim();

				switch (code)
				{
					case Codes.SYSTEM:
						if (value != "ESA")
						{
							Logger.LogError($"Attribute {Codes.SYSTEM} has unrecognized value. ESA code expected.");
						}
						break;
					case Codes.ULOHA:
						if (value != taskName)
						{
							Logger.LogError($"Attribute {Codes.ULOHA} has inconsistent value. '{taskName}' expected.");
						}
						break;
					case Codes.PROJ_NAME:
						simulation.Project = new Project { Name = value };
						break;
					case Codes.AXIS_Z:
						simulation.ZAxisUp = value == "UP";
						break;
					case Codes.CASE:
						int[] loadCaseNumbers = value.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(token => ParseInt32(token)).ToArray();
						foreach(var loadCaseNumber in loadCaseNumbers.Except(loadCasesToIgnore))
						{
							simulation.TimeSteps.Add(new TimeStep { Number = loadCaseNumber });
						}
						break;
					//case Codes.TYPE:
					//case Codes.NEXIS_TYPE:
					//	switch (value)
					//	{
					//		case "Grid_XY":
					//		case "Plane_XY":
					//		case "Wall_XY":
					//			simulation.DimensionFlags = ModelDimensions.XY;
					//			break;
					//		case "Truss_XZ":
					//		case "Frame_XZ":
					//		case "Wall_XZ":
					//			simulation.DimensionFlags = ModelDimensions.XZ;
					//			break;
					//		case "Truss_XYZ":
					//		case "Frame_XYZ":
					//		case "General_XYZ":
					//			simulation.DimensionFlags = ModelDimensions.XYZ;
					//			break;
					//		default:
					//			throw new NotSupportedException();
					//	}
					//	break;
				}
			}
			return simulation;
		}

		private static class Codes
		{
			// NOTE: Významných je jen prvních 6 znaků v kódu

			#region General

			/// <summary>
			/// SYSTEM = NEXIS / RFEM / FE-BEUL / ESA
			/// </summary>
			public const string SYSTEM = nameof(SYSTEM);
			/// <summary>
			/// ULOHA = jméno
			/// </summary>
			public const string ULOHA = nameof(ULOHA);
			/// <summary>
			/// Project name
			/// </summary>
			public const string PROJ_NAME = nameof(PROJ_NAME);
			/// <summary>
			/// NE = xx
			/// MODEL = 1 / 2 / 3 / 4 / 5   modely deska / stěna / skořepina / skořepina+kroucení / membrána(nahradí NE = xx)
			/// </summary>
			public const string MODEL = nameof(MODEL);
			/// <summary>
			/// VOLBY = 1234567890
			/// 	1 - T = kontrola geometrie
			/// 	2 - T = optim.šířky pásu
			/// 	3 - T = stabilita
			/// 	4 - T = kontaktní napětí
			/// 	5 - T = dynamika, F = statika
			/// 	6 - T = počáteční napětí
			/// 	7 – T = DMP file
			/// 	8 - T = krokování výpočtu
			///  	9 - T = nelineární výpočet, F = lineární výpočet
			///    10 - T = příčinkové plochy
			/// </summary>
			public const string VOLBY = nameof(VOLBY);
			/// <summary>
			/// VYPOCET = 12345678901234
			/// 	 1 čtení dat NEPRxx
			/// 	 2 kontrola geometrie(F)
			/// 	 3 soubory pro CAD(F)
			/// 	 4 optimalizace šířky pásu(F)
			/// 	 5 matice tuhosti plošných prvků
			/// 	 6 matice tuhosti prutů
			/// 	 7 sestavovat matici levých stran
			/// 	 8 sestavovat matici pravých stran 
			/// 	 9 řešit levou stranu soustavy rovnic
			/// 	10 řešit pravé strany soustavy rovnic
			/// 	11 vyhodnocení vnitřních sil
			/// 	12 výpočet kontaktních napětí KONTxx
			/// 	13 průběh VS po prutech SPRUT
			/// 	14 nevyužito
			/// </summary>
			public const string VYPOCET = nameof(VYPOCET);
			/// <summary>
			/// AXIS_Z = UP / DOWN      orientace globálního souřadného systému
			/// </summary>
			public const string AXIS_Z = nameof(AXIS_Z);
			/// <summary>
			/// TYPE = kód Truss_XZ, Frame_XZ, Truss_XYZ, Frame_XYZ, Grid_XY
			/// </summary>
			public const string TYPE = nameof(TYPE);
			/// <summary>
			/// NEXIS_TYPE = kód Plane_XY, Wall_XY, General_XYZ, Wall_XZ
			/// </summary>
			public const string NEXIS_TYPE = nameof(NEXIS_TYPE);
			/// <summary>
			/// KIRCHHOFF = YES určení ohybové teorie pro plochy – pokud chybí tento kód, default je Mindlin
			/// </summary>
			public const string KIRCHHOFF = nameof(KIRCHHOFF);
			/// <summary>
			/// SOLVER=DIRECT DIRECT_C – „Číňan“  (v LOGu je potom zapsáno INIT:HANDLE)  nebo DIRECT_P – starý solver  / ITERATIVE / LANCZOS
			/// 				statika:		přímý nebo iterační řešič
			/// 				vlastní tvary:	iterace podprostoru, ICG nebo Lanczos
			/// </summary>
			public const string SOLVER = nameof(SOLVER);

			/// <summary>
			/// FAC_WRITE = YES/NO zápis faktoru(pro dopočítávání ZS) – default NO
			/// </summary>
			public const string FAC_WRITE = nameof(FAC_WRITE);
			/// <summary>
			/// MFFORM = NODAL      formát dat
			/// </summary>
			public const string MFFORM = nameof(MFFORM);
			/// <summary>
			/// TORS = YES matice tuhosti vytvořil TORS
			/// </summary>
			public const string TORS = nameof(TORS);
			/// <summary>
			/// QUADR = YES         použije se čtyřuzlový prvek
			/// </summary>
			public const string QUADR = nameof(QUADR);
			/// <summary>
			/// SOLID = LINEAR Použije se tělesový lineární konečný prvek (izoparametrický tedy bez rotačních stupňů volnosti).
			/// </summary>
			public const string SOLID = nameof(SOLID);
			/// <summary>
			/// PLANE = LINEAR Použije se rovinný lineární konečný prvek (izoparametrický tedy bez rotačních stupňů volnosti).
			/// </summary>
			public const string PLANE = nameof(PLANE);

			/// <summary>
			/// STIFFNESS_GIVEN = YES příznak změny tuhosti elementů definované v souborech STIFxD
			/// </summary>
			public const string STIFFNESS_GIVEN = nameof(STIFFNESS_GIVEN);
			/// <summary>
			/// STRAIN_LOAD = YES      deformačního zatížení ze souborů STRAINxxxx nebo STRAINNxxxx
			/// </summary>
			public const string STRAIN_LOAD = nameof(STRAIN_LOAD);

			#endregion

			#region Linear calculation

			/// <summary>
			/// CASE = 3,6,10, seznam zatěžovacích stavů.Neposlední řádek končí čárkou
			///			15,20
			/// </summary>
			public const string CASE = nameof(CASE);
			/// <summary>
			/// STARTC12 = 5 hodnot startovací parametry C – jen pokud jsou některé makroprvky na zemině (příznak SOILIN u makroprvku) – C1z, C2x, C2y, C1x, C1y
			/// </summary>
			public const string STARTC12 = nameof(STARTC12);

			#endregion

			#region Non-linear calculation

			/// <summary>
			/// NCOMB = n           číslo nelineární kombinace
			/// </summary>
			public const string NCOMB = nameof(NCOMB);
			/// <summary>
			/// NEXIS_NCOMB = n     číslo nelineární kombinace v NEXISu
			/// </summary>
			public const string NEXIS_NCOMB = nameof(NEXIS_NCOMB);
			/// <summary>
			/// TOTAL_NCOMB = n     celkový počet řešených nelin.kombinací
			/// </summary>
			public const string TOTAL_NCOMB = nameof(TOTAL_NCOMB);
			/// <summary>
			/// TEXT = text         identifikační text nelineární kombinace
			/// </summary>
			public const string TEXT = nameof(TEXT);
			/// <summary>
			/// NCASE = 3,5,8			čísla zatěžovacích stavů pro nel.kombinaci.Neposlední řádek
			/// </summary>
			public const string NCASE = nameof(NCASE);
			/// <summary>
			/// NCOEF = 1., 1.2, 1.		hodnoty odpovídajících koeficientů končí čárkou
			/// </summary>
			public const string NCOEF = nameof(NCOEF);
			/// <summary>
			/// GLOBALCOEF = 1.5       globální koeficient pro danou nelin.kombinaci, výsledný koeficient.
			/// Pro daný ZS je výsledný koef. roven násobku  NCOEF a GLOBALCOEF
			/// </summary>
			public const string GLOBALCOEF = nameof(GLOBALCOEF);

			/// <summary>
			/// NINCR = 3           počet přírůstků zatížení při nelineárním řešení N-R metodou
			/// </summary>
			public const string NINCR = nameof(NINCR);
			/// <summary>
			/// MAXITER = n         maximum iterací (v přírůstku).
			/// </summary>
			public const string MAXITER = nameof(MAXITER);
			/// <summary>
			/// GEOMETRICAL = YES/NO geometrická nelinearita(15, 14)
			/// </summary>
			public const string GEOMETRICAL = nameof(GEOMETRICAL);
			/// <summary>
			/// METHOD = TIMOSHENKO / NEWTON metoda řešení nelineárních rovnic
			/// </summary>
			public const string METHOD = nameof(METHOD);
			/// <summary>
			/// NRMODI = YES / NO       modifikovaná Newton-Raphsonova metoda
			/// </summary>
			public const string NRMODI = nameof(NRMODI);
			/// <summary>
			/// UNILATERAL = YES/NO jednostranné vazby(10, 15, 14)
			/// </summary>
			public const string UNILATERAL = nameof(UNILATERAL);
			/// <summary>
			/// EXCLUDE = YES/NO vyloučení tahů/tlaků(15, 14)
			/// </summary>
			public const string EXCLUDE = nameof(EXCLUDE);
			/// <summary>
			/// HINGES = PLASTIC / NONLINEAR / PLASTIC,NONLINEAR plastické klouby  / nelineární klouby(funkce)  / jak plastické, tak nelineární klouby
			/// </summary>
			public const string HINGES = nameof(HINGES);
			/// <summary>
			/// CODE = EC/DIN/NEN úprava mezních momentů pro plastické klouby
			/// </summary>
			public const string CODE = nameof(CODE);
			/// <summary>
			/// IPE = YES / NO            jde o profily typu IPE(jen pro CODE = NEN)
			/// </summary>
			public const string IPE = nameof(IPE);
			/// <summary>
			/// TENSION_STIFFNESS = NO nebrát v úvahu tahové síly pro II.řád(platí to jen pro pruty). Pokud nastaveno YES – bude se vytvářet geometrická matice, i když TIMOSHENKO není zapnutý
			/// </summary>
			public const string TENSION_STIFFNESS = nameof(TENSION_STIFFNESS);
			/// <summary>
			/// MECHANISM = YES     pokud chce uživatel řešič úlohu jako mechanismus
			/// </summary>
			public const string MECHANISM = nameof(MECHANISM);
			/// <summary>
			/// BEAM_FORCES_TRANSFORMATION = YES / NO   vnitřní síly na deformované k-ci(jen pro geom.nelin)
			/// </summary>
			public const string BEAM_FORCES_TRANSFORMATION = nameof(BEAM_FORCES_TRANSFORMATION);
			/// <summary>
			/// PRECISION = n ovlivnění přesnosti výpočtu – čím vyšší číslo, tím více iterací a větší přesnost
			/// </summary>
			public const string PRECISION = nameof(PRECISION);
			/// <summary>
			/// ROBUSTNESS=n ovlivnění  doiterování úloh – čím vyšší číslo, tím vyšší tlumení
			/// </summary>
			public const string ROBUSTNESS = nameof(ROBUSTNESS);
			/// <summary>
			/// PICARD = n  max.počet sečnových iterací pro variantu METHOD = NEWTON.Když nastaven jen Picard pro analýzu velkých deformací, potom PICARD = max počtu iterací + METHOD=NEWTON
			/// </summary>
			public const string PICARD = nameof(PICARD);

			#endregion

			#region Betonová konstrukce s trhlinami

			// NOTE: Použít kódy z kapitoly obecné s nastavením jako u nelineárního výpočtu. Použít NCOMB, TEXT, NCASE a NCOEF, ale bez 2.řádu, lokálních nelinearit a plastických kloubů.

			/// <summary>
			/// CONCRETE_CRACKED = YES      příznak výpočtu konstrukce s touto zmenšenou tuhostí
			/// </summary>
			public const string CONCRETE_CRACKED = nameof(CONCRETE_CRACKED);
			/// <summary>
			/// VERSION = 3.50          příznak kvůli doplněným excentricitám
			/// </summary>
			public const string VERSION = nameof(VERSION);
			/// <summary>
			/// PHYSICAL = YES / NO           YES : trhlinky pro Recoc, dotvarování nebo zdivo
			/// </summary>
			public const string PHYSICAL = nameof(PHYSICAL);

			#endregion
		}
	}
}
