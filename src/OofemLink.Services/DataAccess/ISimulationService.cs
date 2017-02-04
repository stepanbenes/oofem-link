using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Common.Enumerations;
using OofemLink.Data.DataTransferObjects;
using OofemLink.Services.Export;

namespace OofemLink.Services.DataAccess
{
    public interface ISimulationService : IQuery<ViewSimulationDto, int>, ICommand<EditSimulationDto, int>
    {
		Task ChangeSimulationState(int simulationId, SimulationState newState);
	}
}
