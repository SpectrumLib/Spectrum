using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
		public readonly BuildTaskManager Manager;
		public BuildEngine Engine => Manager.Engine;
		public ContentProject Project => Manager.Engine.Project;
		public readonly string PackPath;

		private readonly BuildTask[] _tasks;

		private List<(string Name, uint Hash)> _loaders = null;
		#endregion // Fields

		public PackingProcess(BuildTaskManager manager, BuildTask[] tasks)
		{
			Manager = manager;
			PackPath = PathUtils.CombineToAbsolute(Project.Paths.OutputRoot, CPACK_NAME);
			_tasks = tasks;
		}

		// Builds the .cpack metadata file that describes a pack of processed content
		public bool BuildContentPack()
		{
			// Generate a list of unique used content loader names and their hashes
			_loaders = _tasks
				.SelectMany(task => task.Processors)
				.Select(pair => (pair.Value.LoaderName, pair.Value.LoaderHash))
				.Distinct()
				.ToList();

			// Try to write out the new content pack
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

					// Write the number of loaders, then all of the loader names and hashes as pairs
					writer.Write((uint)_loaders.Count);
					_loaders.ForEach(pair => { writer.Write(pair.Name); writer.Write(pair.Hash); });
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

		// Performs the final processing and moving to the output, potentially packing the content
		public bool ProcessOutput(bool pack)
		{
			if (pack) return packOutput();
			else return noPackOutput();
		}

		// Implements content output with no packing
		private bool noPackOutput()
		{
			return true;
		}

		// Implements content output with packing
		private bool packOutput()
		{
			Engine.Logger.EngineError("Packing content is not yet implemented.");
			return false;
		}
	}
}
