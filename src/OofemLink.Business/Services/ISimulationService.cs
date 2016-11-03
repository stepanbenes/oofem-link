using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Business.Dto;

namespace OofemLink.Business.Services
{
    public interface ISimulationService : IQuery<ViewSimulationDto, int>, ICommand<EditSimulationDto, int>
    {
		void BuildInputFile(int simulationId, string fileFullPath);
		void Run();
	}
}
