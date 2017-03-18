using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OofemLink.Data.DataTransferObjects;
using OofemLink.Services.Export;
using OofemLink.Services.Import;
using OofemLink.Services.DataAccess;
using OofemLink.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using OofemLink.Services.Execution;
using OofemLink.Common.Enumerations;
using OofemLink.Common.Diagnostics;

namespace OofemLink.Cli
{
	public class Program
	{
		#region Entry point

		public static int Main(string[] args)
		{
			if (args.Length == 0)
			{
				drawHelloImage();
			}

			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start(); //start measuring execution time
			int returnCode;
			try
			{
				// run program; use blocking wait of asynchronous task
				//returnCode = new Program().RunAsync(args).Result; // this throws AggregateException
				returnCode = new Program().RunAsync(args).GetAwaiter().GetResult(); // this throws the first exception that occurs
			}
			catch (Exception ex)
			{
				using (new ConsoleBrush(ConsoleColor.Red)) // write error message in red
				{
					Console.Error.WriteLine(ex.GetType().FullName);
					Console.Error.WriteLine(ex.Message + Environment.NewLine + Environment.NewLine + ex.StackTrace);
				}
				returnCode = -1;
			}

			stopwatch.Stop();

			if (returnCode != 1) // write footer with success flag and execution time
			{
				if (returnCode == 0)
				{
					using (new ConsoleBrush(ConsoleColor.Green))
						Console.Write("Success. ");
				}
				else
				{
					using (new ConsoleBrush(ConsoleColor.Red))
						Console.Write("Fail. ");
				}
				using (new ConsoleBrush(ConsoleColor.Gray))
					Console.WriteLine($"Execution time: {stopwatch.Elapsed}");
			}

			return returnCode;
		}

		public async Task<int> RunAsync(string[] args)
		{
			using (serviceProvider.CreateScope())
			{
				return await Parser.Default.ParseArguments<ListOptions, CreateOptions, ImportOptions, ExportOptions, RunOptions, DeleteOptions>(args)
					.WithParsed((CommandLineOptions options) => configureApp(options))
					.MapResult(
						(ListOptions options) => runListCommandAsync(options),
						(CreateOptions options) => runCreateCommandAsync(options),
						(ImportOptions options) => runImportCommandAsync(options),
						(ExportOptions options) => runExportCommandAsync(options),
						(RunOptions options) => runRunCommandAsync(options),
						(DeleteOptions options) => runDeleteCommandAsync(options),
						errors => Task.FromResult(1));
			}
		}

		#endregion

		#region Initialization

		readonly IServiceProvider serviceProvider;
		readonly IConfigurationRoot configuration;

		private Program()
		{
			var configurationLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
			var configurationBuilder = new ConfigurationBuilder()
				.SetBasePath(configurationLocation)
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
				//.AddInMemoryCollection()
			this.configuration = configurationBuilder.Build();

			var services = new ServiceCollection();
			configureServices(services);
			this.serviceProvider = services.BuildServiceProvider();
		}

		private void configureServices(IServiceCollection services)
		{
			services.AddLogging();
			services.AddOptions();
			services.Configure<ExecutionOptions>(configuration.GetSection("Execution"));

			services.AddDbContext<DataContext>(options =>
			{
				string oofem_db_connectionString = Environment.GetEnvironmentVariable("oofem_db_connection_string");
				if (!string.IsNullOrEmpty(oofem_db_connectionString))
				{
					options.UseSqlServer(oofem_db_connectionString); // environment variable takes precedence
				}
				else
				{
					switch (configuration["DatabaseProvider"])
					{
						case "SqlServer":
							options.UseSqlServer(configuration.GetConnectionString("oofem_db"));
							break;
						case "Sqlite":
							options.UseSqlite(configuration.GetConnectionString("oofem_db"));
							break;
						case "InMemory":
							options.UseInMemoryDatabase();
							break;
					}
				}
			});

			services.AddScoped<IProjectService, ProjectService>();
			services.AddScoped<ISimulationService, SimulationService>();
			services.AddScoped<IModelService, ModelService>();
			services.AddScoped<IImportServiceFactory, ImportServiceFactory>();
			services.AddScoped<IExportServiceFactory, ExportServiceFactory>();
			services.AddScoped<IExecutionService, OofemExecutionService>();

			Mapper.Initialize(config => config.AddProfile<DtoMappingProfile>());
		}

		private void configureApp(CommandLineOptions options)
		{
			var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
			loggerFactory.AddConsole(minLevel: options.Verbose ? LogLevel.Information : LogLevel.Error);
#if DEBUG
			loggerFactory.AddDebug(minLevel: LogLevel.Trace);
#endif
		}

		#endregion

		#region Commands

		private async Task<int> runListCommandAsync(ListOptions options)
		{
			var projectService = serviceProvider.GetRequiredService<IProjectService>();
			var simulationService = serviceProvider.GetRequiredService<ISimulationService>();
			var projects = await projectService.GetAllAsync();
			using (new ConsoleBrush(ConsoleColor.Yellow))
				Console.WriteLine($"{projects.Count} project{(projects.Count == 1 ? "" : "s")}");
			for (int i = 0; i < projects.Count; i++)
			{
				var simulations = await simulationService.GetAllAsync(q => q.Where(s => s.ProjectId == projects[i].Id));
				printProjectInfo(projects[i], simulations.Count);
				for (int j = 0; j < simulations.Count; j++)
				{
					printSimulationInfo(simulations[j]);
				}
			}
			return 0;
		}

		private async Task<int> runCreateCommandAsync(CreateOptions options)
		{
			var projectService = serviceProvider.GetRequiredService<IProjectService>();
			await projectService.CreateAsync(new ProjectDto { Name = options.ProjectName });
			return 0;
		}

		private Task<int> runImportCommandAsync(ImportOptions options)
		{
			var location = options.Location ?? Directory.GetCurrentDirectory();
			importSimulationFromLocation(options.Source, location, options.TaskName, options.ModelOnly);
			return Task.FromResult(0);
		}

		private async Task<int> runExportCommandAsync(ExportOptions options)
		{
			string fileFullPath;
			if (!string.IsNullOrEmpty(options.FileName))
			{
				// make absolute path
				fileFullPath = Path.IsPathRooted(options.FileName) ? options.FileName : Path.Combine(Directory.GetCurrentDirectory(), options.FileName);
			}
			else
			{
				fileFullPath = Path.Combine(Directory.GetCurrentDirectory(), "oofem.in"); // choose default file name in current directory
			}
			var exportService = serviceProvider.GetRequiredService<IExportServiceFactory>().Create(fileFullPath);
			await exportService.ExportSimulationAsync(options.SimulationId);
			return 0;
		}

		private async Task<int> runRunCommandAsync(RunOptions options)
		{
			int simulationId;
			if (options.SimulationId.HasValue)
			{
				simulationId = options.SimulationId.Value;
			}
			else if (!string.IsNullOrEmpty(options.Location))
			{
				simulationId = importSimulationFromLocation(source: ImportSource.Default, location: options.Location, taskName: options.TaskName, modelOnly: false);
			}
			else
				throw new InvalidOperationException("Either simulationId or import location must be specified.");

			var executionService = serviceProvider.GetRequiredService<IExecutionService>();
			var success = await executionService.ExecuteAsync(simulationId);
			return success ? 0 : 666;
		}

		private async Task<int> runDeleteCommandAsync(DeleteOptions options)
		{
			var projectService = serviceProvider.GetRequiredService<IProjectService>();
			await projectService.DeleteAsync(options.ProjectId);
			return 0;
		}

		private int importSimulationFromLocation(ImportSource source, string location, string taskName, bool modelOnly)
		{
			var projectService = serviceProvider.GetRequiredService<IProjectService>();
			var importService = serviceProvider.GetRequiredService<IImportServiceFactory>().Create(source, location, taskName, modelOnly);
			return projectService.ImportSimulation(importService); // return simulation id
		}

		#endregion

		#region Helper methods

		private static void printProjectInfo(ProjectDto project, int simulationCount)
		{
			using (new ConsoleBrush(ConsoleColor.Magenta))
				Console.Write(project.Name);
			using (new ConsoleBrush(ConsoleColor.White))
				Console.Write($" id: {project.Id}");
			Console.WriteLine();
			using (new ConsoleBrush(ConsoleColor.Yellow))
				Console.WriteLine($"  {simulationCount} simulation{(simulationCount == 1 ? "" : "s")}");
		}

		private static void printSimulationInfo(ViewSimulationDto simulation)
		{
			Console.Write("  ");
			using (new ConsoleBrush(ConsoleColor.White))
				Console.Write("task: ");
			using (new ConsoleBrush(ConsoleColor.Cyan))
				Console.Write(simulation.TaskName);
			using (new ConsoleBrush(ConsoleColor.White))
				Console.Write($" id: {simulation.Id}, state: {simulation.State}, dimensions: {simulation.DimensionFlags}, model-id: {simulation.ModelId}, z-axis-up: {simulation.ZAxisUp}");
			Console.WriteLine();
		}

		private static void drawHelloImage()
		{
			using (new ConsoleBrush(ConsoleColor.Blue))
			{
				Console.WriteLine(
@"
         ,:.             .,,        ,::::::::::`      .:;,                      
      ;;;;;;;;        .;;;;;;;.     ;;;;;;;;;;;     ;;;;;;;;     .;           ;:
    `;;;;;;;;;;      ;;;;;;;;;;.    ;;;;;;;;;;;   :;;;;;;;;;;    ;;;        .;; 
   :;;;;;;;:.;;;    ;;;;;;;;.;;;   :;;;;;;;;;;.  ;;;;;;;;;;;;;   ;;;;      ;;;; 
  ,;;;;;;;,  ;;;   ;;;;;;;;   ;;.  ;;;;;;;;;.;  ;;;;;:  ;;;;;;  ,;;;;;   ;;;;;: 
  ;;;;;;;;`  ;;;  ;;;;;;;;;  ;;;;  ;;;;;;;;    .;;;;;   ;;;;;;  ;;;;;;;.;;;;;;  
 ;;;;;;;;;;;;;;; .;;;;;;;;;;;;;;; ,;;;;;;;     ;;;;;;:`;;;;;;;  ;;;;;;;;;;;;;;  
 ;;;;;;;;;;;;;;; ;;;;;;;;;;;;;;;, ;;;;;;;;;   .;;;;;;;;;;;;;;; ,;;;;;;;;;;;;;:  
.;;;;;;;;;;;;;;; ;;;;;;;;;;;;;;;  ;;;;;;;;;;; ;;;;;;;;,        ;;;;;;;;;;;;;;   
:;;;;;;;;;;;;;;, ;;;;;;;;;;;;;;; ,;;;;;;;;;;. ;;;;;;;;;        ;;;;;;;;;;;;;;   
:;;;;;;;;;;;;;;  ;;;;;;;;;;;;;;  ;;;;;;;   .  ;;;;;;;;;;      :;;;;;;;;;;;;;:   
.;;;;;;;;;;;;;   ;;;;;;;;;;;;;:  ;;;;;;       ;;;;;;;;;;      ;;;;;,;;,;;;;;    
 ;;;;;;;;;;;;`   ;;;;;;;;;;;;:  .;;;;;         ;;;;;;;;;;     ;;;:  ;   ;;;;    
 .;;;;;;;;;;      ;;;;;;;;;;,   ;;;;;;         ;;;;;;;;;;,   :;;;   ;  .;;;:    
  .;;;;;;;,        ;;;;;;;;     ;;;;;;          :;;;;;;;.    ;;;;;:;;:;;;;;     
    `::,             ,::`      .;;;;;;,           .::.       ;;;;;;;;;;;;;;     
"
				);
			}
		}

		#endregion
	}
}
