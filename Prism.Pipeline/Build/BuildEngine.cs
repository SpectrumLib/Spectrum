using System;
using System.Threading.Tasks;

namespace Prism
{
	// Encapsultes the entire build pipeline state and related objects for a single build instance
	//   The tasks run in a separate thread, so the returned tasks should be waited on
	internal class BuildEngine : IDisposable
	{
		#region Fields
		public readonly ContentProject Project; // The loaded content project
		public readonly BuildLogger Logger; // The logger for this engine

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

			_manager = new BuildTaskManager(this, threads);
		}
		~BuildEngine()
		{
			dispose();
		}

		#region Actions
		// Starts the build task (force=true -> rebuild)
		public Task Build(bool force)
		{
			if (Busy)
				throw new InvalidOperationException("Cannot start a build task while a task is already running");

			return new Task(() => _manager.Build(force));
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
