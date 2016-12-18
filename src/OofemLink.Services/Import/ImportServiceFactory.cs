using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OofemLink.Services.Import.ESA;
using OofemLink.Common.Enumerations;

namespace OofemLink.Services.Import
{
	public interface IImportServiceFactory
	{
		IImportService Create(ImportSource source, string location, string taskName, bool importModelOnly = false);
	}

	public class ImportServiceFactory : IImportServiceFactory
	{
		readonly ILoggerFactory loggerFactory;

		public ImportServiceFactory(ILoggerFactory loggerFactory)
		{
			this.loggerFactory = loggerFactory;
		}

		public IImportService Create(ImportSource source, string location, string taskName, bool importModelOnly = false)
		{
			switch (source)
			{
				case ImportSource.ESA:
					return new EsaImportService(location, taskName, loggerFactory, importModelOnly);
				default:
					throw new NotSupportedException();
			}
		}
	}
}
