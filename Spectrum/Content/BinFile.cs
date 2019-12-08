/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.IO;

namespace Spectrum.Content
{
    // Holds information about the contents of a .cbin content file
	internal class BinFile
	{
        public static readonly string EXTENSION = ".cbin";

        #region Fields
        public readonly uint FileNumber;
        public readonly string FilePath;
        public readonly BinEntry[] Entries;
		#endregion // Fields

        private BinFile(uint num, string path, BinEntry[] ents)
        {
            FileNumber = num;
            FilePath = path;
            Entries = ents;
        }

        // Opens a raw read-only stream to the file, multiple streams are allowed at once
        public FileStream OpenStream() => File.Open(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        // Any exceptions thrown from here are caught and reported (format messages appropriately)
        public unsafe static BinFile LoadFromStream(uint num, string root, uint timestamp, BinaryReader reader)
        {
            // Ensure existence
            var binPath = Path.GetFullPath(Path.Combine(root, $"{num}{EXTENSION}"));
            if (!File.Exists(binPath))
                throw new ContentException($"the bin file '{binPath}' does not exist.");

            // Parse the item info for the file
            uint iCount = reader.ReadUInt32();
            BinEntry[] items = new BinEntry[iCount];
            for (uint i = 0; i < iCount; ++i)
                items[i] = new BinEntry(reader);

            // Load in the header of the bin file
            using (var binReader = new BinaryReader(File.Open(binPath, FileMode.Open, FileAccess.Read, FileShare.None)))
            {
                // Check header
                Span<byte> header = stackalloc byte[5];
                binReader.Read(header);
                if (header[0] != 'C' || header[1] != 'B' || header[2] != 'I' || header[3] != 'N')
                    throw new ContentException($"invalid header for bin file '{binPath}'.");
                if (header[4] != 1)
                    throw new ContentException($"invalid version for bin file '{binPath}'.");

                // Check for validity against known values
                var count = binReader.ReadUInt32();
                if (count != iCount)
                    throw new ContentException($"item count mismatch for bin file '{binPath}'.");
                var tstamp = binReader.ReadUInt32();
                if (tstamp != timestamp)
                    throw new ContentException($"timestamp mismatch for bin file '{binPath}'.");
            }

            // Create the bin file
            return new BinFile(num, binPath, items);
        }
    }

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
