using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OofemLink.Services.Import.ESA
{
	class MtoFileParser : EsaFileParserBase
	{
		public MtoFileParser(string location, string taskName, ILoggerFactory loggerFactory)
			: base(location, taskName, loggerFactory)
		{ }

		public override string Extension => "MTO";

		public IEnumerable<MacroElementsLink> Parse()
		{
			LogStart();

			foreach (var line in File.ReadLines(FileFullPath))
			{
				string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (tokens.Length < 7)
				{
					throw new FormatException("Wrong MTO file format. Each row has to have at least 7 records.");
				}
				switch (tokens[5])
				{
					case "B": // 1D MACRO
						{
							int macroId = ParseInt32(tokens[0]);
							int memberId = ParseInt32(tokens[1]);
							int lineId = ParseInt32(tokens[2]);
							int startElementId = ParseInt32(tokens[3]);
							int endElementId = ParseInt32(tokens[4]);

							yield return new MacroElementsLink(macroId, lineId, MacroElementsLink.ElementDimension.OneD, startElementId, endElementId);
						}
						break;
					case "C": // 2D MACRO
					case "G":
					case "Q":
						{
							int macroId = ParseInt32(tokens[0]);
							int startElementId = ParseInt32(tokens[3]);
							int endElementId = ParseInt32(tokens[4]);
							int localAxisDirection = ParseInt32(tokens[6]); // TODO: deal with local axis direction parameter

							yield return new MacroElementsLink(macroId, null, MacroElementsLink.ElementDimension.TwoD, startElementId, endElementId);
						}
						break;
					case "D": // 3D MACRO
						{
							int macroId = ParseInt32(tokens[0]);
							int startElementId = ParseInt32(tokens[3]);
							int endElementId = ParseInt32(tokens[4]);

							yield return new MacroElementsLink(macroId, null, MacroElementsLink.ElementDimension.ThreeD, startElementId, endElementId);
						}
						break;
					case "S": // SUM
						{
							// TODO: check consistency with mesh object and program number
							int numberOf2dElements = ParseInt32(tokens[0]);
							int numberOf1dElements = ParseInt32(tokens[1]);
							int numberOfNodes = ParseInt32(tokens[2]);
							int NEXXProgramNumber = ParseInt32(tokens[3]);
							int numberOf3dElements = ParseInt32(tokens[4]);
						}
						break;
					default:
						throw new NotSupportedException($"'{tokens[5]}' macro type is not recognized");
				}
			}
		}

		public struct MacroElementsLink
		{
			public enum ElementDimension
			{
				OneD = 1, TwoD, ThreeD
			}
			public int MacroId { get; }
			public int? GeometryEntityId { get; }
			public ElementDimension Dimension { get; }
			public int StartElementId { get; }
			public int EndElementId { get; }
			public MacroElementsLink(int macroId, int? geometryEntityId, ElementDimension dimension, int startElementId, int endElementId)
			{
				MacroId = macroId;
				GeometryEntityId = geometryEntityId;
				Dimension = dimension;
				StartElementId = startElementId;
				EndElementId = endElementId;
			}
		}
	}
}
