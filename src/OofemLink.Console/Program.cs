using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CommandLine;
using OofemLink.Business.Dto;
using OofemLink.Business.Export;
using OofemLink.Business.Import;
using OofemLink.Business.Services;
using OofemLink.Data;
using static System.Console;

namespace OofemLink.Console
{
	public class Program
	{
		public static int Main(string[] args)
		{
			if (args.Length == 0)
			{
				drawHelloImage();
			}

			Mapper.Initialize(config => config.AddProfile<DtoMappingProfile>());

			using (var context = new DataContext())
			{
#if DEBUG
				context.Database.EnsureCreated();
#endif
				return Parser.Default.ParseArguments<CreateOptions, ImportOptions, ExportOptions, RunOptions>(args)
					.MapResult(
						(CreateOptions options) => runCreateCommandAsync(options, context),
						(ImportOptions options) => runImportCommandAsync(options, context),
						(ExportOptions options) => runExportCommandAsync(options, context),
						(RunOptions options) => runRunCommandAsync(options, context),
						errors => Task.FromResult(1)).Result; // blocking wait
			}
		}

		private static async Task<int> runCreateCommandAsync(CreateOptions options, DataContext context)
		{
			var projectService = new ProjectService(context);
			await projectService.CreateAsync(new ProjectDto { Name = options.ProjectName });
			return 0;
		}

		private static Task<int> runImportCommandAsync(ImportOptions options, DataContext context)
		{
			var location = options.Location ?? Directory.GetCurrentDirectory();
			var projectService = new ProjectService(context);
			var importService = ImportServiceFactory.Create(options.Source, location);
			projectService.ImportSimulation(options.ProjectNameOrId ?? Path.GetFileName(location), importService);
			return Task.FromResult(0);
		}

		private static Task<int> runExportCommandAsync(ExportOptions options, DataContext context)
		{
			string fileFullPath = null;
			if (!string.IsNullOrEmpty(options.FileName))
			{
				// make absolute path
				fileFullPath = Path.IsPathRooted(options.FileName) ? options.FileName : Path.Combine(Directory.GetCurrentDirectory(), options.FileName);
			}
			var simulationService = new SimulationService(context);
			var exportService = ExportServiceFactory.Create(context, fileFullPath);
			simulationService.Export(options.SimulationId, exportService);
			return Task.FromResult(0);
		}

		private static Task<int> runRunCommandAsync(RunOptions options, DataContext context)
		{
			var simulationService = new SimulationService(context);
			simulationService.Run();
			return Task.FromResult(0);
		}

		private static void drawHelloImage()
		{
			WriteLine(
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
