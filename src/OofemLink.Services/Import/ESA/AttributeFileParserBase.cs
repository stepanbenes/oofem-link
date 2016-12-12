﻿using System;
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
				Debug.Assert(tokens.Length == 12);
				this.tokens = tokens;
			}

			public string this[int index] => tokens[index];

			public string ItemType => tokens[0];
			public string DimensionType => tokens[1];
			public string QuantityType => tokens[2];
			public string Direction => tokens[3];
			public int? MaterialId => TryParseInt32(tokens[4]);
			public int? SubgradeId => TryParseInt32(tokens[5]);
			public string SelectionType => tokens[6];
			public int? Number => TryParseInt32(tokens[7]);
			public double? X => TryParseFloat64(tokens[8]);
			public double? Y => TryParseFloat64(tokens[9]);
			public double? Z => TryParseFloat64(tokens[10]);
			public double? Value => TryParseFloat64(tokens[11]);
		}

		protected AttributeFileParserBase(string location, string taskName, ILoggerFactory loggerFactory)
			: base(location, taskName, loggerFactory)
		{ }

		protected LineTokens ParseLine(string line)
		{
			var firstPart = line.Substring(0, 80).Split(chunkSize: 10);
			var secondPart = line.Substring(startIndex: 80).Split(chunkSize: 20);
			string[] tokens = firstPart.Concat(secondPart).Select(chunk => chunk.TrimStart()).ToArray();
			return new LineTokens(tokens);
		}
	}
}
