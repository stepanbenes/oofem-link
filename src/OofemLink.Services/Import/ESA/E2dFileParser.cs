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
	class E2dFileParser : EsaFileParserBase
	{
		public E2dFileParser(string location, string taskName, ILoggerFactory loggerFactory)
			: base(location, taskName, loggerFactory)
		{ }

		public override string Extension => "E2D";

		public IEnumerable<Element> Parse(int startElementId)
		{
			if (!CheckExistence())
				yield break;

			LogStart();

			long e2dFileLength = new FileInfo(FileFullPath).Length;
			long e2dSize = 4 * sizeof(int);
			long e2dRecords = e2dFileLength / e2dSize;

			if (e2dRecords * e2dSize != e2dFileLength)
			{
				throw new FormatException("Unexpected length of file " + FileFullPath);
			}

			byte[] e2dByteArray = File.ReadAllBytes(FileFullPath);
			int[] e2dConnectivity = new int[e2dByteArray.Length / sizeof(int)];
			Buffer.BlockCopy(e2dByteArray, 0, e2dConnectivity, 0, e2dByteArray.Length);

			for (int i = 0, elementId = startElementId; i < e2dRecords; i++, elementId++)
			{
				Element element = new Element { Id = elementId, LocalNumber = i + 1 };

				if (e2dConnectivity[i * 4 + 2] == e2dConnectivity[i * 4 + 3]) // triangle
				{
					element.Type = CellType.TriangleLinear;

					element.ElementNodes.Add(new ElementNode { ElementId = elementId, NodeId = e2dConnectivity[i * 4], Rank = 1 });
					element.ElementNodes.Add(new ElementNode { ElementId = elementId, NodeId = e2dConnectivity[i * 4 + 1], Rank = 2 });
					element.ElementNodes.Add(new ElementNode { ElementId = elementId, NodeId = e2dConnectivity[i * 4 + 2], Rank = 3 });
				}
				else // quad
				{
					element.Type = CellType.QuadLinear;

					element.ElementNodes.Add(new ElementNode { ElementId = elementId, NodeId = e2dConnectivity[i * 4], Rank = 1 });
					element.ElementNodes.Add(new ElementNode { ElementId = elementId, NodeId = e2dConnectivity[i * 4 + 1], Rank = 2 });
					element.ElementNodes.Add(new ElementNode { ElementId = elementId, NodeId = e2dConnectivity[i * 4 + 2], Rank = 3 });
					element.ElementNodes.Add(new ElementNode { ElementId = elementId, NodeId = e2dConnectivity[i * 4 + 3], Rank = 4 });
				}

				yield return element;
			}
		}
	}
}
