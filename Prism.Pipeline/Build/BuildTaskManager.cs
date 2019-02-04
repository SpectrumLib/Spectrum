﻿using System;
using System.Diagnostics;
using System.Threading;

namespace Prism
{
	// Manages the distribution of content items to build tasks, as well as maintining the state of the pipeline
	//  and task cancellation
	internal class BuildTaskManager
	{
		#region Fields
		public readonly BuildEngine Engine;

		// Objects for tracking if the build pipeline is running
		private readonly object _busyLock = new object();
		private bool _busy = false;
		public bool Busy
		{
			get { lock (_busyLock) { return _busy; } }
			private set { lock (_busyLock) { _busy = value; } }
		}

		// Objects for tracking if the build pipeline should stop running
		private readonly object _stopLock = new object();
		private bool _shouldStop = false;
		public bool ShouldStop
		{
			get { lock(_stopLock) { return _shouldStop; } }
			private set { lock(_stopLock) { _shouldStop = value; } }
		}

		// The list of available tasks
		private readonly BuildTask[] _tasks;

		// Tracks if the current process is a clean action
		private bool _isCleaning = false;
		#endregion // Fields

		public BuildTaskManager(BuildEngine engine, uint threads)
		{
			Engine = engine;

			_tasks = new BuildTask[threads];
			for (uint i = 0; i < threads; ++i)
				_tasks[i] = new BuildTask(this);
		}

		// Called by BuildTask instances to get the next available content item to start building
		//   Returns false when no items are left
		internal bool GetTaskItem(out ContentItem item)
		{
			item = null;
			return false;
		}

		#region Task Functions
		// The pipeline control function for build tasks that runs on the separate build thread
		public void Build(bool rebuild)
		{
			Busy = true;
			ShouldStop = false;
			_isCleaning = false;
			Stopwatch timer = Stopwatch.StartNew();

			bool success = false;
			try
			{
				Engine.Logger.BuildStart(rebuild);

				// Launch the build tasks
				foreach (var task in _tasks)
					task.Start(rebuild);

				// Wait for the tasks to complete
				foreach (var task in _tasks)
					task.Join();

				success = true;
			}
			finally
			{
				Busy = false;
				Engine.Logger.BuildEnd(success, timer.Elapsed, ShouldStop);
			}
		}

		// The pipeline control function for clean tasks that runs on the separate build thread
		public void Clean()
		{
			Busy = true;
			ShouldStop = false;
			_isCleaning = true;
			Stopwatch timer = Stopwatch.StartNew();

			bool success = false;
			try
			{
				Engine.Logger.CleanStart();

				// For testing
				Thread.Sleep(500);
				success = true;
			}
			finally
			{
				Busy = false;
				Engine.Logger.CleanEnd(success, timer.Elapsed, ShouldStop);
			}
		}

		// Sets the pipeline to stop processing, and then waits for it to finish
		public void Cancel()
		{
			ShouldStop = true;
			if (_isCleaning)
			{
				// Crude way to wait for the clean process to finish
				while (Busy)
					Thread.Sleep(50);
			}
			else
			{
				// TODO: Wait for tasks instead
				while (Busy)
					Thread.Sleep(50);
			}
		}
		#endregion // Task Functions
	}
}