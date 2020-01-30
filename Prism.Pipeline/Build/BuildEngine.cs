﻿/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Prism.Pipeline
{
	// Encapsulates the entire build process for a content project
	internal class BuildEngine : IDisposable
	{
		#region Fields
		public readonly ContentProject Project;
		public readonly BuildLogger Logger;

		public readonly ProcessorTypeCache ProcessorTypes;

		#region Task Management
		// If the engine is currently running tasks
		public bool Busy { get; private set; } = false;
		// If the processing should stop
		public bool ShouldStop { get; private set; } = false;

		// Manages allocation of items to build tasks
		private readonly object _taskLock = new object();
		private uint _itemIndex = 0;
		private IEnumerator<ContentItem> _itemsToBuild;

		// Task objects
		private readonly BuildTask[] _tasks;
		public uint ThreadCount => (uint)_tasks.Length;

		// Temporary file objects
		public readonly string TempFileDirectory;
		private long _tempFileIndex = 0;
		#endregion // Task Management

		// If the current task is a cleaning task
		public bool IsCleaning { get; private set; } = false;
		// The current build settings
		public BuildSettings Settings { get; private set; } = default;
		// If output compression is used
		public bool Compress => Project.Properties.Compress && Settings.Release;
		#endregion // Fields

		public BuildEngine(ContentProject proj, BuildLogger logger, uint threads)
		{
			Project = proj ?? throw new ArgumentNullException(nameof(proj));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			Logger.Engine = this;

			ProcessorTypes = new ProcessorTypeCache(this);

			_tasks = new BuildTask[threads];
			for (uint i = 0; i < threads; ++i)
				_tasks[i] = new BuildTask(this);

			PathUtils.TryMakeAbsolutePath("./.__tmp__/", proj.Paths.Cache.FullName, out TempFileDirectory);
		}
		~BuildEngine()
		{
			dispose(false);
		}

		// Gets a unique temp file path in the build cache directory
		public string ReserveTempFile() => Path.Combine(TempFileDirectory, $"{Interlocked.Increment(ref _tempFileIndex)}.tmp");

		// Called by BuildTask instances to get another item to build
		public bool GetNextOrder(out BuildOrder order)
		{
			lock (_taskLock)
			{
				if (_itemsToBuild.MoveNext())
				{
					var ci = _itemsToBuild.Current;
					order = new BuildOrder(ci, _itemIndex++);
					return true;
				}
				else
				{
					order = null;
					return false;
				}
			}
		}

		#region Actions
		public Task Build(BuildSettings settings)
		{
			if (Busy)
				throw new InvalidOperationException("Cannot start a build task on a running engine");

			return new Task(() => doBuild(settings));
		}

		public Task Clean()
		{
			if (Busy)
				throw new InvalidOperationException("Cannot start a clean task on a running engine");

			return new Task(() => doClean());
		}

		public Task Cancel()
		{
			if (!Busy)
				return Task.CompletedTask;

			return new Task(() => doCancel());
		}
		#endregion // Actions

		#region Task Impl
		private void doBuild(BuildSettings settings)
		{
			Busy = true;
			ShouldStop = false;
			Settings = settings;
			IsCleaning = false;

			Stopwatch timer = Stopwatch.StartNew();
			bool success = false;
			try
			{
				Logger.BuildStart(settings.Rebuild, settings.Release);

				// Reset the item enumerator
				_itemIndex = 0;
				_itemsToBuild = Project.Items.GetEnumerator();

				// Load the content processors
				if (!ProcessorTypes.LoadProcessors(typeof(BuildEngine).Assembly))
					return;

				// Ensure the directories
				if (!Project.EnsurePaths())
				{
					Logger.EngineError("Unable to create build cache and/or output directories.");
					return;
				}
				if (!PathUtils.CreateDirectorySafe(TempFileDirectory, out _))
				{
					Logger.EngineError("Unable to create build temporary directory.");
					return;
				}

				// Exit check
				if (ShouldStop)
					return;

				// Start, then join, the build tasks
				foreach (var task in _tasks)
					task.Start();
				foreach (var task in _tasks)
					task.Join();

				// Exit check
				if (ShouldStop)
					return;

				// Clean up, most likely lots of temp items by now
				GC.Collect();

				// Collate results, and check for failure, or early exit
				var resCount = _tasks.SelectMany(task => task.Results.Select(res => (fail: !res.Success, skip: res.Skipped)))
									 .Aggregate((fail: 0u, skip: 0u), (acc, res) =>
										(acc.Item1 + (res.fail ? 1u : 0u), acc.Item2 + (res.skip ? 1u : 0u)));
				if (resCount.fail > 0)
				{
					Logger.EngineError("One or more items failed to build, skipping output step.");
					return;
				}
				if (resCount.skip == Project.Items.Count)
				{
					if (!OutputTask.NeedsRebuild(this))
					{
						Logger.EngineInfo($"Skipping content output - all items are up to date.", true);
						success = true;
						return;
					}
				}

				// Continue to the output stage, prepare the output task
				Logger.BuildContinue(timer.Elapsed);
				var outTask = new OutputTask(this, _tasks);

				// Move the content files into output
				if (!outTask.GenerateOutputFiles())
					return;

				// Final exit check
				if (ShouldStop)
					return;

				// Output the cpak file
				if (!outTask.GenerateContentPack())
					return;

				success = true;
			}
			finally
			{
				Logger.BuildEnd(success, timer.Elapsed, ShouldStop);
				Busy = false;
				Settings = default;
			}
		}

		private void doClean()
		{
			Busy = true;
			ShouldStop = false;
			IsCleaning = true;

			Stopwatch timer = Stopwatch.StartNew();
			bool success = false;
			try
			{
				Logger.CleanStart();

				Project.Paths.Cache.Refresh();
				Project.Paths.Output.Refresh();

				if (Project.Paths.Cache.Exists)
				{
					foreach (var file in Project.Paths.Cache.EnumerateFiles("*.bin", SearchOption.TopDirectoryOnly))
						file.Delete();
					if (ShouldStop)
						return;
					foreach (var file in Project.Paths.Cache.EnumerateFiles("*.cache", SearchOption.TopDirectoryOnly))
						file.Delete();
					if (ShouldStop)
						return;
					foreach (var file in Project.Paths.Cache.EnumerateFiles(".__tmp__/*", SearchOption.TopDirectoryOnly))
						file.Delete();
					if (ShouldStop)
						return;
				}

				if (Project.Paths.Output.Exists)
				{
					foreach (var file in Project.Paths.Output.EnumerateFiles("*.cpak", SearchOption.TopDirectoryOnly))
						file.Delete();
					if (ShouldStop)
						return;
					foreach (var file in Project.Paths.Output.EnumerateFiles("*.cbin", SearchOption.TopDirectoryOnly))
						file.Delete();
				}

				success = true;
			}
			finally
			{
				Logger.CleanEnd(success, timer.Elapsed, ShouldStop);
				Busy = false;
				IsCleaning = false;
			}
		}

		private void doCancel()
		{
			if (!Busy)
				return;

			ShouldStop = true;
			if (IsCleaning)
			{
				while (Busy)
					Thread.Sleep(50); // Crude way to wait for cleaning to cancel
			}
			else
			{
				foreach (var task in _tasks)
					task.Join();
			}
		}
		#endregion // Task Impl

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			Cancel().Wait();
		}
		#endregion // IDisposable
	}

	// Settings to control build engine build tasks
	internal struct BuildSettings
	{
		public bool Rebuild;
		public bool Release;
		public bool HighCompression;
	}
}
