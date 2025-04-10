﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OofemLink.Common.Enumerations;
using OofemLink.Data;
using OofemLink.Data.DbEntities;
using OofemLink.Services.DataAccess;
using OofemLink.Services.Export.OOFEM;

namespace OofemLink.Services.Execution
{
	public class OofemExecutionService : IExecutionService
	{
		#region Fields, constructor

		readonly ISimulationService simulationService;
		readonly IModelService modelService;
		readonly ExecutionOptions options;
		readonly ILogger logger;

		public OofemExecutionService(ISimulationService simulationService, IModelService modelService, IOptions<ExecutionOptions> options, ILoggerFactory loggerFactory)
		{
			this.simulationService = simulationService;
			this.modelService = modelService;
			this.options = options.Value;
			this.logger = loggerFactory.CreateLogger<OofemExecutionService>();
		}

		#endregion

		#region Public methods

		public async Task<bool> ExecuteAsync(int simulationId)
		{
			var simulation = await simulationService.GetOneAsync(simulationId);
			if (simulation == null)
			{
				throw new KeyNotFoundException($"Simulation with id {simulationId} does not exist.");
			}

			if (simulation.State < SimulationState.ModelReady)
			{
				throw new InvalidOperationException("Simulation is not ready to run. Current state: " + simulation.State);
			}

			string inputFileFullPath = await prepareInputFileAsync(simulationId);
			logger.LogInformation($"Input file generated at '{inputFileFullPath}'");

			logger.LogInformation($"Starting simulation at '{options.OofemExecutableFilePath}'");
			Process process = new Process();
			process.StartInfo = new ProcessStartInfo(options.OofemExecutableFilePath, $"-f {inputFileFullPath}" /* -qo {logFile} -qe {errorFile}"*/)
			{
				UseShellExecute = false
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

			if (success)
			{
				await simulationService.ChangeSimulationState(simulationId, SimulationState.Finished);
			}

			//logger.LogInformation("Simulation finished " + (success ? "successfully" : "with errors"));
			return success;
		}

		#endregion

		#region Private methods

		private async Task<string> prepareInputFileAsync(int simulationId)
		{
			string inputFileFullPath;
			string outputFileDirectory;
			if (!string.IsNullOrEmpty(options.DefaultInputLocation))
				inputFileFullPath = Path.Combine(options.DefaultInputLocation, "oofem.in");
			else
				inputFileFullPath = Path.GetTempFileName();
			if (!string.IsNullOrEmpty(options.DefaultOutputLocation))
				outputFileDirectory = options.DefaultOutputLocation;
			else
				outputFileDirectory = Path.GetTempPath();
			var oofemExportService = new OofemInputFileExportService(simulationService, modelService, inputFileFullPath, outputFileDirectory);
			await oofemExportService.ExportSimulationAsync(simulationId);
			return inputFileFullPath;
		}

		#endregion
	}
}
