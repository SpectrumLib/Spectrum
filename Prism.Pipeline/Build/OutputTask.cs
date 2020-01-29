/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Prism.Pipeline
{
	// Contains the functionality for building content cache files into the final content pack files
	internal sealed class OutputTask
	{
		private static readonly string PACK_NAME = "Content.cpak";
		private static readonly byte[] PACK_HEADER = Encoding.ASCII.GetBytes("CPAK");
		private const byte PACK_VERSION = 1;

		#region Fields
		public readonly BuildEngine Engine;
		public bool ShouldStop => Engine.ShouldStop;
		public BuildLogger Logger => Engine.Logger;

		private List<PackEntry> _entries;
		#endregion // Fields

		public OutputTask(BuildEngine engine, IEnumerable<BuildTask> tasks)
		{
			Engine = engine;

			// Organize the content items by descending size then cast into entries
			_entries = tasks
							.SelectMany(task => task.Results)
							.OrderByDescending(res => res.Size)
							.Select(res => new PackEntry { Result = res })
							.ToList();
		}

		public bool GenerateOutputFiles(bool release)
		{
			if (release) // Perform compression and binning -  !!TODO!! actually perform compression
			{
				// This assumes that the compression can only make the data smaller (not a bad assumption)
				//   Even if the data does get bigger, it will only get bigger by a factor of 1/256

				// Prepare the bin files (and find any that are too large)
				ulong packSize = Engine.Project.Properties.PackSize * 1024 * 1024; // MB to B
				ulong totalSize = (ulong)_entries.Sum(ent => (long)ent.DataSize);
				if (_entries.FirstOrDefault(ent => ent.DataSize > packSize) is var badEnt && badEnt.Result != null)
				{
					Engine.Logger.EngineError($"The item {badEnt.Item.ItemName} is too large for the pack size setting.");
					return false;
				}
				ulong[] bins = new ulong[(uint)Math.Ceiling(totalSize / (double)packSize)];
				Array.Fill(bins, 0ul);

				// Assign the entries into bin files
				uint[] indices = new uint[_entries.Count];
				for (int i = 0; i < _entries.Count; ++i)
				{
					// Find and shrink the first bin file that fits the item
					int bidx = Array.FindIndex(bins, bin => (bin + _entries[i].DataSize) <= packSize);
					bins[bidx] += _entries[i].DataSize;
					indices[i] = (uint)bidx;
				}

				// Perform the binning
				FileStream[] bstreams = new FileStream[bins.Length];
				try
				{
					// Open the bin streams
					for (int i = 0; i < bins.Length; ++i)
					{
						var binpath = Path.Combine(Engine.Project.Paths.Output.FullName, $"{i}.cbin");
						bstreams[i] = File.Open(binpath, FileMode.Create, FileAccess.Write, FileShare.None);
					}

					// Write each content file into the bin stream, update the entry
					for (int i = 0; i < _entries.Count; ++i)
					{
						var bidx = _entries[i].BinIndex;
						using var instream = _entries[i].Item.OutputFile.OpenRead();
						var outstream = bstreams[bidx];

						var off = outstream.Position;
						instream.CopyTo(outstream);
						var size = outstream.Position - off;

						_entries[i] = new PackEntry {
							Result = _entries[i].Result,
							BinIndex = bidx,
							BinSize = (ulong)size,
							Offset = (ulong)off
						};
					}
				}
				catch (Exception e)
				{
					Engine.Logger.EngineError($"Failed to back bin file: {e.Message}.");
					return false;
				}
				finally
				{
					foreach (var bs in bstreams)
						bs?.Dispose();
				}
			}
			else // For debug builds, simply link the cache binaries to the output directory
			{
				// Try to link, otherwise perform a direct copy
				foreach (var entry in _entries)
				{
					var linkPath = Path.Combine(Engine.Project.Paths.Output.FullName, 
						$"{entry.Item.ItemName}.cbin");

					if (!PathUtils.CreateFileLink(entry.Item.OutputFile.FullName, linkPath))
					{
						Engine.Logger.EngineWarn($"Failed to create link for '{entry.Item.ItemName}', copying file...");
						try
						{
							entry.Item.OutputFile.CopyTo(linkPath);
						}
						catch
						{
							Engine.Logger.EngineError($"Failed to copy item '{entry.Item.ItemName}' to output.");
							return false;
						}
					}
				}
			}

			return true;
		}

		// Writes the content pack file
		public bool GenerateContentPack(bool release)
		{
			// Write the cpak
			try
			{
				// Open the file stream
				if (!PathUtils.TryGetFileInfo(Path.Combine(Engine.Project.Paths.Output.FullName, PACK_NAME), out var finfo))
				{
					Logger.EngineError("Could not access content pack file.");
					return false;
				}
				using BinaryWriter writer = new BinaryWriter(
					finfo.Open(FileMode.Create, FileAccess.Write, FileShare.None));

				// Header info
				writer.Write(PACK_HEADER);
				writer.Write(PACK_VERSION);
				uint flags = (release ? 0x01u : 0x00u);
				writer.Write((byte)flags);
				writer.Write(DateTime.UtcNow.ToBinary());

				// Write each item entry
				writer.Write((uint)_entries.Count);
				foreach (var entry in _entries)
				{
					writer.Write(entry.Item.ItemName);
					writer.Write(entry.Item.Type);
					writer.Write(entry.DataSize);
					writer.Write(entry.BinSize);
					writer.Write(entry.Offset);
					writer.Write(entry.BinIndex);
				}
			}
			catch (Exception e)
			{
				Logger.EngineError($"Unable to write content pack - {e.Message}.");
				return false;
			}

			return true;
		}

		private struct PackEntry
		{
			public ItemResult Result;
			public uint BinIndex;
			public ulong BinSize; // Compressed size
			public ulong Offset;

			public ContentItem Item => Result.Item;
			public ulong DataSize => Result.Size; // Uncompressed (real) size
		}

		private static class Kernel32
		{
			public const uint FLAG_DIRECTORY = 0x1;

			[DllImport("kernel32.dll")]
			static extern bool CreateSymbolicLink(
				string lpSymlinkFileName, string lpTargetFileName, uint dwFlags);
		}
	}
}
