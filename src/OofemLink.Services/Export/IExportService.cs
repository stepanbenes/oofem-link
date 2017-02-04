using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Services.Export
{
	public interface IExportService
	{
		Task ExportSimulationAsync(int simulationId);
	}
}
