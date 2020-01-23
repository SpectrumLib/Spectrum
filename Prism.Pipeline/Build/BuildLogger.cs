/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Diagnostics;

namespace Prism.Pipeline
{
	// Base class for build engine process loggers
	internal abstract class BuildLogger
	{
		#region Fields
		public BuildEngine Engine { get; internal set; }
		public ContentProject Project => Engine.Project;

		private Stopwatch _timer;
		private DateTime _startTime;
		#endregion // Fields

		protected BuildLogger()
		{
			_timer = new Stopwatch();
			_startTime = default;
		}

		#region Internal Logging
		internal void EngineInfo(string msg, bool important = false) =>
			onEngineInfo(DateTime.Now, msg, important);

		internal void EngineWarn(string msg) =>
			onEngineWarning(DateTime.Now, msg);

		internal void EngineError(string msg) =>
			onEngineError(DateTime.Now, msg);

		internal void BuildStart(bool rebuild, bool release)
		{
			_startTime = DateTime.Now;
			_timer.Restart();
			onBuildStart(_startTime, rebuild, release);
		}

		internal void BuildContinue(TimeSpan itemBuildTime) =>
			onBuildContinue(DateTime.Now, itemBuildTime);

		internal void BuildEnd(bool success, TimeSpan elapsed, bool cancelled) =>
			onBuildEnd(DateTime.Now, success, elapsed, cancelled);

		internal void CleanStart()
		{
			_startTime = DateTime.Now;
			_timer.Restart();
			onCleanStart(_startTime);
		}

		internal void CleanEnd(bool success, TimeSpan elapsed, bool cancelled) =>
			onCleanEnd(DateTime.Now, success, elapsed, cancelled);

		internal void ItemStart(ContentItem item, uint index) =>
			onItemStarted(DateTime.Now, item, index);

		internal void ItemFinished(ContentItem item, uint index, TimeSpan elapsed) =>
			onItemFinished(DateTime.Now, item, index, elapsed);

		internal void ItemFailed(ContentItem item, uint index, string message) =>
			onItemFailed(DateTime.Now, item, index, message);

		internal void ItemSkipped(ContentItem item, uint index) =>
			onItemSkipped(DateTime.Now, item, index);

		internal void ItemInfo(ContentItem item, uint index, string message, bool important = false) =>
			onItemInfo(DateTime.Now, item, index, message, important);

		internal void ItemWarn(ContentItem item, uint index, string message) =>
			onItemWarn(DateTime.Now, item, index, message);

		internal void ItemError(ContentItem item, uint index, string message) =>
			onItemError(DateTime.Now, item, index, message);

		internal void ItemPack(ContentItem item, uint packNum) =>
			onItemPack(DateTime.Now, item, packNum);
		#endregion // Internal Logging

		#region Message Handlers
		// Called when the build engine produces a general info message
		protected abstract void onEngineInfo(DateTime ts, string msg, bool important);
		// Called when the build engine produces a general warning message
		protected abstract void onEngineWarning(DateTime ts, string msg);
		// Called when the build engine produces a general error message
		protected abstract void onEngineError(DateTime ts, string msg);

		// Called when a new build or rebuild action starts
		protected abstract void onBuildStart(DateTime ts, bool rebuild, bool release);
		// Called when the build process moves from building content items to preparing them for output
		protected abstract void onBuildContinue(DateTime ts, TimeSpan itemBuildTime);
		// Called when a build or rebuild action ends, either through success, early failure, or if it was cancelled
		protected abstract void onBuildEnd(DateTime ts, bool success, TimeSpan elapsed, bool cancelled);

		// Called when a new clean action starts
		protected abstract void onCleanStart(DateTime ts);
		// Called when a clean action ends
		protected abstract void onCleanEnd(DateTime ts, bool success, TimeSpan elapsed, bool cancelled);

		// Called when processing on a content item starts
		protected abstract void onItemStarted(DateTime ts, ContentItem item, uint id);
		// Called when a content item is finished processing
		protected abstract void onItemFinished(DateTime ts, ContentItem item, uint id, TimeSpan elapsed);
		// Called when the build process for a content item fails
		protected abstract void onItemFailed(DateTime ts, ContentItem item, uint idx, string message);
		// Called when a content item is skipped by the build engine
		protected abstract void onItemSkipped(DateTime ts, ContentItem item, uint idx);

		// Called from a pipeline stage to relay normal-level information about a content item build process
		protected abstract void onItemInfo(DateTime ts, ContentItem item, uint id, string message, bool important);
		// Called from a pipeline stage to relay warning-level information about a content item build process
		protected abstract void onItemWarn(DateTime ts, ContentItem item, uint id, string message);
		// Called from a pipeline stage to relay error-level information about a content item build process
		protected abstract void onItemError(DateTime ts, ContentItem item, uint id, string message);

		// Called from the packing process when an item is moved to the output
		protected abstract void onItemPack(DateTime ts, ContentItem item, uint packNum);
		#endregion // Message Handlers
	}
}
