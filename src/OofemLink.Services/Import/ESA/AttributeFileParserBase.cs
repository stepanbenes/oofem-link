using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OofemLink.Common.Extensions;

namespace OofemLink.Services.Import.ESA
{
	abstract class AttributeFileParserBase : EsaFileParserBase
	{
		protected struct LineTokens
		{
			// IST:  druh_polozky, dimenze, typ_veliciny, smer, material_#, podlozi_#, typ_vyberu, cislo, x, y, z, hodnota
			// Ixxx: druh_polozky dimenze typ_veliciny smer zpusob_zatizeni typ_zatizeni typ_vyberu cislo x y z? hodnota

			readonly string[] tokens;

			public LineTokens(string[] tokens)
			{
				this.tokens = tokens;
			}

			public string this[int index] => tokens[index];

			public string ItemType => tokens[0];
			public string DimensionType => tokens[1];
			public string QuantityType => tokens[2];
			public string Direction => tokens[3];
			public int? MaterialId => TryParseInt32(tokens[4]);
			public int? SubsoilId => TryParseInt32(tokens[5]);
			public string SelectionType => tokens[6];
			public int? Number => TryParseInt32(tokens[7]);
			public double? X => TryParseFloat64(tokens[8]);
			public double? Y => TryParseFloat64(tokens[9]);
			public double? Z => TryParseFloat64(tokens[10]);
			public double? Value => TryParseFloat64(tokens[11]);

			public override string ToString() => string.Join(" ", tokens.Select(token => token == "" ? "_" : token));
		}

		protected struct LineValues
		{
			readonly double?[] values;
			public LineValues(double?[] values)
			{
				this.values = values;
			}
			public double? this[int index] => values[index];
			public override string ToString() => string.Join(" ", values.Select(value => value.HasValue ? value.ToString() : "_"));
		}

		protected AttributeFileParserBase(string location, string taskName, ILoggerFactory loggerFactory)
			: base(location, taskName, loggerFactory)
		{ }

		protected LineTokens ParseLineTokens(string line)
		{
			var firstPart = line.Substring(0, 80).Split(chunkSize: 10);
			var secondPart = line.Substring(startIndex: 80).Split(chunkSize: 20);
			string[] tokens = firstPart.Concat(secondPart).Select(chunk => chunk.TrimStart()).ToArray();
			Debug.Assert(tokens.Length == 12);
			return new LineTokens(tokens);
		}

		protected LineValues ParseLineValues(string line)
		{
			double?[] values = line.Split(chunkSize: 20).Select(chunk => TryParseFloat64(chunk.TrimStart())).ToArray();
			Debug.Assert(values.Length == 8);
			return new LineValues(values);
		}

		protected static int ParseDofId(string direction, out bool isLcs)
		{
			switch (direction)
			{
				case Directions.X:
				case Directions.XG:
					isLcs = false;
					return 1;
				case Directions.Y:
				case Directions.YG:
					isLcs = false;
					return 2;
				case Directions.Z:
				case Directions.ZG:
					isLcs = false;
					return 3;
				case Directions.XL:
				case Directions.XE:
					isLcs = true;
					return 1;
				case Directions.YL:
				case Directions.YE:
					isLcs = true;
					return 2;
				case Directions.ZL:
				case Directions.ZE:
					isLcs = true;
					return 3;
				default:
					throw new NotSupportedException($"direction '{direction}' is not supported");
			}
		}

		protected static class Directions
		{
			public const string X = nameof(X);
			public const string Y = nameof(Y);
			public const string Z = nameof(Z);
			public const string XG = nameof(XG);
			public const string YG = nameof(YG);
			public const string ZG = nameof(ZG);
			public const string XL = nameof(XL);
			public const string YL = nameof(YL);
			public const string ZL = nameof(ZL);
			public const string XPG = nameof(XPG);
			public const string YPG = nameof(YPG);
			public const string ZPG = nameof(ZPG);
			public const string XE = nameof(XE);
			public const string YE = nameof(YE);
			public const string ZE = nameof(ZE);
		}
	}
}
