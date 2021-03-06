﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Prism.Content;

namespace Prism.Build
{
	// Manages the distribution of content items to build tasks, as well as maintining the state of the pipeline
	//  and task cancellation
	internal class BuildTaskManager
	{
		#region Fields
		public readonly BuildEngine Engine;
		public ContentProject Project => Engine.Project;

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

		// Objects for tracking and dispensing items to build tasks
		private readonly object _taskLock = new object();
		private uint _itemIndex = 0;
		private IEnumerator<KeyValuePair<string, ContentItem>> _itemEnumerator;

		// The list of available tasks
		private readonly BuildTask[] _tasks;

		// Information relating to temporary build files
		public readonly string TempFilePath;
		private readonly object _tmpFileLock = new object();
		private uint _tmpFileIndex = 0;

		// Tracks if the current process is a clean action
		private bool _isCleaning = false;
		#endregion // Fields

		public BuildTaskManager(BuildEngine engine, uint threads)
		{
			Engine = engine;

			TempFilePath = Path.Combine(engine.Project.Paths.IntermediateRoot, ".__tmp__");

			_tasks = new BuildTask[threads];
			for (uint i = 0; i < threads; ++i)
				_tasks[i] = new BuildTask(this);
		}

		// Called by BuildTask instances to get the next available content item to start building
		//   Returns false when no items are left
		internal bool GetTaskItem(out BuildEvent item)
		{
			lock (_taskLock)
			{
				if (_itemEnumerator.MoveNext())
				{
					var ci = _itemEnumerator.Current.Value;
					item = BuildEvent.FromItem(Engine, ci, _itemIndex++);
					return true;
				}
				else
				{
					item = null;
					return false;
				}
			}
		}

		// Attempts to load the settings used in the last build to see if the output settings have changed
		//   Returns true if the settings have changed (need to redo the packing process)
		//   This checks for things like if the output directory or packing settings have changed
		//   Also check if the compression changed but might not be caught by the content items, which can
		//   happen when switching around intermediate and output roots in a specific way
		private bool CheckOutputChanged()
		{
			var packPath = PackingProcess.GetPackPath(Project.Paths.OutputRoot);
			if (!File.Exists(packPath))
				return true;

			try
			{
				using (var reader = new BinaryReader(File.Open(packPath, FileMode.Open, FileAccess.Read, FileShare.None)))
				{
					byte flags = reader.ReadBytes(6)[5]; // 6-byte header, last byte is build flags
					bool rel = (flags & 0x01) > 0;
					bool compress = (flags & 0x02) > 0;
					uint packSize = reader.ReadUInt32();
					return (Engine.IsRelease != rel) || (compress != Engine.Compress) || 
						(Project.Properties.PackSize != packSize);
				}
			}
			catch { return true; }
		}

		// Gets the name of a unique temporary file that can be used by a build pipeline
		internal string ReserveTempFile()
		{
			lock (_tmpFileLock)
			{
				return Path.Combine(TempFilePath, $"{_tmpFileIndex++}.tmp");
			}
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
				Engine.Logger.BuildStart(rebuild, Engine.IsRelease);

				// Reset the item enumerator to the beginning
				_itemEnumerator = Project.Items.GetEnumerator();
				_itemIndex = 0;

				// Ensure that the intermediate, output, and temp directories exist
				if (!Directory.Exists(Project.Paths.IntermediateRoot))
					Directory.CreateDirectory(Project.Paths.IntermediateRoot);
				if (!Directory.Exists(Project.Paths.OutputRoot))
					Directory.CreateDirectory(Project.Paths.OutputRoot);
				if (Directory.Exists(TempFilePath))
					Directory.Delete(TempFilePath, true);
				Directory.CreateDirectory(TempFilePath);

				// Launch the build tasks
				foreach (var task in _tasks)
					task.Start(rebuild);

				// Wait for the tasks to complete
				foreach (var task in _tasks)
					task.Join();

				// Clean up the temp files
				Directory.Delete(TempFilePath, true);

				// Check for exit request before moving on
				if (ShouldStop)
					return;

				// Clean up, lots of temp items probably hanging around at this point
				GC.Collect();

				// Fail out if any items failed
				if (_tasks.Any(task => task.Results.FailCount > 0))
				{
					Engine.Logger.EngineError("One or more items failed to build, skipping content output step.");
					return;
				}

				// Check if the output settings have changed
				bool outChanged = CheckOutputChanged();

				// Test if we can skip output, otherwise report
				if (_tasks.All(task => task.Results.PassCount == task.Results.SkipCount))
				{
					if (!outChanged)
					{
						Engine.Logger.EngineInfo($"Skipping content output step, no items were rebuilt.", true);
						success = true;
						return;
					}
					else
						Engine.Logger.EngineInfo($"No items were rebuilt, but the output settings have changed.", true);
				}
				Engine.Logger.BuildContinue(timer.Elapsed);

				// Create the output process and build the metadata
				var outProc = new PackingProcess(this, _tasks);
				if (!outProc.BuildContentPack())
					return;

				// One last exit check before starting the output process
				if (ShouldStop)
					return;

				// Perform the final output steps (will check for cancellation)
				success = outProc.ProcessOutput(outChanged);
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

				// Intermediate files
				if (Directory.Exists(Project.Paths.IntermediateRoot))
				{
					// Clean all .pcf files out of the intermediate path
					var iInfo = (new DirectoryInfo(Project.Paths.IntermediateRoot)).GetFiles("*.pcf", SearchOption.TopDirectoryOnly);
					for (int i = 0; i < iInfo.Length; ++i)
					{
						// Check for stop every 5th item, middle ground between too often (slow) and not enough (why implement cancelling to begin with)
						if (((i % 5) == 0) && ShouldStop)
							return;
						iInfo[i].Delete();
					}

					// Clean all .bcache files out of the intermediate path
					iInfo = (new DirectoryInfo(Project.Paths.IntermediateRoot)).GetFiles("*.bcache", SearchOption.TopDirectoryOnly);
					for (int i = 0; i < iInfo.Length; ++i)
					{
						// Check for stop every 5th item, middle ground between too often (slow) and not enough (why implement cancelling to begin with)
						if (((i % 5) == 0) && ShouldStop)
							return;
						iInfo[i].Delete();
					}
				}

				// Output files
				if (Directory.Exists(Project.Paths.OutputRoot))
				{
					// Clean the content pack file from the output
					string packPath = PathUtils.CombineToAbsolute(Project.Paths.OutputRoot, PackingProcess.CPACK_NAME);
					if (File.Exists(packPath))
						File.Delete(packPath);

					// Clean the individual items out (no packing)
					var oInfo = (new DirectoryInfo(Project.Paths.OutputRoot)).GetFiles("*.dci", SearchOption.TopDirectoryOnly);
					for (int i = 0; i < oInfo.Length; ++i)
					{
						// Check for stop every 5th item, middle ground between too often (slow) and not enough (why implement cancelling to begin with)
						if (((i % 5) == 0) && ShouldStop)
							return;
						oInfo[i].Delete();
					}
				}

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
			if (!Busy)
				return;

			ShouldStop = true;
			if (_isCleaning)
			{
				// Crude way to wait for the clean process to finish
				while (Busy)
					Thread.Sleep(50);
			}
			else
			{
				foreach (var task in _tasks)
					task.Join();
			}
		}
		#endregion // Task Functions
	}
}
