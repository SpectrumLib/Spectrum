using System;

namespace Prism
{
	// Encapsultes the entire build pipeline state and related objects for a single build instance
	internal class BuildEngine
	{
		#region Fields
		public readonly ContentProject Project; // The loaded content project
		#endregion // Fields

		// Creates an initial build pipeline for the given project and settings
		public BuildEngine(ContentProject project)
		{
			Project = project ?? throw new ArgumentNullException(nameof(project));
		}
	}
}
