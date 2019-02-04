using System;

namespace Prism.Content
{
	// All paths in this type are absolute
	internal struct ProjectPaths
	{
		#region Fields
		// The path to the content project file, with the filename
		public string ProjectPath;
		// The directory that the content project file lives in
		public string ProjectDirectory;
		// The root directory to search for content files in
		public string ContentRoot;
		// The root directory for intermediate content files to be written to
		public string IntermediateRoot;
		// The root directory for content output to be written to
		public string OutputRoot;
		#endregion // Fields
	}
}
