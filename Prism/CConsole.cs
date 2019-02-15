using System;

namespace Prism
{
	// Simple utility class for printing colored messages to the terminal with a severity tag
	internal static class CConsole
	{
		private static readonly ConsoleColor DefaultFGColor;
		private static readonly ConsoleColor DefaultBGColor;

		static CConsole()
		{
			DefaultFGColor = Console.ForegroundColor;
			DefaultBGColor = Console.BackgroundColor;
		}

		public static void Info(string msg) => Console.WriteLine($"INFO: {msg}");

		public static void Warn(string msg)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine($"WARN: {msg}");
			Console.ForegroundColor = DefaultFGColor;
		}

		public static void Error(string msg)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine($"ERROR: {msg}");
			Console.ForegroundColor = DefaultFGColor;
		}

		public static void Fatal(string msg)
		{
			Console.BackgroundColor = ConsoleColor.Magenta;
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine($"FATAL: {msg}");
			Console.BackgroundColor = DefaultBGColor;
			Console.ForegroundColor = DefaultFGColor;
		}
	}
}
