using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OofemLink.Common.Enumerations;
using OofemLink.Common.Extensions;
using OofemLink.Data.DbEntities;

namespace OofemLink.Services.Import.ESA
{
    class XyzFileParser : EsaFileParserBase
    {
		public XyzFileParser(string location, string taskName, ILoggerFactory loggerFactory)
			: base(location, taskName, loggerFactory)
		{ }

		public override string Extension => "XYZ";

		public IEnumerable<Node> Parse(ModelDimensions dimensions)
		{
			if (!CheckExistence())
				yield break;

			LogStart();

			/// Soubor .XYZ
			/// Pro každý uzel jsou v něm uloženy binárně jeho 3 souřadnice jako proměnné typu double (pro rovinné úlohy se ukládají pouze 2 souřadnice). Záznam odpovídající jednomu uzlu má tedy délku 24 bytů(příp. 16 bytů pro rovinné úlohy).
			/// Poznámka: Uzel s identifikátorem Id se nachází v Id-tém záznamu souboru .XYZ (za předpokladu, že je součástí nějaké generované entity, jinak by byl ignorován). Z toho vyplývá, že pokud je maximální Id větší než počet vygenerovaných uzlů, musí být uměle vytvořeny další uzly, aby měl soubor .XYZ dostatečnou velikost. Tyto uzly jsou naplněny hodnotou 1.e+30 pro všechny souřadnice.

			const double missingNodeCoordinateValue = 1.0e+30;
			uint dimensionCount = ((int)dimensions).BitCount();

			long xyzFileLength = new FileInfo(FileFullPath).Length;
			long xyzSize = dimensionCount * sizeof(double);
			long xyzRecords = xyzFileLength / xyzSize;

			if (xyzRecords * xyzSize != xyzFileLength)
			{
				throw new FormatException("Unexpected length of file " + FileFullPath);
			}

			double[] coordinateArray;

			{
				byte[] xyzByteArray = File.ReadAllBytes(FileFullPath);
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
	}
}
