using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;

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
		public enum ModelFormat
		{
			SEN = 0, // Scia ENgineer
			Default = SEN,
		}

		[Option('f', "format", Required = false, HelpText = "Format of model to import, options: \"SEN\" for Scia-Engineer (default)")]
		public ModelFormat Format { get; set; }

		[Option('l', "location", Required = false, HelpText = "Location of input directory (current directory is used if none provided)")]
		public string Location { get; set; }
	}
}
