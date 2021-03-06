﻿using System;
using System.IO;

namespace Prism
{
	// Various utilities for filesystem IO
	internal static class PathUtils
	{
		// Checks if the given path is a valid filesystem URI
		public static bool IsValidPath(string path)
		{
			try
			{
				Path.GetFullPath(path);
				return true;
			}
			catch { return false; }
		}

		// Attempts to convert the path into an absolute path, returns if the path was valid and could be converted
		// Optional override for the working directory to fix paths relative to
		public static bool TryGetFullPath(string path, out string fullPath, string workingDir = null)
		{
			try
			{
				if (!Path.IsPathRooted(path))
				{
					if (workingDir == null)
						workingDir = Directory.GetCurrentDirectory();
					path = Path.Combine(workingDir, path);
				}
				fullPath = Path.GetFullPath(path);
				return true;
			}
			catch { fullPath = path; return false; }
		}

		// Combines paths and makes them absolute in one step (no validity checking on this)
		public static string CombineToAbsolute(params string[] paths) => Path.GetFullPath(Path.Combine(paths));
	}
}
