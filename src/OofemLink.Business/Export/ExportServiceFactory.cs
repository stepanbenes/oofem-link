using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Business.Export
{
    public static class ExportServiceFactory
    {
		public static IExportService Create(string fileFullPath)
		{
			return new OofemInputFileExportService(fileFullPath);
		}
	}
}
