using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Data;

namespace OofemLink.Services.Export
{
	public interface IExportServiceFactory
	{
		IExportService Create(string fileFullPath);
	}

	public class ExportServiceFactory : IExportServiceFactory
	{
		DataContext dataContext;

		public ExportServiceFactory(DataContext dataContext)
		{
			this.dataContext = dataContext;
		}

		public IExportService Create(string fileFullPath)
		{
			return new OOFEM.OofemInputFileExportService(dataContext, fileFullPath);
		}
	}
}
