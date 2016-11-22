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

			return new Program().RunAsync(args).Result; // blocking wait
		}

		public async Task<int> RunAsync(string[] args)
		{
			using (serviceProvider.CreateScope())
			{
				return await Parser.Default.ParseArguments<CreateOptions, ImportOptions, ExportOptions, RunOptions>(args)
					.WithParsed((CommandLineOptions options) => configureApp(options))
					.MapResult(
						(CreateOptions options) => runCreateCommandAsync(options),
						(ImportOptions options) => runImportCommandAsync(options),
						(ExportOptions options) => runExportCommandAsync(options),
						(RunOptions options) => runRunCommandAsync(options),
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
			Console.WriteLine(configurationLocation);
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

			services.AddDbContext<DataContext>(options =>
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
					default:
						options.UseInMemoryDatabase();
						break;
				}
			});

			services.AddScoped<IProjectService, ProjectService>();
			services.AddScoped<ISimulationService, SimulationService>();
			services.AddScoped<IImportServiceFactory, ImportServiceFactory>();
			services.AddScoped<IExportServiceFactory, ExportServiceFactory>();

			Mapper.Initialize(config => config.AddProfile<DtoMappingProfile>());
		}

		private void configureApp(CommandLineOptions options)
		{
			var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
			loggerFactory.AddConsole(minLevel: options.Verbose ? LogLevel.Information : LogLevel.Warning);
#if DEBUG
			loggerFactory.AddDebug(minLevel: LogLevel.Trace);
#endif
		}

		#endregion

		#region Commands

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

		#endregion

		#region Helper methods

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

		#endregion
	}
}
