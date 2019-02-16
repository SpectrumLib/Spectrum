using System;
using System.Threading.Tasks;
using Prism.Content;

namespace Prism.Build
{
	// Encapsultes the entire build pipeline state and related objects for a single build instance
	//   The tasks run in a separate thread, so the returned tasks should be waited on
	internal class BuildEngine : IDisposable
	{
		#region Fields
		public readonly ContentProject Project; // The loaded content project
		public readonly BuildLogger Logger; // The logger for this engine

		public readonly StageCache StageCache; // Holds the importer & processor types available to this engine

		private readonly BuildTaskManager _manager;
		public bool Busy => _manager.Busy; // If there is a currently a build/clean process happening

		private bool _isDisposed = false;
		#endregion // Fields

		// Creates an initial build pipeline for the given project and settings
		public BuildEngine(ContentProject project, BuildLogger logger, uint threads)
		{
			Project = project ?? throw new ArgumentNullException(nameof(project));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			Logger.Engine = this;

			StageCache = new StageCache(this);

			_manager = new BuildTaskManager(this, threads);
		}
		~BuildEngine()
		{
			dispose();
		}

		#region Actions
		// Starts the build task
		public Task Build(bool rebuild, bool release)
		{
			if (Busy)
				throw new InvalidOperationException("Cannot start a build task while a task is already running");

			return new Task(() => _manager.Build(rebuild, release));
		}

		// Starts the clean task
		public Task Clean()
		{
			if (Busy)
				throw new InvalidOperationException("Cannot start a clean task while a task is already running");

			return new Task(() => _manager.Clean());
		}

		// Cancels the current running task (if there is one)
		public Task Cancel()
		{
			if (!Busy)
				throw new InvalidOperationException("Cannot cancel a task if no tasks are running");

			return new Task(() => _manager.Cancel());
		}
		#endregion // Actions

		#region IDisposable
		public void Dispose()
		{
			dispose();
			GC.SuppressFinalize(this);
		}

		public void dispose()
		{
			if (_isDisposed)
				return;

			if (Busy)
				Cancel().Wait();

			_isDisposed = true;
		}
		#endregion // IDisposable
	}
}
