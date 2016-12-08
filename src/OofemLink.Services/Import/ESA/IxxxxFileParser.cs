using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OofemLink.Data.Entities;

namespace OofemLink.Services.Import.ESA
{
	class IxxxxFileParser : EsaFileParserBase
	{
		readonly int loadCaseNumber;

		public IxxxxFileParser(int loadCaseNumber, string location, string taskName, ILoggerFactory loggerFactory)
			: base(location, taskName, loggerFactory)
		{
			if (loadCaseNumber < 1 || loadCaseNumber > 9999)
				throw new ArgumentOutOfRangeException(nameof(loadCaseNumber), "Argument must be in range <1, 9999>");
			this.loadCaseNumber = loadCaseNumber;
		}

		public override string Extension => $"I{loadCaseNumber:D4}";

		public IEnumerable<ModelAttribute> Parse()
		{
			LogStart();

			using (var streamReader = File.OpenText(FileFullPath))
			{
				string line = streamReader.ReadLine();
				while ((line = streamReader.ReadLine()) != null)
				{
					// TODO: implement this method
					// TODO: generate time function for each load step and add it to attribute-macro mappings
					yield break;
				}
			}
		}
	}
}
