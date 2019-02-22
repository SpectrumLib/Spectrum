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

		private readonly List<BinEntry> _items;
		public IReadOnlyList<BinEntry> Items => _items;
		#endregion // Fields

		private BinFile(uint fnum, string path, List<BinEntry> items)
		{
			FileNumber = fnum;
			FilePath = path;
			_items = items;
		}

		// Opens a raw read-only stream to the bin file. Multiple streams can be opened to the same file with this
		//   function, as they are read-only.
		public FileStream OpenStream() => File.Open(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

		// Any exceptions thrown from here are caught and reported (format messages appropriately)
		public static BinFile LoadFromStream(uint num, string root, uint timestamp, BinaryReader reader)
		{
			// Ensure existence
			var binPath = Path.GetFullPath(Path.Combine(root, $"{num}{FILE_EXTENSION}"));
			if (!File.Exists(binPath))
				throw new Exception($"the bin file '{binPath}' does not exist.");

			// Parse the item info for the file
			uint iCount = reader.ReadUInt32();
			List<BinEntry> items = new List<BinEntry>((int)iCount);
			for (uint i = 0; i < iCount; ++i)
				items.Add(new BinEntry(reader));

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

	// Holds information about a file entry in a .cbin, read from the .cpak file
	internal class BinEntry
	{
		#region Fields
		public readonly string Name;
		public readonly uint RealSize;
		public readonly uint UCSize;
		public readonly uint Offset;
		public readonly uint LoaderHash;
		public bool Compressed => RealSize != UCSize;
		#endregion // Fields

		public BinEntry(BinaryReader reader)
		{
			Name = reader.ReadString();
			RealSize = reader.ReadUInt32();
			UCSize = reader.ReadUInt32();
			Offset = reader.ReadUInt32();
			LoaderHash = reader.ReadUInt32();
		}
	}
}
