using System;
using System.Collections.Generic;
using System.IO;

namespace Spectrum.Content
{
	// Holds information about the contents of a .cbin file for release build content
	internal class BinFile
	{
		public static readonly string FILE_EXTENSION = ".cbin";

		#region Fields
		public readonly uint FileNumber;
		public readonly string FilePath;

		private readonly List<(string, uint, uint, uint)> _items;
		public IReadOnlyList<(string Name, uint Size, uint Offset, uint LoaderHash)> Items => _items;
		#endregion // Fields

		private BinFile(uint fnum, string path, List<(string, uint, uint, uint)> items)
		{
			FileNumber = fnum;
			FilePath = path;
			_items = items;
		}

		// Any exceptions thrown from here are caught and reported (format messages appropriately)
		public static BinFile LoadFromStream(uint num, string root, uint timestamp, BinaryReader reader)
		{
			// Ensure existence
			var binPath = Path.GetFullPath(Path.Combine(root, $"{num}{FILE_EXTENSION}"));
			if (!File.Exists(binPath))
				throw new Exception($"the bin file '{binPath}' does not exist.");

			// Parse the item info for the file
			uint iCount = reader.ReadUInt32();
			List<(string, uint, uint, uint)> items = new List<(string, uint, uint, uint)>((int)iCount);
			for (uint i = 0; i < iCount; ++i)
			{
				var iname = reader.ReadString();
				var isize = reader.ReadUInt32();
				var ioffset = reader.ReadUInt32();
				var ihash = reader.ReadUInt32();
				items.Add((iname, isize, ioffset, ihash));
			}

			// Load in the header of the bin file
			using (var binReader = new BinaryReader(File.Open(binPath, FileMode.Open, FileAccess.Read, FileShare.None)))
			{
				var header = binReader.ReadBytes(5);
				if (header[0] != 'C' || header[1] != 'B' || header[2] != 'I' || header[3] != 'N' || header[4] != 1)
					throw new Exception($"invalid header for bin file '{binPath}'.");
				var count = binReader.ReadUInt32();
				if (count != iCount)
					throw new Exception($"item count mismatch for bin file '{binPath}'.");
				var tstamp = binReader.ReadUInt32();
				if (tstamp != timestamp)
					throw new Exception($"timestamp mismatch for bin file '{binPath}'.");
			}

			// Create the bin file
			return new BinFile(num, binPath, items);
		}
	}
}
