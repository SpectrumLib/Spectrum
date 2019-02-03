using System;
using System.IO;

namespace Prism
{
	// Various utilities for filesystem IO
	internal static class IOUtils
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
		public static bool TryGetFullPath(string path, out string fullPath)
		{
			try
			{
				fullPath = Path.GetFullPath(path);
				return true;
			}
			catch { fullPath = path; return false; }
		}
	}
}
