﻿using System;
using System.Diagnostics;
using System.Threading;
using Prism.Content;

namespace Prism.Build
{
	// Represents a single thread in a build process, which can operate on a single content item at a time
	//   A task is responsible for guiding a single content item through the build pipeline stages
	internal class BuildTask
	{
		#region Fields
		public readonly BuildTaskManager Manager;
		public BuildEngine Engine => Manager.Engine;
		public BuildLogger Logger => Manager.Engine.Logger;
		public ContentProject Project => Manager.Engine.Project;

		// Objects for tracking run state
		private readonly object _runLock = new object();
		private bool _running = false;
		public bool Running
		{
			get { lock (_runLock) { return _running; } }
			private set { lock (_runLock) { _running = value; } }
		}

		// The thread instance
		private Thread _thread = null;
		#endregion // Fields

		public BuildTask(BuildTaskManager manager)
		{
			Manager = manager;
		}

		// Starts the thread and begins processing the content items
		public void Start(bool rebuild)
		{
			if (Running)
				throw new InvalidOperationException("Cannot start a build task while it is already running");

			_thread = new Thread(() => {
				Running = true;
				_thread_func(rebuild);
				_thread = null;
				Running = false;
			});
			_thread.Start();
		}
		
		// Waits until this task has exited
		public void Join() => _thread?.Join();

		// The function that runs on the thread
		private void _thread_func(bool rebuild)
		{
			Stopwatch _timer = new Stopwatch();

			// Iterate over the tasks
			while (!Manager.ShouldStop && Manager.GetTaskItem(out ContentItem currentItem, out uint currentIdx))
			{
				// TODO: Check that the source file exists
				// TODO: Check for skipping

				_timer.Restart();

				// Report start
				Engine.Logger.ItemStart(currentItem, currentIdx);

				// Testing only
				Thread.Sleep(500);

				// Report end
				Engine.Logger.ItemFinished(currentItem, currentIdx, _timer.Elapsed);
			}
		}
	}
}
