using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using OofemLink.Business;
using OofemLink.Business.Import;
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

			return Parser.Default.ParseArguments<CreateOptions, ImportOptions, BuildOptions, RunOptions>(args)
					.MapResult(
						(CreateOptions options) => runCreateCommand(options),
						(ImportOptions options) => runImportCommand(options),
						(BuildOptions options) => runBuildCommand(options),
						(RunOptions options) => runRunCommand(options),
						errors => 1);
		}

		private static int runCreateCommand(CreateOptions options)
		{
			var projectManager = ProjectManager.GetOrCreateNew(options.ProjectName);
			return 0;
		}

		private static int runImportCommand(ImportOptions options)
		{
			var location = options.Location ?? Directory.GetCurrentDirectory();
			ProjectManager projectManager;
			int projectId;
			if (int.TryParse(options.ProjectNameOrId, out projectId))
				projectManager = new ProjectManager(projectId);
			else
				projectManager = ProjectManager.GetOrCreateNew(options.ProjectNameOrId);
			var importService = ImportServiceFactory.Create(options.Source, location);
			projectManager.ImportSimulation(importService);
			return 0;
		}

		private static int runBuildCommand(BuildOptions options)
		{
			string inputFileFullPath = null;
			if (!string.IsNullOrEmpty(options.InputFileName))
			{
				// make absolute path
				inputFileFullPath = Path.IsPathRooted(options.InputFileName) ? options.InputFileName : Path.Combine(Directory.GetCurrentDirectory(), options.InputFileName);
			}
			var simulationManager = new SimulationManager(options.SimulationId);
			simulationManager.BuildInputFile(inputFileFullPath);
			return 0;
		}

		private static int runRunCommand(RunOptions options)
		{
			var simulationManager = new SimulationManager(options.SimulationId);
			simulationManager.Run();
			return 0;
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
