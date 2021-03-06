﻿using System;
using Prism.Build;
using Prism.Content;
using static Prism.CConsole;

namespace Prism
{
	// Message logger for command line actions
	internal class CommandLineLogger : BuildLogger
	{
		#region Fields
		public readonly bool Verbose;
		public readonly bool UseStats;

		private bool _isReleaseBuild;
		#endregion // Fields

		public CommandLineLogger(bool verbose, bool stats) :
			base()
		{
			Verbose = verbose;
			UseStats = stats;
		}

		protected override void onEngineInfo(string msg, bool important) => Info($"Build Engine: {msg}");

		protected override void onEngineWarning(string msg) => Warn($"Build Engine: {msg}");

		protected override void onEngineError(string msg) => Error($"Build Engine: {msg}");

		protected override void onBuildStart(DateTime start, bool rebuild, bool release)
		{
			Info($"{(rebuild ? "Rebuild" : "Build")} started on {start.ToShortDateString()} at {start.ToLongTimeString()}.");
			Info($"Build Settings: Mode={(release ? "Release" : "Debug")} Compress={release && Project.Properties.Compress} " +
				$"PackSize={Project.Properties.PackSize / ProjectProperties.SIZE_TO_BYTES}");
			_isReleaseBuild = release;
		}

		protected override void onBuildContinue(TimeSpan itemBuildTime) =>
			Info($"Items built in {itemBuildTime.TotalSeconds:0.000} seconds, starting content output process.");

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

		protected override void onItemSkipped(ContentItem item, uint idx) =>
			Info($"Skipped content item '{item.ItemPath}'.");

		protected override void onItemInfo(ContentItem item, uint id, string message, bool important)
		{
			if (Verbose || important)
				Info($"'{item.ItemPath}' {message}");
		}

		protected override void onItemWarn(ContentItem item, uint id, string message) =>
			Warn($"'{item.ItemPath}' {message}");

		protected override void onItemError(ContentItem item, uint id, string message) =>
			Error($"'{item.ItemPath}' {message}");

		protected override void onItemStats(ContentItem item, uint id, string message)
		{
			if (UseStats)
				Stats($"'{item.ItemPath}' {message}");
		}

		protected override void onItemPack(ContentItem item, uint packNum)
		{
			if (!_isReleaseBuild)
				Info($"Content item '{item.ItemPath}' processed to output.");
			else if (Verbose)
				Info($"Content item '{item.ItemPath}' packed in content pack {packNum}.");
		}
	}
}
