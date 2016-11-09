using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using OofemLink.Data;
using OofemLink.Data.Entities;

namespace OofemLink.Business.Export
{
	class OofemInputBuilder : IOofemInputCreator<OofemInputBuilder>
	{
		// inspired by DynamicInputRecord in OOFEM source code

		readonly List<OofemInputRecord> records = new List<OofemInputRecord>();

		public OofemInputBuilder AddInputRecord(OofemInputRecord inputRecord)
		{
			records.Add(inputRecord);
			return this;
		}


		public void WriteTo(StreamWriter streamWriter)
		{
			foreach (var record in records)
			{
				streamWriter.WriteLine(record.ToString());
			}
		}
	}
}
