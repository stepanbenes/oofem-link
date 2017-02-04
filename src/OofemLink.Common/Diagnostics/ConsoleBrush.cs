using System;

namespace OofemLink.Common.Diagnostics
{
	public struct ConsoleBrush : IDisposable
	{
		readonly ConsoleColor colorToRestore;

		public ConsoleBrush(ConsoleColor color)
		{
			colorToRestore = Console.ForegroundColor;
			Console.ForegroundColor = color;
		}

		public void Dispose()
		{
			Console.ForegroundColor = colorToRestore;
		}
	}
}
