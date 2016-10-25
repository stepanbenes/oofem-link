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

			//var dataContextOptions = Data.DataContext.CreateNewInMemoryContextOptions();

			//using (var db = new Data.DataContext())
			//{
			//	for (int i = 0; i < 1000; i++)
			//	{
			//		db.Projects.Add(new Data.Project { Name = "Test " + i });
			//	}
			//	db.SaveChanges();
			//}

			using (var db = new Data.DataContext())
			{
				WriteLine("Projects:");
				foreach (var project in db.Projects)
				{
					WriteLine(project.Name);
				}
			}

			return 0;
		}
	}
}
