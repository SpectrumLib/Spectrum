using System;
using System.Collections.Generic;
using System.Json;

namespace Prism.Content
{
	// Contains the item name, path, and build paramaters as loaded from the content project file
	internal class ContentItem
	{
		private static readonly char[] PAIR_SPLIT = { ';' };
		private static readonly char[] KEYVALUE_SPLIT = { '=' };

		#region Fields
		// The path information for this item
		public ItemPaths Paths;
		// The name of the importer type for this item
		public string ImporterName;
		// The name of the processor type for this item
		public string ProcessorName;
		// The arguments for the importer
		public List<(string Key, string Value)> ImporterArgs;
		// The arguments for the processor
		public List<(string Key, string Value)> ProcessorArgs;

		// Reference to the item as it was defined in the content project file
		public string ItemPath => Paths.ItemPath;
		#endregion // Fields

		private ContentItem(in ItemPaths paths, string iName, string pName, List<(string, string)> iArgs, List<(string, string)> pArgs)
		{
			Paths = paths;
			ImporterName = iName;
			ProcessorName = pName;
			ImporterArgs = iArgs;
			ProcessorArgs = pArgs;
		}

		public static ContentItem LoadJson(string path, JsonObject obj, string rootDir)
		{
			if (!IOUtils.IsValidPath(path))
				throw new Exception($"The item path '{path}' is not valid");

			// Get the importer/processor names
			if (!obj.TryGetValue("itype", out var impName) || (impName.JsonType != JsonType.String))
				throw new Exception($"The item '{path}' did not specify a valid importer string");
			if (!obj.TryGetValue("ptype", out var proName) || (proName.JsonType != JsonType.String))
				throw new Exception($"The item '{path}' did not specify a valid processor string");

			// Make the paths
			if (!IOUtils.TryGetFullPath(path, out string srcPath, rootDir))
				throw new Exception($"The item path '{path}' does not map to a valid filesystem path");
			ItemPaths paths = new ItemPaths {
				ItemPath = path,
				SourcePath = srcPath
			};

			// Parse the type paramters
			List<(string, string)> iArgs = new List<(string, string)>();
			List<(string, string)> pArgs = new List<(string, string)>();
			if (obj.TryGetValue("iargs", out var iaObj))
			{
				if (iaObj.JsonType == JsonType.String)
				{
					var argList = ((string)iaObj).Split(PAIR_SPLIT, StringSplitOptions.RemoveEmptyEntries);
					foreach (var argPair in argList)
					{
						var keyVal = argPair.Split(KEYVALUE_SPLIT, 2);
						if (keyVal.Length != 2)
							throw new Exception($"The item '{path}' specified an invalid importer key/value pair ('{argPair}')");
						iArgs.Add((keyVal[0], keyVal[1]));
					}
				}
				else
					throw new Exception($"The item '{path}' did not specify the importer arguments as a string");
			}
			if (obj.TryGetValue("pargs", out var paObj))
			{
				if (paObj.JsonType == JsonType.String)
				{
					var argList = ((string)paObj).Split(PAIR_SPLIT, StringSplitOptions.RemoveEmptyEntries);
					foreach (var argPair in argList)
					{
						var keyVal = argPair.Split(KEYVALUE_SPLIT, 2);
						if (keyVal.Length != 2)
							throw new Exception($"The item '{path}' specified an invalid processor key/value pair ('{argPair}')");
						pArgs.Add((keyVal[0], keyVal[1]));
					}
				}
				else
					throw new Exception($"The item '{path}' did not specify the processor arguments as a string");
			}

			// Return the item
			return new ContentItem(paths, impName, proName, iArgs, pArgs);
		}
	}
}
