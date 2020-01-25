/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using Prism.Pipeline;

namespace Prism
{
	// Build logger for command line actions
	internal class ConsoleLogger : BuildLogger
	{
		#region Fields
		private readonly object _lock = new object();

		private bool _isRelease = false;
		#endregion // Fields

		public ConsoleLogger() :
			base()
		{ }


		protected override void onEngineInfo(DateTime ts, string msg, bool important)
		{
			if (Arguments.Verbosity >= 0)
				INFO($"Engine - {msg}");
		}

		protected override void onEngineWarning(DateTime ts, string msg)
		{
			if (Arguments.Verbosity >= 0)
				WARN($"Engine - {msg}");
		}

		protected override void onEngineError(DateTime ts, string msg) =>
			ERROR($"Engine - {msg}");


		protected override void onBuildStart(DateTime ts, bool rebuild, bool release)
		{
			_isRelease = release;
			INFO($"{(rebuild ? "Rebuild" : "Build")} started{((Arguments.Verbosity > 0) ? $" at {ts.ToShortTimeString()}" : "")}.");

			if (Arguments.Verbosity > 1)
			{
				INFO($"Build settings: Mode={(release ? "Release" : "Debug")} Compress={Project.Properties.Compress && release} " +
					 $"PackSize={Project.Properties.PackSize}");
			}
		}

		protected override void onBuildContinue(DateTime ts, TimeSpan itemBuildTime) =>
			INFO($"Items built in {itemBuildTime.TotalSeconds:0.000}s - starting output task.");

		protected override void onBuildEnd(DateTime ts, bool success, TimeSpan elapsed, bool cancelled)
		{
			if (cancelled)
				WARN($"Build cancelled after {elapsed.TotalSeconds:0.000}s.");
			else if (success)
				INFO($"Build completed ({elapsed.TotalSeconds:0.000} s).");
			else
				ERROR($"Build failed after {elapsed.TotalSeconds:0.000}s.");
		}


		protected override void onCleanStart(DateTime ts) =>
			INFO($"Clean started{((Arguments.Verbosity > 0) ? $" at {ts.ToShortTimeString()}" : "")}.");

		protected override void onCleanEnd(DateTime ts, bool success, TimeSpan elapsed, bool cancelled)
		{
			if (cancelled)
				WARN($"Clean cancelled after {elapsed.TotalSeconds:0.000}s.");
			else if (success)
				INFO($"Clean completed ({elapsed.TotalSeconds:0.000} s).");
			else
				ERROR($"Clean failed after {elapsed.TotalSeconds:0.000}s.");
		}


		protected override void onItemStarted(DateTime ts, ContentItem item, uint id)
		{
			if (Arguments.Verbosity > 0)
				INFO($"Started: {item.ItemPath}{((Arguments.Verbosity > 1) ? $" at {ts.ToShortTimeString()}" : "")}.");
		}

		protected override void onItemFinished(DateTime ts, ContentItem item, uint id, TimeSpan elapsed)
		{
			if (Arguments.Verbosity >= 0)
				INFO($"Complete: {item.ItemPath}{((Arguments.Verbosity > 0) ? $" ({elapsed.TotalSeconds:0.000} s)" : "")}.");
		}

		protected override void onItemFailed(DateTime ts, ContentItem item, uint idx, string message) =>
			ERROR($"Failed: {item.ItemPath} - {message}");

		protected override void onItemPack(DateTime ts, ContentItem item, uint packNum)
		{
			if (Arguments.Verbosity > 0 && _isRelease)
				INFO($"Packed: {item.ItemPath}{((Arguments.Verbosity > 1) ? $" in pack {packNum}" : "")}.");
		}

		protected override void onItemSkipped(DateTime ts, ContentItem item, uint idx)
		{
			if (Arguments.Verbosity >= 0)
				INFO($"Skipped: {item.ItemPath}.");
		}


		protected override void onItemInfo(DateTime ts, ContentItem item, uint id, string message, bool important)
		{
			if (Arguments.Verbosity > 0 || (important && Arguments.Verbosity >= 0))
				INFO($"'{item.ItemPath}' - {message}.");
		}

		protected override void onItemWarn(DateTime ts, ContentItem item, uint id, string message)
		{
			if (Arguments.Verbosity >= 0)
				WARN($"'{item.ItemPath}' - {message}.");
		}

		protected override void onItemError(DateTime ts, ContentItem item, uint id, string message) =>
			ERROR($"'{item.ItemPath}' - {message}.");


		#region Locked Console
		private void INFO(string msg)
		{
			lock (_lock)
			{
				CConsole.Info(msg);
			}
		}

		private void WARN(string msg)
		{
			lock (_lock)
			{
				CConsole.Warn(msg);
			}
		}

		private void ERROR(string msg)
		{
			lock (_lock)
			{
				CConsole.Error(msg);
			}
		}
		#endregion // Locked Console
	}
}
