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
		[Option(Required = false, HelpText = "Prints all messages to standard output.")]
		public bool Verbose { get; set; }
	}

	[Verb("list", HelpText = "Show list of all projects and simulations in OOFEM database")]
	class ListOptions : CommandLineOptions
	{
	}

	[Verb("create", HelpText = "Create new project in OOFEM database")]
	class CreateOptions : CommandLineOptions
	{
		[Value(index: 0, Required = true, MetaName = "Project name", HelpText = "Nema of project to create")]
		public string ProjectName { get; set; }
	}

	[Verb("import", HelpText = "Import simulation data to OOFEM database")]
	class ImportOptions : CommandLineOptions
	{
		public const string DefaultTaskName = @"$001$064";

		[Value(index: 0, Required = false, MetaName = "Task-name", Default = DefaultTaskName, HelpText = "Name of task to import")]
		public string TaskName { get; set; }

		[Option('s', "source", Required = false, Default = ImportSource.Default, HelpText = "Source of model data to import")]
		public ImportSource Source { get; set; }

		[Option('l', "location", Required = false, HelpText = "Location of input data (current directory is default)")]
		public string Location { get; set; }
	}

	abstract class SimulationOptions : CommandLineOptions
	{
		[Option('s', "simulation", Required = true, HelpText = "Id of simulation")]
		public int SimulationId { get; set; }
	}

	[Verb("export", HelpText = "Build OOFEM input file from model in database")]
	class ExportOptions : SimulationOptions
	{
		[Option('f', "file", Required = false, HelpText = "File absolute or relative path")]
		public string FileName { get; set; }
	}

	[Verb("run", HelpText = "Run simulation in OOFEM")]
	class RunOptions : SimulationOptions
	{
	}
}
