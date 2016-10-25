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

			return Parser.Default.ParseArguments<ImportOptions, BuildOptions, RunOptions>(args)
					.MapResult(
						(ImportOptions options) => runImportCommand(options),
						(BuildOptions options) => runBuildCommand(options),
						(RunOptions options) => runRunCommand(options),
						errors => 1);
		}

		private static int runImportCommand(ImportOptions options)
		{
			var location = options.Location ?? Directory.GetCurrentDirectory();
			if (options.Verbose)
			{
				WriteLine("Location: " + location);
				WriteLine("Format: " + options.Source);
			}
			var importService = ImportServiceFactory.Create(options.Source, location);
			var modelBuilder = new ModelBuilder();
			modelBuilder.ImportModel(importService);
			modelBuilder.ImportMesh(importService);

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
			var inputBuilder = new InputFileBuilder(inputFileFullPath);
			inputBuilder.Build(options.ModelId);
			return 0;
		}

		private static int runRunCommand(RunOptions options)
		{
			throw new NotImplementedException();
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
