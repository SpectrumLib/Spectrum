using System;
using Prism.Build;
using Prism.Content;

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

		public void Warn(string msg)
		{
			var old = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine($"WARN: {msg}");
			Console.ForegroundColor = old;
		}

		public void Error(string msg)
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

		protected override void onEngineInfo(string msg) => Info($"Build Engine: {msg}");

		protected override void onEngineWarning(string msg) => Warn($"Build Engine: {msg}");

		protected override void onEngineError(string msg) => Error($"Build Engine: {msg}");

		protected override void onBuildStart(DateTime start, bool rebuild) =>
			Info($"{(rebuild ? "Rebuild" : "Build")} started on {start.ToShortDateString()} at {start.ToLongTimeString()}.");

		protected override void onBuildEnd(bool success, TimeSpan elapsed, bool cancelled)
		{
			if (cancelled)
				Warn($"Build process was cancelled after {elapsed.TotalSeconds:0.000} seconds.");
			else if (!success)
				Error($"Build process failed after {elapsed.TotalSeconds:0.000} seconds.");
			else
				Info($"Build process succeeded ({elapsed.TotalSeconds:0.000} seconds).");
		}

		protected override void onCleanStart(DateTime start) =>
			Info($"Clean started on {start.ToShortDateString()} at {start.ToShortTimeString()}.");

		protected override void onCleanEnd(bool success, TimeSpan elapsed, bool cancelled)
		{
			if (cancelled)
				Warn($"Clean process was cancelled after {elapsed.TotalSeconds:0.000} seconds.");
			else if (!success)
				Error($"Clean process failed after {elapsed.TotalSeconds:0.000} seconds.");
			else
				Info($"Clean process succeeded ({elapsed.TotalSeconds:0.000} seconds).");
		}

		protected override void onItemStarted(ContentItem item, uint id)
		{
			if (Verbose)
				Info($"Started building content item '{item.ItemPath}' at {MessageTime.ToLongTimeString()}.");
		}

		protected override void onItemContinued(ContentItem item, uint id, ContinueStage stage)
		{
			if (Verbose)
				Info($"Content item '{item.ItemPath}' is now in the {stage.ToString().ToLower()} stage.");
		}

		protected override void onItemFinished(ContentItem item, uint id, TimeSpan elapsed) =>
			Info($"Finished building content item '{item.ItemPath}' ({elapsed.TotalSeconds:0.000} seconds).");

		protected override void onItemFailed(ContentItem item, uint idx, string message) =>
			Error($"Failed to build content item '{item.ItemPath}', reason: {message}.");

		protected override void onItemSkipped(ContentItem item) =>
			Info($"Skipped content item '{item.ItemPath}'.");

		protected override void onItemInfo(ContentItem item, uint id, string message)
		{
			if (Verbose)
				Info($"'{item.ItemPath}' {message}");
		}

		protected override void onItemWarn(ContentItem item, uint id, string message) =>
			Warn($"'{item.ItemPath}' {message}");

		protected override void onItemError(ContentItem item, uint id, string message) =>
			Error($"'{item.ItemPath}' {message}");
	}
}
