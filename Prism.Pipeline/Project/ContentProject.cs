using System;
using System.IO;
using System.Json;

namespace Prism
{
	internal class ContentProject
	{
		#region Fields
		// The absolute paths for this project
		public ProjectPaths Paths { get; private set; }
		// The properties of the project
		public ProjectProperties Properties { get; private set; }

		// The aboslute path to the file loaded by this content project
		public string FilePath => Paths.ProjectPath;
		#endregion // Fields

		private ContentProject(string path, in ProjectProperties pp)
		{
			Properties = pp;

			// Convert the paths to absolute and perform some validations
			if (!IOUtils.TryGetFullPath(pp.RootDir, out var rPath))
				throw new Exception($"The root content directory '{rPath}' is not a valid filesystem path");
			if (!IOUtils.TryGetFullPath(pp.IntermediateDir, out var iPath))
				throw new Exception($"The intermediate directory '{iPath}' is not a valid filesystem path");
			if (!IOUtils.TryGetFullPath(pp.OutputDir, out var oPath))
				throw new Exception($"The output directory '{oPath}' is not a valid filesystem path");
			if (rPath == iPath || rPath == oPath || iPath == oPath)
				throw new Exception($"The content root, intermediate, and output paths must all be different");
			Paths = new ProjectPaths {
				ProjectPath = path,
				ProjectDirectory = Path.GetDirectoryName(path),
				ContentRoot = rPath,
				IntermediateRoot = iPath,
				OutputRoot = oPath
			};
		}

		#region Load/Save
		public static ContentProject LoadFromFile(string path)
		{
			if (!IOUtils.TryGetFullPath(path, out path))
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
				throw new Exception($"{e.Message} ({e.GetType().Name})");
			}
			JsonValue fileObj = null;
			try
			{
				fileObj = JsonValue.Parse(fileText);
			}
			catch (Exception e)
			{
				throw new Exception($"Invalid Json, {e.Message}");
			}

			// Get and load the properties
			if (!fileObj.ContainsKey("project") || (fileObj["project"].JsonType != JsonType.Object))
				throw new Exception("The content file does not contain the section for project properties");
			if (!ProjectProperties.LoadJson(fileObj["project"] as JsonObject, out var pp, out var missing))
				throw new Exception($"The project properties section does not contain the required entry '{missing}'");

			// Good to go
			return new ContentProject(path, pp);
		}
		#endregion // Load/Save
	}
}
