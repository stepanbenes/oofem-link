using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Business.Export
{
    class OofemInputWriter : IOofemInputCreator<OofemInputWriter>
    {
		readonly StreamWriter writer;

		public OofemInputWriter(StreamWriter writer)
		{
			this.writer = writer;
		}

		public OofemInputWriter SetOutputFileName(string outputFileName)
		{
			writer.WriteLine(outputFileName);
			return this;
		}

		public OofemInputWriter SetDescription(string description)
		{
			writer.WriteLine(description);
			return this;
		}

		public OofemInputWriter AddInputRecord(OofemInputRecord inputRecord)
		{
			writer.WriteLine(inputRecord.ToString());
			return this;
		}
	}
}
