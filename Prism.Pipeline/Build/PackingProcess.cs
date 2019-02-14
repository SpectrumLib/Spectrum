using System;
using System.IO;
using System.Text;
using Prism.Content;

namespace Prism.Build
{
	// Controls the building of the content pack file and the final cotent item processing and packing
	internal class PackingProcess
	{
		public static readonly string CPACK_NAME = "Content.cpack";
		private static readonly byte[] CPACK_HEADER = Encoding.ASCII.GetBytes("PCP");
		private static readonly byte CPACK_VERSION = 1;

		#region Fields
		public readonly BuildEngine Engine;
		public ContentProject Project => Engine.Project;
		public readonly string PackPath;
		#endregion // Fields

		public PackingProcess(BuildEngine engine)
		{
			Engine = engine;
			PackPath = PathUtils.CombineToAbsolute(Project.Paths.OutputRoot, CPACK_NAME);
		}

		public bool BuildContentPack()
		{
			try
			{
				// Remove the old content pack file
				if (File.Exists(PackPath))
					File.Delete(PackPath);

				// Write the new content pack file
				using (var writer = new BinaryWriter(File.Open(PackPath, FileMode.Create, FileAccess.Write, FileShare.None)))
				{
					// Magic number and version
					writer.Write(CPACK_HEADER);
					writer.Write(CPACK_VERSION);

					// Build flags
					byte buildFlags = (byte)(
						(Project.Properties.Pack     ? 0x01 : 0x00) |
						(Project.Properties.Compress ? 0x02 : 0x00));
					writer.Write(buildFlags);
				}
			}
			catch (Exception e)
			{
				Engine.Logger.EngineError($"Unable to create content pack file, reason: {e.Message}");
				return false;
			}

			// Good to go
			Engine.Logger.EngineInfo($"Created content pack file.");
			return true;
		}
	}
}
