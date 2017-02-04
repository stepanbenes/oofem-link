using System;
using System.Diagnostics;

namespace OofemLink.Common.Diagnostics
{
    public struct TimeReport : IDisposable
    {
		readonly Stopwatch stopwatch;

		public TimeReport(string taskName)
		{
			stopwatch = Stopwatch.StartNew();
			writeToConsole(taskName, ConsoleColor.White);
		}

		public TimeSpan Elapsed => stopwatch.Elapsed;

		public void Dispose()
		{
			stopwatch.Stop();
			writeLineToConsole(" " + Elapsed.ToString(), ConsoleColor.Gray);
		}

		[Conditional("DEBUG")]
		private void writeToConsole(string message, ConsoleColor color)
		{
			using (new ConsoleBrush(color))
			{
				Console.Write(message);
			}
		}

		[Conditional("DEBUG")]
		private void writeLineToConsole(string message, ConsoleColor color)
		{
			using (new ConsoleBrush(color))
			{
				Console.WriteLine(message);
			}
		}
	}
}
