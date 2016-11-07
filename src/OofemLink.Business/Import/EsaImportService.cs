﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Common.Enumerations;
using OofemLink.Common.Extensions;
using OofemLink.Data;
using OofemLink.Data.Entities;

namespace OofemLink.Business.Import
{
	class EsaImportService : FormatParserBase, IImportService
	{
		#region Fields, constructor

		readonly string location;
		int globalElementCounter;

		public EsaImportService(string location)
		{
			this.location = location;
		}

		#endregion

		#region Public methods

		public Simulation ImportSimulation()
		{
			const string proFileExtension = ".PRO";
			string proFileFullPath = Directory.EnumerateFiles(location, "*" + proFileExtension).Where(file => file.EndsWith(proFileExtension)).SingleOrDefault();
			if (proFileFullPath == null)
			{
				throw new FileNotFoundException("PRO file was not found");
			}

			var simulation = parseProFile(proFileFullPath);
			ModelDimensions dimensions;
			var model = importModel(simulation.TaskName, out dimensions);
			var mesh = importMesh(simulation.TaskName, dimensions);
			model.Meshes.Add(mesh);
			// TODO: link model and mesh entities togeteher (using e.g. file MTO)
			simulation.DimensionFlags = dimensions;
			simulation.Models.Add(model);
			return simulation;
		}

		#endregion

		#region Private methods

		private Model importModel(string taskName, out ModelDimensions dimensions)
		{
			string geoFileFullPath = Path.Combine(location, $"{taskName}.geo");
			return parseGeoFile(geoFileFullPath, out dimensions);
		}

		private Mesh importMesh(string taskName, ModelDimensions dimensions)
		{
			string xyzFileFullPath = Path.Combine(location, $"{taskName}.XYZ");
			string e1dFileFullPath = Path.Combine(location, $"{taskName}.E1D");
			string e2dFileFullPath = Path.Combine(location, $"{taskName}.E2D");

			Mesh mesh = new Mesh();
			// NODES
			if (File.Exists(xyzFileFullPath))
			{
				foreach (var node in parseXyzFile(xyzFileFullPath, dimensions))
				{
					mesh.Nodes.Add(node);
				}
			}
			// 1D ELEMENTS
			if (File.Exists(e1dFileFullPath))
			{
				foreach (var element in parseE1DFile(e1dFileFullPath))
				{
					mesh.Elements.Add(element);
				}
			}
			// 2D ELEMENTS
			if (File.Exists(e2dFileFullPath))
			{
				foreach (var element in parseE2DFile(e2dFileFullPath))
				{
					mesh.Elements.Add(element);
				}
			}
			return mesh;
		}

		#region PRO file parsing

		private static class ProFileCodes
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

		private Simulation parseProFile(string fileFullPath)
		{
			var simulation = new Simulation();
			foreach (var line in File.ReadLines(fileFullPath).Select(l => l.TrimEnd('!')).MergeIfStartsWith(" "))
			{
				string[] tokens = line.Split('=');
				if (tokens.Length != 2)
				{
					throw new FormatException("Wrong PRO file format, line: " + line);
				}

				string code = tokens[0].Trim();
				string value = tokens[1].Trim();

				switch (code)
				{
					case ProFileCodes.SYSTEM:
						Debug.Assert(value == "ESA");
						break;
					case ProFileCodes.ULOHA:
						simulation.TaskName = value;
						break;
					case ProFileCodes.AXIS_Z:
						simulation.ZAxisUp = value == "UP";
						break;
						//case ProFileCodes.TYPE:
						//case ProFileCodes.NEXIS_TYPE:
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

		#endregion

		#region Mesh files parsing

		private static IEnumerable<Node> parseXyzFile(string fileFullPath, ModelDimensions dimensions)
		{
			/// Soubor .XYZ
			/// Pro každý uzel jsou v něm uloženy binárně jeho 3 souřadnice jako proměnné typu double (pro rovinné úlohy se ukládají pouze 2 souřadnice). Záznam odpovídající jednomu uzlu má tedy délku 24 bytů(příp. 16 bytů pro rovinné úlohy).
			/// Poznámka: Uzel s identifikátorem Id se nachází v Id-tém záznamu souboru .XYZ (za předpokladu, že je součástí nějaké generované entity, jinak by byl ignorován). Z toho vyplývá, že pokud je maximální Id větší než počet vygenerovaných uzlů, musí být uměle vytvořeny další uzly, aby měl soubor .XYZ dostatečnou velikost. Tyto uzly jsou naplněny hodnotou 1.e+30 pro všechny souřadnice.

			const double missingNodeCoordinateValue = 1.0e+30;
			uint dimensionCount = ((int)dimensions).BitCount();

			long xyzFileLength = new FileInfo(fileFullPath).Length;
			long xyzSize = dimensionCount * sizeof(double);
			long xyzRecords = xyzFileLength / xyzSize;

			if (xyzRecords * xyzSize != xyzFileLength)
			{
				throw new FormatException("Unexpected length of file " + fileFullPath);
			}

			double[] coordinateArray;

			{
				byte[] xyzByteArray = File.ReadAllBytes(fileFullPath);
				coordinateArray = new double[xyzByteArray.Length / sizeof(double)];
				Buffer.BlockCopy(xyzByteArray, 0, coordinateArray, 0, xyzByteArray.Length);
			}

			for (int i = 0; i < xyzRecords; i++)
			{
				double x = 0.0, y = 0.0, z = 0.0;
				int offset = 0;
				if (dimensions.HasFlag(ModelDimensions.X))
				{
					double value = coordinateArray[i * dimensionCount + offset];
					if (value == missingNodeCoordinateValue)
						continue;
					x = value;
					offset += 1;
				}
				if (dimensions.HasFlag(ModelDimensions.Y))
				{
					double value = coordinateArray[i * dimensionCount + offset];
					if (value == missingNodeCoordinateValue)
						continue;
					y = value;
					offset += 1;
				}
				if (dimensions.HasFlag(ModelDimensions.Z))
				{
					double value = coordinateArray[i * dimensionCount + offset];
					if (value == missingNodeCoordinateValue)
						continue;
					z = value;
					offset += 1;
				}

				yield return new Node { Id = i + 1, X = x, Y = y, Z = z };
			}
		}

		private IEnumerable<Element> parseE1DFile(string fileFullPath)
		{
			long e1dFileLength = new FileInfo(fileFullPath).Length;
			long e1dSize = 2 * sizeof(int);
			long e1dRecords = e1dFileLength / e1dSize;

			if (e1dRecords * e1dSize != e1dFileLength)
			{
				throw new FormatException("Unexpected length of file " + fileFullPath);
			}

			byte[] e1dByteArray = File.ReadAllBytes(fileFullPath);
			int[] e1dConnectivity = new int[e1dByteArray.Length / sizeof(int)];
			Buffer.BlockCopy(e1dByteArray, 0, e1dConnectivity, 0, e1dByteArray.Length);

			for (int i = 0; i < e1dRecords; i++)
			{
				globalElementCounter += 1;
				Element element = new Element { Id = globalElementCounter, LocalNumber = i + 1, Type = CellType.LineLinear };
				element.ElementNodes.Add(new ElementNode { ElementId = globalElementCounter, NodeId = e1dConnectivity[i * 2], Rank = 1 });
				element.ElementNodes.Add(new ElementNode { ElementId = globalElementCounter, NodeId = e1dConnectivity[i * 2 + 1], Rank = 2 });
				yield return element;
			}
		}

		private IEnumerable<Element> parseE2DFile(string fileFullPath)
		{
			long e2dFileLength = new FileInfo(fileFullPath).Length;
			long e2dSize = 4 * sizeof(int);
			long e2dRecords = e2dFileLength / e2dSize;

			if (e2dRecords * e2dSize != e2dFileLength)
			{
				throw new FormatException("Unexpected length of file " + fileFullPath);
			}

			byte[] e2dByteArray = File.ReadAllBytes(fileFullPath);
			int[] e2dConnectivity = new int[e2dByteArray.Length / sizeof(int)];
			Buffer.BlockCopy(e2dByteArray, 0, e2dConnectivity, 0, e2dByteArray.Length);

			for (int i = 0; i < e2dRecords; i++)
			{
				globalElementCounter += 1;
				Element element = new Element { Id = globalElementCounter, LocalNumber = i + 1 };

				if (e2dConnectivity[i * 4 + 2] == e2dConnectivity[i * 4 + 3]) // triangle
				{
					element.Type = CellType.TriangleLinear;

					element.ElementNodes.Add(new ElementNode { ElementId = globalElementCounter, NodeId = e2dConnectivity[i * 4], Rank = 1 });
					element.ElementNodes.Add(new ElementNode { ElementId = globalElementCounter, NodeId = e2dConnectivity[i * 4 + 1], Rank = 2 });
					element.ElementNodes.Add(new ElementNode { ElementId = globalElementCounter, NodeId = e2dConnectivity[i * 4 + 2], Rank = 3 });
				}
				else // quad
				{
					element.Type = CellType.QuadLinear;

					element.ElementNodes.Add(new ElementNode { ElementId = globalElementCounter, NodeId = e2dConnectivity[i * 4], Rank = 1 });
					element.ElementNodes.Add(new ElementNode { ElementId = globalElementCounter, NodeId = e2dConnectivity[i * 4 + 1], Rank = 2 });
					element.ElementNodes.Add(new ElementNode { ElementId = globalElementCounter, NodeId = e2dConnectivity[i * 4 + 2], Rank = 3 });
					element.ElementNodes.Add(new ElementNode { ElementId = globalElementCounter, NodeId = e2dConnectivity[i * 4 + 3], Rank = 4 });
				}

				yield return element;
			}
		}

		//private IEnumerable<Element> parseE3DFile(string fileFullPath)

		#endregion

		#region Model files parsing

		private static class GeoFileCodes
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

		private static Model parseGeoFile(string fileFullPath, out ModelDimensions dimensions)
		{
			var modelBuilder = new ModelBuilder();
			dimensions = ModelDimensions.None;
			foreach (var line in File.ReadLines(fileFullPath).Where(line => !line.StartsWith(" ") && !string.IsNullOrWhiteSpace(line)))
			{
				string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (tokens.Length < 2)
				{
					throw new FormatException("Wrong GEO file format, line: " + line);
				}

				string value = tokens[1].Trim();

				switch (tokens[0])
				{
					case GeoFileCodes.PROG:
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
					case GeoFileCodes.NODE:
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
					case GeoFileCodes.LINE:
						{
							int lineId = ParseInt32(tokens[1]);
							switch (tokens[2])
							{
								case GeoFileCodes.LineTypeCodes.STRL:
									{
										int firstVertexId = ParseInt32(tokens[3]);
										int secondVertexId = ParseInt32(tokens[4]);
										modelBuilder.AddStraightLine(lineId, firstVertexId, secondVertexId);
									}
									break;
								default:
									throw new NotSupportedException($"Line code '{tokens[2]}' is not supported");
							}
						}
						break;
					case GeoFileCodes.MACR:
						{
							int macroId = ParseInt32(tokens[1]);
							switch (tokens[2])
							{
								case GeoFileCodes.MacroTypeCodes.BEAM:
									{
										int lineId = ParseInt32(tokens[3]);
										modelBuilder.AddBeamMacro(macroId, lineId);
									}
									break;
								case GeoFileCodes.MacroTypeCodes.GEN:
									{
										int index = 3;
										List<int> perimeterLineIds = new List<int>();
										while (index < tokens.Length)
										{
											switch (tokens[index])
											{
												case GeoFileCodes.MacroDetailCodes.OPEN:
												case GeoFileCodes.MacroDetailCodes.LINE:
												case GeoFileCodes.MacroDetailCodes.NODE:
													throw new NotSupportedException($"Unsupported macro detail code '{tokens[2]}'");
													//Console.WriteLine("Ignoring " + tokens[0] + " " + tokens[index]);
												default:
													int lineId = ParseInt32(tokens[index]);
													perimeterLineIds.Add(lineId);
													break;
											}
											index += 1;
										}
										modelBuilder.AddSurfaceMacro(macroId, perimeterLineIds);
									}
									break;
								default:
									throw new NotSupportedException($"Macro code {tokens[2]} is not supported");
							}
						}
						break;
					default:
						Console.WriteLine("Ignoring " + tokens[0]);
						break;
				}
			}
			return modelBuilder.Model;
		}

		#endregion

		#endregion
	}
}
