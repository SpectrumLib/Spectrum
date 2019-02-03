using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Prism
{
	// Creates new default project and content files
	internal static class NewFileGenerator
	{
		private static readonly string NEW_PROJECT_RES = "Prism.Resources.Default.prism";

		// Creates a new default, empty project file at the given path
		public static string NewProjectFile(string path)
		{
			// If a directory is specified, append the file name
			string ext = Path.GetExtension(path);
			if (ext.Length == 0)
				path = Path.Combine(path, "Content.prism");

			// Check if it already exists
			path = Path.GetFullPath(path);
			var pathDir = Path.GetDirectoryName(path);
			if (File.Exists(path))
				throw new Exception($"A file already exists at the path '{path}'");
			if (!Directory.Exists(pathDir))
				Directory.CreateDirectory(pathDir);

			// Write the embedded resource as the new file
			using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(NEW_PROJECT_RES)))
			using (var writer = new StreamWriter(File.Open(path, FileMode.CreateNew, FileAccess.Write, FileShare.None), Encoding.Unicode))
			{
				var raw = reader.ReadToEnd();
				writer.Write(raw);
			}

			// Return the absolute path of the new file
			return path;
		}
	}
}
