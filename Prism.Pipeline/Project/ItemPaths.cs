using System;

namespace Prism.Content
{
	// Contains filesystem path information for a ContentItem
	internal struct ItemPaths
	{
		public string ItemPath; // The path of the item given in the project file (not absolute)
		public string SourcePath; // The absolute path to the input file (ItemPath translated to an absolute path)
		public string OutputFile; // The filename for the intermediate file (path separators replaced with periods)
		public string OutputPath; // The abosolute path to the intermediate file (from IntermediateFile and project settings)
		public string CachePath; // The absolute path to the cache file for the build event
	}
}
