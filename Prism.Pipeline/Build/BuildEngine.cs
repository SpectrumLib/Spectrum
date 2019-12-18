/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
		// If the current task is a release build task
		public bool IsRelease { get; private set; } = false;
		// If output compression is used
		public bool Compress => Project.Properties.Compress && IsRelease;
		#endregion // Fields

		public BuildEngine(ContentProject proj, BuildLogger logger, uint threads)
		{
			Project = proj ?? throw new ArgumentNullException(nameof(proj));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			Logger.Engine = this;

			_tasks = new BuildTask[threads];
			for (uint i = 0; i < threads; ++i)
				_tasks[i] = new BuildTask(this);

			PathUtils.TryMakeAbsolutePath("./__tmp__/", proj.Paths.Cache.FullName, out TempFileDirectory);
		}
		~BuildEngine()
		{
			dispose(false);
		}

		// Gets a unique temp file path in the build cache directory
		public string ReserveTempFile() => Path.Combine(TempFileDirectory, $"{Interlocked.Increment(ref _tempFileIndex)}.tmp");

		#region Actions
		public Task Build(bool rebuild, bool release)
		{
			if (Busy)
				throw new InvalidOperationException("Cannot start a build task on a running engine");

			return new Task(() => doBuild(rebuild, release));
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
		private void doBuild(bool rebuild, bool release)
		{
			Busy = true;
			ShouldStop = false;
			IsRelease = release;
			IsCleaning = false;

			Stopwatch timer = Stopwatch.StartNew();
			bool success = false;
			try
			{
				Logger.BuildStart(rebuild, release);
				success = true;
			}
			finally
			{
				Logger.BuildEnd(success, timer.Elapsed, ShouldStop);
				Busy = false;
				IsRelease = false;
			}
		}

		private void doClean()
		{
			Busy = true;
			ShouldStop = false;
			IsRelease = false;
			IsCleaning = true;

			Stopwatch timer = Stopwatch.StartNew();
			bool success = false;
			try
			{
				Logger.CleanStart();
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
}
