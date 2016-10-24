using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using static System.Console;

namespace OofemLink.Console
{
    public class Program
    {
        public static int Main(string[] args)
        {
			return Parser.Default.ParseArguments<ImportOptions>(args)
					.MapResult(
						(ImportOptions options) => runImportCommand(options),
						errors => 1);
		}

		private static int runImportCommand(ImportOptions options)
		{
			if (options.Verbose)
			{
				var location = options.Location ?? Directory.GetCurrentDirectory();
				WriteLine("Location: " + location);
				WriteLine("Format: " + options.Format);
			}
			return 0;
		}
	}
}
