using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OofemLink.Data.Entities;

namespace OofemLink.Services.Import.ESA
{
	class NumesaFileParser : EsaFileParserBase
	{
		public NumesaFileParser(string location, string taskName, ILoggerFactory loggerFactory)
			: base(location, taskName, loggerFactory)
		{ }

		public override string Extension => "NUMESA";

		public IEnumerable<NodeMap> ParseVertexNodes()
		{
			LogStart();

			const string pattern = @"(\d+)\s+N(\d+)"; // "{FEMco-node-id}\tN{ESA-node-id}", example: "18	N13"
			Regex regex = new Regex(pattern);

			foreach (var line in File.ReadLines(FileFullPath))
			{
				if (line == "" || line.StartsWith("!")) // skip empty lines or comments
					continue;
				var match = regex.Match(line);
				if (match.Success)
				{
					Debug.Assert(match.Groups.Count == 3);

					int femcoNodeId = ParseInt32(match.Groups[1].Value);
					int esaNodeId = ParseInt32(match.Groups[2].Value);
					
					Debug.Assert(!match.NextMatch().Success);

					yield return new NodeMap(femcoNodeId, esaNodeId);
				}
			}
		}

		public struct NodeMap
		{
			public int FemcoNodeId { get; }
			public int EsaNodeId { get; }
			public NodeMap(int femcoNodeId, int esaNodeId)
			{
				FemcoNodeId = femcoNodeId;
				EsaNodeId = esaNodeId;
			}
		}
	}
}
