using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using OofemLink.Data;
using OofemLink.Data.Entities;

namespace OofemLink.Business.Export.OOFEM
{
	class InputBuilder
	{
		// inspired by DynamicInputRecord in OOFEM source code

		readonly List<InputRecord> records = new List<InputRecord>();

		public InputBuilder AddInputRecord(InputRecord inputRecord)
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

	//class InputWriter
	//{
	//	readonly StreamWriter writer;

	//	public OofemInputWriter(StreamWriter writer)
	//	{
	//		this.writer = writer;
	//	}

	//	public void AddInputRecord(InputRecord inputRecord)
	//	{
	//		writer.WriteLine(inputRecord.ToString());
	//	}
	//}
}
