using System;
using Prism.Build;

namespace Prism
{
	// Message logger for command line actions
	internal class CommandLineLogger : BuildLogger
	{
		#region Fields
		public readonly bool Verbose;
		#endregion // Fields

		public CommandLineLogger(bool verbose) :
			base()
		{
			Verbose = verbose;
		}

		public void Info(string msg) => Console.WriteLine($"INFO: {msg}");

		public new void Warn(string msg)
		{
			var old = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine($"WARN: {msg}");
			Console.ForegroundColor = old;
		}

		public new void Error(string msg)
		{
			var old = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine($"ERROR: {msg}");
			Console.ForegroundColor = old;
		}

		public void Fatal(string msg)
		{
			var old = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.WriteLine($"FATAL: {msg}");
			Console.ForegroundColor = old;
		}

		protected override void onWarning(string msg) => Warn(msg);

		protected override void onError(string msg) => Error(msg);

		protected override void onBuildStart(DateTime start, bool rebuild) =>
			Info($"{(rebuild ? "Rebuild" : "Build")} started on {start.ToShortDateString()} at {start.ToLongTimeString()}.");

		protected override void onBuildEnd(bool success, TimeSpan elapsed, bool cancelled)
		{
			if (cancelled)
				Warn($"Build process was cancelled after {elapsed.TotalSeconds} seconds.");
			else if (!success)
				Error($"Build process failed after {elapsed.TotalSeconds} seconds.");
			else
				Info($"Build process succeeded ({elapsed.TotalSeconds} seconds).");
		}

		protected override void onCleanStart(DateTime start) =>
			Info($"Clean started on {start.ToShortDateString()} at {start.ToShortTimeString()}.");

		protected override void onCleanEnd(bool success, TimeSpan elapsed, bool cancelled)
		{
			if (cancelled)
				Warn($"Clean process was cancelled after {elapsed.TotalSeconds} seconds.");
			else if (!success)
				Error($"Clean process failed after {elapsed.TotalSeconds} seconds.");
			else
				Info($"Clean process succeeded ({elapsed.TotalSeconds} seconds).");
		}
	}
}
