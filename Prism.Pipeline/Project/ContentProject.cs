using System;
using System.Collections.Generic;
using System.IO;
using System.Json;

namespace Prism.Content
{
	// Holds the properties and raw content items for a content project file
	internal class ContentProject
	{
		#region Fields
		// The absolute paths for this project, loaded from the project file
		public readonly ProjectPaths LoadedPaths;
		// The path overrides of the project
		public ProjectPaths? PathOverrides { get; private set; } = null;
		// The paths to use
		public ProjectPaths Paths => PathOverrides ?? LoadedPaths;
		// The properties of the project, loaded from the project file
		public readonly ProjectProperties LoadedProperties;
		// The property overrides of the project
		public ProjectProperties? PropertyOverrides { get; private set; } = null;
		// The properties to use
		public ProjectProperties Properties => PropertyOverrides ?? LoadedProperties;

		// The aboslute path to the file loaded by this content project
		public string FilePath => Paths.ProjectPath;

		// The list of content items contained in this project
		private readonly Dictionary<string, ContentItem> _items;
		public IReadOnlyDictionary<string, ContentItem> Items => _items;
		#endregion // Fields

		private ContentProject(string path, in ProjectPaths paths, in ProjectProperties pp, Dictionary<string, ContentItem> items,
			in ProjectPaths? oPaths, in ProjectProperties? oProps)
		{
			LoadedPaths = paths;
			PathOverrides = oPaths;
			LoadedProperties = pp;
			PropertyOverrides = oProps;
			_items = items;
		}

		#region Load/Save
		public static ContentProject LoadFromFile(string path, Dictionary<string, object> overrides = null)
		{
			if (!PathUtils.TryGetFullPath(path, out path))
				throw new Exception($"The path '{path}' is not a valid filesystem path");
			if (!File.Exists(path))
				throw new Exception($"The content file '{path}' does not exist");

			// Load the file into a json object
			string fileText = "";
			try
			{
				fileText = File.ReadAllText(path);
			}
			catch (Exception e)
			{
				throw new Exception($"Unable to read content file, {e.Message}", e);
			}
			JsonObject fileObj = null;
			try
			{
				var json = JsonValue.Parse(fileText);
				if (json.JsonType != JsonType.Object)
					throw new Exception("the file is not a top-level Json object");
				fileObj = json as JsonObject;
			}
			catch (Exception e)
			{
				throw new Exception($"Invalid Json, {e.Message}", e);
			}

			// Get and load the properties
			if (!fileObj.TryGetValue("project", out var propObj) || (propObj.JsonType != JsonType.Object))
				throw new Exception("The content file does not contain the section for project properties");
			if (!ProjectProperties.LoadJson(propObj as JsonObject, out var pp, out var error))
				throw new Exception($"Could not load the project properties, reason: {error}");
			ProjectProperties.LoadOverrides(pp, overrides, out var opp);

			// Convert the paths to absolute and perform some validations
			if (!ProjectPaths.LoadPaths(path, opp ?? pp, out var paths, out error))
				throw new Exception($"Unable to load project paths, reason: {error}");
			if (!ProjectPaths.LoadOverrides(paths, overrides, out var opaths, out error))
				throw new Exception($"Unable to load project path overrides, reason: {error}");

			// Load the items
			if (!fileObj.TryGetValue("items", out var itemsObj) || (itemsObj.JsonType != JsonType.Object))
				throw new Exception("The content file does not contain the section for content items");
			Dictionary<string, ContentItem> items = new Dictionary<string, ContentItem>();
			foreach (var item in (itemsObj as JsonObject))
			{
				if (item.Value.JsonType != JsonType.Object)
					throw new Exception($"The content item '{item.Key}' is not a valid Json object");
				var citem = ContentItem.LoadJson(item.Key, item.Value as JsonObject, opaths ?? paths);
				if (items.ContainsKey(citem.ItemPath))
					throw new Exception($"The content project file has more than one entry for the item '{citem.ItemPath}'");
				items.Add(citem.ItemPath, citem);
			}

			// Good to go
			return new ContentProject(path, paths, pp, items, opaths, opp);
		}
		#endregion // Load/Save
	}
}
