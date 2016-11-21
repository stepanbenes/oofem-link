using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OofemLink.Business.Dto;
using OofemLink.Business.Export;
using OofemLink.Business.Import;
using OofemLink.Business.Services;
using OofemLink.Data;

namespace OofemLink.Cli
{
	public class Program
	{
		public static int Main(string[] args)
		{
			if (args.Length == 0)
			{
				drawHelloImage();
			}

			return new Program().Run(args).Result; // blocking wait
		}

		readonly IServiceProvider serviceProvider;

		private Program()
		{
			Mapper.Initialize(config => config.AddProfile<DtoMappingProfile>());

			var services = new ServiceCollection();
			configureServices(services);
			this.serviceProvider = services.BuildServiceProvider();
		}

		private void configureServices(IServiceCollection services)
		{
			services.AddLogging();
			services.AddDbContext<DataContext>();

			services.AddSingleton<IProjectService, ProjectService>();
			services.AddSingleton<ISimulationService, SimulationService>();
			services.AddSingleton<IImportServiceFactory, ImportServiceFactory>();
			services.AddSingleton<IExportServiceFactory, ExportServiceFactory>();
		}

		private void configure(bool verbose)
		{
			var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
			loggerFactory.AddConsole(minLevel: verbose ? LogLevel.Information : LogLevel.Warning);
			loggerFactory.AddDebug(minLevel: LogLevel.Trace);
		}

		public Task<int> Run(string[] args)
		{
			return Parser.Default.ParseArguments<CreateOptions, ImportOptions, ExportOptions, RunOptions>(args)
				.WithParsed((CommandLineOptions options) => configure(options.Verbose))
				.MapResult(
					(CreateOptions options) => runCreateCommandAsync(options),
					(ImportOptions options) => runImportCommandAsync(options),
					(ExportOptions options) => runExportCommandAsync(options),
					(RunOptions options) => runRunCommandAsync(options),
					errors => Task.FromResult(1));
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
			var projectService = serviceProvider.GetRequiredService<IProjectService>();
			var importService = serviceProvider.GetRequiredService<IImportServiceFactory>().Create(options.Source, location, options.TaskName);
			projectService.ImportSimulation(importService);
			return Task.FromResult(0);
		}

		private Task<int> runExportCommandAsync(ExportOptions options)
		{
			string fileFullPath = null;
			if (!string.IsNullOrEmpty(options.FileName))
			{
				// make absolute path
				fileFullPath = Path.IsPathRooted(options.FileName) ? options.FileName : Path.Combine(Directory.GetCurrentDirectory(), options.FileName);
			}
			var simulationService = serviceProvider.GetRequiredService<ISimulationService>();
			var exportService = serviceProvider.GetRequiredService<IExportServiceFactory>().Create(fileFullPath);
			simulationService.Export(options.SimulationId, exportService);
			return Task.FromResult(0);
		}

		private Task<int> runRunCommandAsync(RunOptions options)
		{
			var simulationService = serviceProvider.GetRequiredService<ISimulationService>();
			simulationService.Run();
			return Task.FromResult(0);
		}

		private static void drawHelloImage()
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
}
