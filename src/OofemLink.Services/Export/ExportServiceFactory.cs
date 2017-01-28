using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Data;
using OofemLink.Services.DataAccess;

namespace OofemLink.Services.Export
{
	public interface IExportServiceFactory
	{
		IExportService Create(string fileFullPath);
	}

	public class ExportServiceFactory : IExportServiceFactory
	{
		readonly DataContext dataContext;
		readonly ISimulationService simulationService;
		readonly IModelService modelService;

		public ExportServiceFactory(DataContext dataContext, ISimulationService simulationService, IModelService modelService)
		{
			this.dataContext = dataContext;
			this.simulationService = simulationService;
			this.modelService = modelService;
		}

		public IExportService Create(string fileFullPath)
		{
			return new OOFEM.OofemInputFileExportService(dataContext, simulationService, modelService, fileFullPath);
		}
	}
}
