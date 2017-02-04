using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OofemLink.Common.Enumerations;
using OofemLink.Data.DbEntities;

namespace OofemLink.Services.Import.ESA
{
	class E1dFileParser : EsaFileParserBase
	{
		public E1dFileParser(string location, string taskName, ILoggerFactory loggerFactory)
			: base(location, taskName, loggerFactory)
		{ }

		public override string Extension => "E1D";

		public IEnumerable<Element> Parse(int startElementId)
		{
			if (!CheckExistence())
				yield break;

			LogStart();

			long e1dFileLength = new FileInfo(FileFullPath).Length;
			long e1dSize = 2 * sizeof(int);
			long e1dRecords = e1dFileLength / e1dSize;

			if (e1dRecords * e1dSize != e1dFileLength)
			{
				throw new FormatException("Unexpected length of file " + FileFullPath);
			}

			byte[] e1dByteArray = File.ReadAllBytes(FileFullPath);
			int[] e1dConnectivity = new int[e1dByteArray.Length / sizeof(int)];
			Buffer.BlockCopy(e1dByteArray, 0, e1dConnectivity, 0, e1dByteArray.Length);

			for (int i = 0, elementId = startElementId; i < e1dRecords; i++, elementId++)
			{
				Element element = new Element { Id = elementId, LocalNumber = i + 1, Type = CellType.LineLinear };
				element.ElementNodes.Add(new ElementNode { ElementId = elementId, NodeId = e1dConnectivity[i * 2], Rank = 1 });
				element.ElementNodes.Add(new ElementNode { ElementId = elementId, NodeId = e1dConnectivity[i * 2 + 1], Rank = 2 });
				yield return element;
			}
		}
	}
}
