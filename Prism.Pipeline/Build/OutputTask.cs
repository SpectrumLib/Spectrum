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
			// Switch the output based on the build type
			if (release) // Perform compression and binning -  !!TODO!! actually perform compression
			{
				
			}
			else // For debug builds, simply link the cache binaries to the output directory
			{
					
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
			public bool Placed;

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
