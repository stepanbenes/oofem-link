using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Data;

namespace OofemLink.Business.Export
{
	public interface IExportService
	{
		void ExportSimulation(Simulation simulation);
	}
}
