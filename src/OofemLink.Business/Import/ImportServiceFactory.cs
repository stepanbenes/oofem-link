using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Common.Enumerations;

namespace OofemLink.Business.Import
{
    public class ImportServiceFactory
    {
		public static IImportService Create(ImportSource source, string location)
		{
			switch (source)
			{
				case ImportSource.ESA:
					return new EsaImportService(location);
				default:
					throw new NotSupportedException();
			}
		}
    }
}
