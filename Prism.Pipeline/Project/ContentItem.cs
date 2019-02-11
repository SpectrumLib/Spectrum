using System;
using System.Collections.Generic;
using System.IO;
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
		// The arguments for the processor
		public List<(string Key, string Value)> ProcessorArgs;

		// Reference to the item as it was defined in the content project file
		public string ItemPath => Paths.ItemPath;
		#endregion // Fields

		private ContentItem(in ItemPaths paths, string iName, string pName, List<(string, string)> pArgs)
		{
			Paths = paths;
			ImporterName = iName;
			ProcessorName = pName;
			ProcessorArgs = pArgs;
		}

		public static string ToIntermedateFile(string path) =>
			Path.GetFileNameWithoutExtension(path.Replace(Path.DirectorySeparatorChar, '.').Replace(Path.AltDirectorySeparatorChar, '.'));

		public static ContentItem LoadJson(string path, JsonObject obj, in ProjectPaths ppaths)
		{
			if (!PathUtils.IsValidPath(path))
				throw new Exception($"The item path '{path}' is not valid");

			// Get the importer/processor names
			if (!obj.TryGetValue("itype", out var impName) || (impName.JsonType != JsonType.String))
				throw new Exception($"The item '{path}' did not specify a valid importer string");
			if (!obj.TryGetValue("ptype", out var proName) || (proName.JsonType != JsonType.String))
				throw new Exception($"The item '{path}' did not specify a valid processor string");

			// Make the paths
			if (!PathUtils.TryGetFullPath(path, out string srcPath, ppaths.ContentRoot))
				throw new Exception($"The item path '{path}' does not map to a valid filesystem path");
			var intFile = ToIntermedateFile(path);
			ItemPaths paths = new ItemPaths {
				ItemPath = path,
				SourcePath = srcPath,
				OutputFile = intFile,
				OutputPath = PathUtils.CombineToAbsolute(ppaths.IntermediateRoot, intFile) + ".pcf",
				CachePath = PathUtils.CombineToAbsolute(ppaths.IntermediateRoot, intFile) + ".bcache"
			};

			// Parse the processor paramters
			List<(string, string)> pArgs = null;
			if (obj.TryGetValue("pargs", out var paObj))
			{
				if (paObj.JsonType == JsonType.String)
				{
					try
					{
						pArgs = ParseArgs((string)paObj);
					}
					catch (Exception e)
					{
						throw new Exception($"The item '{path}' specified an invalid processor key/value pair ('{e.Message}')");
					}
				}
				else
					throw new Exception($"The item '{path}' did not specify the processor arguments as a string");
			}

			// Return the item
			return new ContentItem(paths, impName, proName, pArgs);
		}

		public static List<(string, string)> ParseArgs(string args)
		{
			List<(string, string)> pRet = new List<(string, string)>();
			var argList = args.Split(PAIR_SPLIT, StringSplitOptions.RemoveEmptyEntries);
			foreach (var argPair in argList)
			{
				var keyVal = argPair.Split(KEYVALUE_SPLIT, 2);
				if (keyVal.Length != 2)
					throw new Exception(argPair);
				pRet.Add((keyVal[0], keyVal[1]));
			}
			return pRet;
		}
	}
}
