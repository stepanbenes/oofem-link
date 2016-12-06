using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Data.DataTransferObjects;
using OofemLink.Services.Export;

namespace OofemLink.Services.DataAccess
{
    public interface ISimulationService : IQuery<ViewSimulationDto, int>, ICommand<EditSimulationDto, int>
    {
	}
}
