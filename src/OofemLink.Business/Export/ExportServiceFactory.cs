using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Data;

namespace OofemLink.Business.Export
{
    public static class ExportServiceFactory
    {
		public static IExportService Create(DataContext context, string fileFullPath)
		{
			return new OofemInputFileExportService(context, fileFullPath);
		}
	}
}
