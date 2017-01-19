using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using OofemLink.Common.Enumerations;

namespace OofemLink.Cli
{
	abstract class CommandLineOptions
	{
		[Option(Required = false, HelpText = "Show detailed information for debugging")]
		public bool Verbose { get; set; }
	}

	[Verb("list", HelpText = "Show list of all projects and simulations in OOFEM database")]
	class ListOptions : CommandLineOptions
	{
	}

	[Verb("create", HelpText = "Create new project in OOFEM database")]
	class CreateOptions : CommandLineOptions
	{
		[Value(index: 0, Required = true, MetaName = "Project name", HelpText = "Name of project to create")]
		public string ProjectName { get; set; }
	}

	[Verb("import", HelpText = "Import simulation data to OOFEM database")]
	class ImportOptions : CommandLineOptions
	{
		public const string DefaultTaskName = @"$001$064";

		[Option('l', "location", Required = true, HelpText = "Location of input data")]
		public string Location { get; set; }

		[Option("task-name", Required = false, Default = DefaultTaskName, HelpText = "Name of task to import")]
		public string TaskName { get; set; }

		[Option('s', "source", Required = false, Default = ImportSource.Default, HelpText = "Source of model data to import")]
		public ImportSource Source { get; set; }

		[Option("modelOnly", Required = false, HelpText = "Import model only (exclude attributes)")]
		public bool ModelOnly { get; set; }
	}

	[Verb("export", HelpText = "Build OOFEM input file from model in database")]
	class ExportOptions : CommandLineOptions
	{
		[Option('s', "simulation", Required = true, HelpText = "Id of simulation to export")]
		public int SimulationId { get; set; }

		[Option('f', "file", Required = false, HelpText = "File absolute or relative path")]
		public string FileName { get; set; }
	}

	[Verb("run", HelpText = "Run simulation in OOFEM")]
	class RunOptions : CommandLineOptions
	{
		[Option('s', "simulation", Required = false, HelpText = "Id of simulation to run")]
		public int? SimulationId { get; set; }

		[Option('i', "import", Required = false, HelpText = "Location of input data to import")]
		public string Location { get; set; }

		[Option("task-name", Required = false, Default = ImportOptions.DefaultTaskName, HelpText = "Name of task to import")]
		public string TaskName { get; set; }
	}

	abstract class ProjectOptions : CommandLineOptions
	{
		[Option('p', "project", Required = true, HelpText = "Id of project")]
		public int ProjectId { get; set; }
	}

	[Verb("delete", HelpText = "Delete project from OOFEM database")]
	class DeleteOptions : ProjectOptions
	{
	}
}
