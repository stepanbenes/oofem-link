using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OofemLink.Common.Enumerations;
using OofemLink.Data;
using OofemLink.Data.Entities;
using OofemLink.Services.Export.OOFEM;

namespace OofemLink.Services.Execution
{
	public class OofemExecutionService : IExecutionService
	{
		#region Fields, constructor

		readonly DataContext dataContext;
		readonly ExecutionOptions options;
		readonly ILogger logger;

		public OofemExecutionService(DataContext dataContext, IOptions<ExecutionOptions> options, ILoggerFactory loggerFactory)
		{
			this.dataContext = dataContext;
			this.options = options.Value;
			this.logger = loggerFactory.CreateLogger<OofemExecutionService>();
		}

		#endregion

		#region Public methods

		public async Task<bool> ExecuteAsync(int simulationId)
		{
			var simulation = await dataContext.Simulations.FindAsync(simulationId);
			if (simulation == null)
			{
				throw new KeyNotFoundException($"Simulation with id {simulationId} does not exist.");
			}

			if (simulation.State == SimulationState.MeshGenerated)
			{
				await prepareSimulationToRunAsync(simulation);
			}

			if (simulation.State != SimulationState.ReadyToRun)
			{
				throw new InvalidOperationException("Simulation is not ready to run. Current state: " + simulation.State);
			}

			string inputFileFullPath = prepareInputFile(simulationId);
			logger.LogInformation($"Input file generated at '{inputFileFullPath}'");

			logger.LogInformation($"Starting simulation at '{options.OofemExecutableFilePath}'");
			Process process = new Process();
			process.StartInfo = new ProcessStartInfo(options.OofemExecutableFilePath, $"-f {inputFileFullPath}" /* -qo {logFile} -qe {errorFile}"*/)
			{
				UseShellExecute = false,
				RedirectStandardOutput = true
			};
			process.EnableRaisingEvents = true;

			var tsc = new TaskCompletionSource<int>();

			process.Exited += (s, e) =>
			{
				tsc.SetResult(process.ExitCode);
			};

			process.Start(); // RUUUN OOFEM RUUUN!

			int oofemExitCode = await tsc.Task; // Await Exited event

			bool success = oofemExitCode == 0;
			await finishSimulationAsync(simulation, success);
			//logger.LogInformation("Simulation finished " + (success ? "successfully" : "with errors"));
			return success;
		}

		#endregion

		#region Private methods

		private async Task prepareSimulationToRunAsync(Simulation simulation)
		{
			// TODO: call model transformation service etc.

			simulation.State = SimulationState.ReadyToRun;
			await dataContext.SaveChangesAsync();
		}

		private string prepareInputFile(int simulationId)
		{
			string inputFileFullPath = Path.GetTempFileName();
			var oofemExportService = new OofemInputFileExportService(dataContext, inputFileFullPath);
			oofemExportService.ExportSimulation(simulationId); // TODO: make async
			return inputFileFullPath;
		}

		private async Task finishSimulationAsync(Simulation simulation, bool success)
		{
			if (success)
			{
				simulation.State = SimulationState.Finished;
				await dataContext.SaveChangesAsync();
			}
		}

		#endregion
	}
}
