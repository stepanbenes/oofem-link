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
	class InputBuilder : IDisposable
	{
		readonly StreamWriter streamWriter;

		public InputBuilder(string fileFullPath)
		{
			var stream = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write, FileShare.None);
			streamWriter = new StreamWriter(stream);
		}

		public void AddComment(string comment)
		{
			Add(new CommentRecord(comment));
		}

		public void AddString(string text)
		{
			Add(new EmptyRecord(text));
		}

		public void Add(InputRecord record)
		{
			// TODO: decide whether to add record to buffer and then write them all at once in Dispose method, or write them immediately
			streamWriter.WriteLine(record.ToString().Trim());
		}

		public void Dispose()
		{
			streamWriter.Dispose();
		}
	}
}
