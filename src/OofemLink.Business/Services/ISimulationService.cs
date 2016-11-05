using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Business.Dto;
using OofemLink.Business.Export;

namespace OofemLink.Business.Services
{
    public interface ISimulationService : IQuery<ViewSimulationDto, int>, ICommand<EditSimulationDto, int>
    {
		void Export(int simulationId, IExportService exportService);
		void Run();
	}
}
