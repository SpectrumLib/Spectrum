﻿/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Spectrum.Content
{
	// Contains content file metadata loaded from a .cpak file generated by Prism
	// Also caches objects, such as loader types, that are required by the pack file
	internal class ContentPack
	{
		public static readonly string FILE_EXTENSION = ".cpak";
		public static readonly string DEBUG_EXTENSION = ".dci";

		private static readonly Dictionary<string, ContentPack> _PackCache =
			new Dictionary<string, ContentPack>();

		#region Fields
		// Pack file info
		public readonly string FilePath;
		public readonly string Directory;
		public readonly bool ReleaseMode;
		public readonly uint PackSize;
		public readonly uint Timestamp;

		// Dictionary of loader hashes and their instances
		public Dictionary<uint, LoaderType> Loaders { get; private set; }

		// Bin files (will only exist for release built content)
		public BinFile[] BinFiles { get; private set; }

		// The map of content items names to bin files and bin indices
		public Dictionary<string, (uint BinNum, uint Index)> ItemMap { get; private set; }
		#endregion // Fields

		private ContentPack(string path, BinaryReader reader)
		{
			FilePath = path;
			Directory = Path.GetDirectoryName(path);

			// Validate the header
			Span<byte> header = stackalloc byte[5];
			reader.Read(header);
			if (header[0] != 'C' || header[1] != 'P' || header[2] != 'A' || header[3] != 'K')
				throw new Exception("invalid header.");
			if (header[4] != 1)
				throw new Exception("invalid cpak version number.");

			// Build flags
			byte flags = reader.ReadByte();
			ReleaseMode = (flags & 0x01) > 0;

			// Other build info
			PackSize = reader.ReadUInt32();
			Timestamp = reader.ReadUInt32();

			// Load the hash/name loader map
			uint lcount = reader.ReadUInt32();
			var loaders = new List<(uint Hash, string Name)>((int)lcount);
			for (uint i = 0; i < lcount; ++i)
			{
				var lname = reader.ReadString();
				var lhash = reader.ReadUInt32();
				loaders.Add((lhash, lname));
			}

			// Get the loader types
			Loaders = new Dictionary<uint, LoaderType>();
			foreach (var lpair in loaders)
			{
				var ltype = LoaderCache.GetOrLoad(lpair.Name);
				Loaders.Add(lpair.Hash, ltype);
			}

			// Load the bin files (if release)
			if (ReleaseMode)
			{
				uint bcount = reader.ReadUInt32();
				BinFiles = new BinFile[bcount];
				for (uint i = 0; i < bcount; ++i)
					BinFiles[i] = BinFile.LoadFromStream(i, Directory, Timestamp, reader);

				// Create the mapping
				ItemMap = new Dictionary<string, (uint BinNum, uint Index)>(BinFiles.Sum(bf => bf.Entries.Length));
				for (int bi = 0; bi < BinFiles.Length; ++bi)
				{
					var bf = BinFiles[bi];
					uint ii = 0;
					foreach (var item in bf.Entries)
						ItemMap.Add(item.Name, ((uint)bi, ii++));
				}
			}
			else
			{
				BinFiles = null;
				ItemMap = null;
			}
		}

		// Attempts to get the BinEntry information for a content item
		public bool TryGetItem(string name, out uint binNum, out BinEntry item)
		{
			if (ItemMap.ContainsKey(name))
			{
				var map = ItemMap[name];
				binNum = map.BinNum;
				item = BinFiles[map.BinNum].Entries[(int)map.Index];
				return true;
			}
			else
			{
				item = default;
				binNum = UInt32.MaxValue;
				return false;
			}
		}

		// Creates the absolute path to the debug content item
		public string GetDebugItemPath(string name) => Path.Combine(Directory, $"{name}{DEBUG_EXTENSION}");

		// If there is a content pack loaded at the path, return the cached instance, otherwise load and cache a new one
		public static ContentPack GetOrLoad(string path)
		{
			path = Path.GetFullPath(path);

			// Try to get it from the cache
			if (_PackCache.ContainsKey(path))
				return _PackCache[path];

			// Load a new one
			if (!File.Exists(path))
				throw new ContentException($"The content pack file '{path}' does not exist.");

			// Perform the load
			try
			{
				using (var reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None)))
				{
					var pack = new ContentPack(path, reader);
					_PackCache.Add(path, pack);
					return pack;
				}
			}
			catch (ContentException) { throw; }
			catch (Exception e)
			{
				throw new ContentException($"Unable to load the content pack file '{path}', reason: {e.Message}", e);
			}
		}
	}
}
