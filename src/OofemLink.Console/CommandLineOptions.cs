using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using OofemLink.Common.Enumerations;

namespace OofemLink.Console
{
    abstract class CommandLineOptions
    {
		[Option(Required = false, HelpText = "Prints all messages to standard output.")]
		public bool Verbose { get; set; }
	}

	[Verb("import", HelpText = "Import model and mesh data to OOFEM database")]
	class ImportOptions : CommandLineOptions
	{
		[Option('s', "source", Required = false, HelpText = "Source of model data to import, options: \"SEN\" for Scia-Engineer (default)")]
		public ImportSource Source { get; set; }

		[Option('l', "location", Required = false, HelpText = "Location of input data (current directory is used if none provided)")]
		public string Location { get; set; }
	}

	[Verb("build", HelpText = "Build OOFEM input file from model in database")]
	class BuildOptions : CommandLineOptions
	{
		[Value(index: 0, Required = true, MetaName = "Model Id", HelpText = "Id of model entity to construct input from")]
		public int ModelId { get; set; }

		[Option('f', "file", Required = false, HelpText = "Input file full path or just name (\"<project-name>.in\" if not provided)")]
		public string InputFileName { get; set; }
	}

	[Verb("run", HelpText = "Run simulation in OOFEM")]
	class RunOptions : CommandLineOptions
	{
		[Value(index: 0, Required = true, MetaName = "Simulation Id", HelpText = "Id of simulation to run")]
		public int SimulationId { get; set; }
	}
}
