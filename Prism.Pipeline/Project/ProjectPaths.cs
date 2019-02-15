using System;
using System.Collections.Generic;
using System.IO;

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

		public static bool LoadPaths(string path, in ProjectProperties pp, out ProjectPaths paths, out string error)
		{
			paths = default;
			error = null;

			var pDir = Path.GetDirectoryName(path);
			if (!PathUtils.TryGetFullPath(pp.RootDir, out var rPath, pDir))
			{
				error = $"the root content directory '{rPath}' is not a valid filesystem path";
				return false;
			}
			if (!PathUtils.TryGetFullPath(pp.IntermediateDir, out var iPath, pDir))
			{
				error = $"the intermediate directory '{iPath}' is not a valid filesystem path";
				return false;
			}
			if (!PathUtils.TryGetFullPath(pp.OutputDir, out var oPath, pDir))
			{
				error = $"the output directory '{oPath}' is not a valid filesystem path";
				return false;
			}
			if (rPath == iPath || rPath == oPath || iPath == oPath)
			{
				error = $"the content root, intermediate, and output paths must all be different";
				return false;
			}

			paths = new ProjectPaths {
				ProjectPath = path,
				ProjectDirectory = pDir,
				ContentRoot = rPath,
				IntermediateRoot = iPath,
				OutputRoot = oPath
			};
			return true;
		}

		public static bool LoadOverrides(in ProjectPaths paths, Dictionary<string, object> os, out ProjectPaths? opaths, out string error)
		{
			opaths = null;
			error = null;
			if (os == null)
				return true;

			var copy = paths;
			bool changed = false;

			if (os.ContainsKey("ipath"))
			{
				if (PathUtils.TryGetFullPath((string)os["ipath"], out var apath, paths.ProjectDirectory))
				{
					copy.IntermediateRoot = apath;
					changed = paths.IntermediateRoot != copy.IntermediateRoot; 
				}
				else
				{
					error = $"invalid ipath override '{(string)os["ipath"]}'";
					return false;
				}
			}
			if (os.ContainsKey("opath"))
			{
				if (PathUtils.TryGetFullPath((string)os["opath"], out var apath, paths.ProjectDirectory))
				{
					copy.OutputRoot = apath;
					changed = changed || (paths.OutputRoot != copy.OutputRoot); 
				}
				else
				{
					error = $"invalid opath override '{(string)os["opath"]}'";
					return false;
				}
			}

			opaths = changed ? copy : (ProjectPaths?)null;
			return true;
		}
	}
}
