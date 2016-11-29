using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OofemLink.Services.Import.ESA
{
    abstract class EsaFileParserBase : FormatParserBase
    {
		protected readonly string location, taskName;

		protected EsaFileParserBase(string location, string taskName, ILoggerFactory loggerFactory)
		{
			this.location = location;
			this.taskName = taskName;
			Logger = loggerFactory.CreateLogger(GetType());
		}

		public abstract string Extension { get; }
		protected ILogger Logger { get; }
		protected string FileFullPath => Path.Combine(location, $"{taskName}.{Extension}");

		protected bool CheckExistence()
		{
			bool exists = File.Exists(FileFullPath);
			if (!exists)
			{
				Logger.LogWarning($"File '{FileFullPath}' does not exist.");
			}
			return exists;
		}

		protected void LogStart() => Logger.LogTrace($"Parsing {Extension} file '{FileFullPath}'");
	}
}
