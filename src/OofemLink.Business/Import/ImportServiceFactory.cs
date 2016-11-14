using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OofemLink.Business.Import.ESA;
using OofemLink.Common.Enumerations;

namespace OofemLink.Business.Import
{
	public interface IImportServiceFactory
	{
		IImportService Create(ImportSource source, string location);
	}

	public class ImportServiceFactory : IImportServiceFactory
	{
		readonly ILoggerFactory loggerFactory;

		public ImportServiceFactory(ILoggerFactory loggerFactory)
		{
			this.loggerFactory = loggerFactory;
		}

		public IImportService Create(ImportSource source, string location)
		{
			switch (source)
			{
				case ImportSource.ESA:
					return new EsaImportService(location, loggerFactory.CreateLogger<EsaImportService>());
				default:
					throw new NotSupportedException();
			}
		}
	}
}
