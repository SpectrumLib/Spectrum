using System;

namespace Prism
{
	// Encapsultes the entire build pipeline state and related objects for a single build instance
	internal class BuildEngine : IDisposable
	{
		#region Fields
		public readonly ContentProject Project; // The loaded content project
		public readonly BuildLogger Logger; // The logger for this engine

		private bool _isDisposed = false;
		#endregion // Fields

		// Creates an initial build pipeline for the given project and settings
		public BuildEngine(ContentProject project, BuildLogger logger)
		{
			Project = project ?? throw new ArgumentNullException(nameof(project));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			Logger.Engine = this;
		}
		~BuildEngine()
		{
			dispose();
		}

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

			_isDisposed = true;
		}
		#endregion // IDisposable
	}
}
