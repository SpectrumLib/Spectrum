using System;

namespace Prism
{
	// Contains filesystem path information for a ContentItem, all paths are absolute
	internal struct ItemPaths
	{
		public string ItemPath; // The path of the item given in the project file (not absolute)
		public string SourcePath; // The path to the input file (ItemPath translated to an absolute path)
	}
}
