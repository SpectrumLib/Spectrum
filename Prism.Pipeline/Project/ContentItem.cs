using System;
using System.Json;

namespace Prism
{
	// Contains the item name, path, and build paramaters as loaded from the content project file
	internal class ContentItem
	{
		#region Fields
		// The path information for this item
		public ItemPaths Paths;
		// The name of the importer type for this item
		public string ImporterName;
		// The name of the processor type for this item
		public string ProcessorName;
		#endregion // Fields

		private ContentItem(in ItemPaths paths, string iName, string pName)
		{
			Paths = paths;
			ImporterName = iName;
			ProcessorName = pName;
		}

		public static ContentItem LoadJson(string path, JsonObject obj, string rootDir)
		{
			if (!IOUtils.IsValidPath(path))
				throw new Exception($"The content item path '{path}' is not valid");

			// Get the importer/processor names
			if (!obj.TryGetValue("importer", out var impName) || (impName.JsonType != JsonType.String))
				throw new Exception($"The content item '{path}' did not specify a valid importer string");
			if (!obj.TryGetValue("processor", out var proName) || (proName.JsonType != JsonType.String))
				throw new Exception($"The content item '{path}' did not specify a valid processor string");

			// Make the paths
			if (!IOUtils.TryGetFullPath(path, out string srcPath, rootDir))
				throw new Exception($"The content item path '{path}' does not map to a valid filesystem path");
			ItemPaths paths = new ItemPaths {
				ItemPath = path,
				SourcePath = srcPath
			};

			// Return the item
			return new ContentItem(paths, impName, proName);
		}
	}
}
