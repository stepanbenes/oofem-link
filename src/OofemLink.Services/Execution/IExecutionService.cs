using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Services.Execution
{
    public interface IExecutionService
    {
		Task<bool> ExecuteAsync(int simulationId);
    }
}
