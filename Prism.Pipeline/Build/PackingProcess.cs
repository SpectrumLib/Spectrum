using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Prism.Content;

namespace Prism.Build
{
	// Controls the building of the content pack file and the final content item processing and packing
	internal class PackingProcess
	{
		public static readonly string CPACK_NAME = "Content.cpak";
		private static readonly byte[] CPACK_HEADER = Encoding.ASCII.GetBytes("CPAK");
		private static readonly byte[] CITEM_HEADER = Encoding.ASCII.GetBytes("PCI");
		private static readonly byte CPACK_VERSION = 1;

		#region Fields
		public readonly BuildTaskManager Manager;
		public BuildEngine Engine => Manager.Engine;
		public ContentProject Project => Manager.Engine.Project;
		public readonly string PackPath;

		private readonly BuildTask[] _tasks;

		private Dictionary<string, (string Name, uint Hash)> _loaders = null;
		public IReadOnlyDictionary<string, (string Name, uint Hash)> Loaders => _loaders;
		#endregion // Fields

		public PackingProcess(BuildTaskManager manager, BuildTask[] tasks)
		{
			Manager = manager;
			PackPath = GetPackPath(Project.Paths.OutputRoot);
			_tasks = tasks;
		}

		public static string GetPackPath(string outRoot) => PathUtils.CombineToAbsolute(outRoot, CPACK_NAME);

		// Builds the content pack file that describes the content build in this pipeline
		public bool BuildContentPack()
		{
			// Generate a list of unique used content loader names and their hashes
			_loaders = _tasks
				.SelectMany(task => task.Processors)
				.Select(pair => (pair.Key, pair.Value.LoaderName, pair.Value.LoaderHash))
				.Distinct()
				.ToDictionary(pair => pair.Key, pair => (pair.LoaderName, pair.LoaderHash));

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
						(Engine.Release  ? 0x01 : 0x00) |
						(Engine.Compress ? 0x02 : 0x00));
					writer.Write(buildFlags);

					// The pack size
					writer.Write(Project.Properties.PackSize);

					// Write the number of loaders, then all of the loader names and hashes as pairs
					writer.Write((uint)_loaders.Count);
					_loaders.Values.ToList().ForEach(pair => { writer.Write(pair.Name); writer.Write(pair.Hash); });
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
		public bool ProcessOutput(bool force)
		{
			if (Engine.Release) return releaseOutput(force);
			else return debugOutput(force);
		}

		// Implements content output with no packing
		private bool debugOutput(bool force)
		{
			var results = _tasks.Select(t => t.Results);
			foreach (var result in results)
			{
				var items = force ? result.PassItems : result.PassItems.Where(item => !item.Skipped);
				foreach (var item in items)
				{
					// Check for cancel
					if (Manager.ShouldStop)
						return false;

					// The source and destination paths for this content item
					var srcPath = item.Item.Paths.OutputPath;
					var dstPath = PathUtils.CombineToAbsolute(Project.Paths.OutputRoot, item.Item.Paths.OutputFile) + ".pci";

					try
					{
						// Will overwrite the old file, if there is one
						using (var dstFile = File.Open(dstPath, FileMode.Create, FileAccess.Write, FileShare.None))
						using (var writer = new BinaryWriter(dstFile))
						{
							writer.Write(CITEM_HEADER);
							writer.Write(CPACK_VERSION);

							// Write the loader hash
							var loader = _loaders[item.Item.ProcessorName];
							writer.Write(loader.Hash);

							// Write the length of the raw item data
							writer.Write(item.Size);

							// Copy the output file from the pipeline to the new output file
							using (var srcFile = File.Open(srcPath, FileMode.Open, FileAccess.Read, FileShare.None))
							{
								srcFile.CopyTo(dstFile);
							}
						}
					}
					catch (Exception e)
					{
						Engine.Logger.EngineError($"Unable to process item '{item.Item.ItemPath}' to output, reason: {e.Message}");
						return false;
					}

					// Report success
					Engine.Logger.ItemPack(item.Item, 0);
				}
			}

			return true;
		}

		// Implements content output with packing
		private bool releaseOutput(bool force)
		{
			// Create and run the item binner
			Engine.Logger.EngineInfo($"Calculating item packs and offsets.", true);
			var binner = new ItemBinner(this, _tasks);
			if (!binner.MakeBins())
				return false;

			// Check if we should exit
			if (Manager.ShouldStop)
			{
				Engine.Logger.EngineError("The build process was cancelled during the packing process.");
				return false;
			}

			// Update the pack file with the offsets (will open to the end of the loader hashes)
			Engine.Logger.EngineInfo("Writing item name map to content pack file.", true);
			try
			{
				using (var writer = new BinaryWriter(File.Open(PackPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)))
				{
					writer.Seek(0, SeekOrigin.End);

					// Write the total number of pack files
					writer.Write((uint)binner.Bins.Count);

					// For each bin
					foreach (var bin in binner.Bins)
					{
						// Write the number of items in the bin
						writer.Write((uint)bin.Items.Count);

						// Write the name, length, offset, and loader hash of each item
						foreach (var item in bin.Items)
						{
							writer.Write(item.Item.Paths.OutputFile);
							writer.Write(item.Size);
							writer.Write(item.Offset);
							var loader = _loaders[item.Item.ProcessorName];
							writer.Write(loader.Hash);
						}
					}
				}
			}
			catch (Exception e)
			{
				Engine.Logger.EngineError($"Unable to update content pack file with name map, reason: {e.Message}");
				return false;
			}

			// Check if we should exit
			if (Manager.ShouldStop)
			{
				Engine.Logger.EngineError("The build process was cancelled during the packing process.");
				return false;
			}

			return true;
		}
	}
}
