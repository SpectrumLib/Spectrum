/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using K4os.Compression.LZ4.Streams;

namespace Spectrum.Content
{
	public sealed class ContentStream : IDisposable
	{
		// The length of the headers (to add to the content offset to get to content data)
		internal const uint BIN_HEADER_LENGTH = 13;
		internal const uint DCI_HEADER_LENGTH = 16;

		// The UTF8 encoding and associated encoder used to get string and character bytes
		private static readonly Encoding _Encoding = new UTF8Encoding(
			encoderShouldEmitUTF8Identifier: false,
			throwOnInvalidBytes: true
		);

		#region Fields
		internal readonly string Item;
		internal readonly uint ItemStart; // The offset into the file that is the beginning of the data
		internal readonly uint RealSize; // The real length of the available amount of data, in bytes
		internal readonly uint UCSize; // The size of the uncompressed data
		internal readonly bool Compressed;
		internal readonly bool IsRelease;

		/// <summary>
		/// The absolute path to the file that this stream is reading from.
		/// </summary>
		public string FilePath => _file.Name;
		/// <summary>
		/// The current offset into the file.
		/// </summary>
		public uint CurrentOffset => (uint)_file.Position;

		// The streams
		private readonly FileStream _file;
		private LZ4DecoderStream _decompressor;
		internal BinaryReader Reader;
		private bool _ownsFile;

		// Faster way to keep track of where we are in the stream
		private uint _offset;

		/// <summary>
		/// Gets the number of bytes remaining in the stream to read.
		/// </summary>
		public uint Remaining => UCSize - _offset;

		private bool _isDisposed = false;
		#endregion // Fields

		private ContentStream(string item, FileStream fstream, uint rlen, uint uclen, uint? off)
		{
			Item = item;
			ItemStart = off.HasValue ? (off.Value + BIN_HEADER_LENGTH) : DCI_HEADER_LENGTH;
			RealSize = rlen;
			UCSize = uclen;
			Compressed = (rlen != uclen);
			IsRelease = off.HasValue;

			_file = fstream;
			_file.Seek(ItemStart, SeekOrigin.Begin);
			if (Compressed)
				_decompressor = LZ4Stream.Decode(_file, 0, leaveOpen: true);
			Reader = new BinaryReader(Compressed ? (Stream)_decompressor : (Stream)_file, _Encoding, true);
			_ownsFile = false;
			_offset = 0;
		}
		~ContentStream()
		{
			dispose(false);
		}

		internal static ContentStream FromBin(string item, FileStream fstream, uint rlen, uint uclen, uint off) =>
			new ContentStream(item, fstream, rlen, uclen, off);

		internal static ContentStream FromDci(string item, FileStream fstream, uint rlen, uint uclen) =>
			new ContentStream(item, fstream, rlen, uclen, null);

		/// <summary>
		/// Creates a copy of this stream, pointing to the same content item as the original stream, but at the
		/// beginning of the item.
		/// <para>
		/// Because ContentStreams are reused across multiple <see cref="ContentLoader{T}"/> instances, an instance
		/// cannot be saved and used again outside of the Load function. This function can be used to duplicate a
		/// content stream for use outside of the Load function, such as with streaming.
		/// </para>
		/// </summary>
		/// <returns>A duplicate of this content stream, which the calling code must dispose when finished with it.</returns>
		public ContentStream Duplicate()
		{
			var file = File.Open(_file.Name, FileMode.Open, FileAccess.Read, FileShare.Read);
			var stream = IsRelease ?
				FromBin(Item, file, RealSize, UCSize, ItemStart) :
				FromDci(Item, file, RealSize, UCSize);
			stream._ownsFile = true;
			return stream;
		}

		/// <summary>
		/// Moves the read position of the stream by the given offset. Seeking in compressed streams is slow in the
		/// current implementation, particularly backwards seeks.
		/// </summary>
		/// <param name="offset">The offset to move the read position by.</param>
		/// <param name="o">The origin from which to seek.</param>
		public void Seek(long offset, SeekOrigin o)
		{
			long newpos = o switch { 
				SeekOrigin.Begin => offset,
				SeekOrigin.End => UCSize - offset,
				SeekOrigin.Current => _offset + offset,
				_ => throw new ContentLoadException(Item, "invalid seek value.")
			};
			if (newpos < 0)
				throw new ContentLoadException(Item, "attempt to seek before start of data.");
			if (newpos > UCSize)
				throw new ContentLoadException(Item, "attempt to seek past end of data.");

			if (Compressed)
			{
				if (newpos >= _offset) // A simple skip-forward will suffice
				{
					long rem = newpos - _offset;
					while (rem-- > 0)
						_decompressor.ReadByte();
				}
				else // Need to recreate the streams and seek to desired position
				{
					Reader.Dispose();
					_decompressor.Dispose();
					_file.Seek(ItemStart, SeekOrigin.Begin);
					_decompressor = LZ4Stream.Decode(_file, 0, leaveOpen: true);
					Reader = new BinaryReader(Compressed ? (Stream)_decompressor : (Stream)_file, _Encoding, true);
					long rem = newpos;
					while (rem-- > 0)
						_decompressor.ReadByte();
				}
			}
			else
			{
				if (newpos >= _offset)
					_file.Seek(newpos - _offset, SeekOrigin.Current);
				else
					_file.Seek(_offset - newpos, SeekOrigin.Current);
			}

			_offset = (uint)newpos;
		}

		#region Read Functions
		/// <summary>
		/// Reads general typed data from the file into the passed buffer.
		/// </summary>
		/// <typeparam name="T">The data type to read.</typeparam>
		/// <param name="data">The buffer to read data into.</param>
		/// <returns>The actual number of bytes read from the file.</returns>
		public uint Read<T>(Span<T> data)
			where T : struct
		{
			var bytes = MemoryMarshal.AsBytes(data);
			var len = (uint)Math.Min(bytes.Length, Remaining);
			updateSize(len);
			return (uint)Reader.Read(bytes.Slice(0, (int)len));
		}

		/// <summary>
		/// Reads a boolean value from the stream and advances the stream by one byte.
		/// </summary>
		public bool ReadBoolean()
		{
			updateSize(1);
			return Reader.ReadBoolean();
		}

		/// <summary>
		/// Reads a sbyte value from the stream and advances the stream by one byte.
		/// </summary>
		public sbyte ReadSByte()
		{
			updateSize(1);
			return Reader.ReadSByte();
		}

		/// <summary>
		/// Reads a byte value from the stream and advances the stream by one byte.
		/// </summary>
		public byte ReadByte()
		{
			updateSize(1);
			return Reader.ReadByte();
		}

		/// <summary>
		/// Reads a short value from the stream and advances the stream by two bytes.
		/// </summary>
		public short ReadInt16()
		{
			updateSize(2);
			return Reader.ReadInt16();
		}

		/// <summary>
		/// Reads a ushort value from the stream and advances the stream by two bytes.
		/// </summary>
		public ushort ReadUInt16()
		{
			updateSize(2);
			return Reader.ReadUInt16();
		}

		/// <summary>
		/// Reads an int value from the stream and advances the stream by four bytes.
		/// </summary>
		public int ReadInt32()
		{
			updateSize(4);
			return Reader.ReadInt32();
		}

		/// <summary>
		/// Reads a uint value from the stream and advances the stream by four bytes.
		/// </summary>
		public uint ReadUInt32()
		{
			updateSize(4);
			return Reader.ReadUInt32();
		}

		/// <summary>
		/// Reads a long value from the stream and advances the stream by eight bytes.
		/// </summary>
		public long ReadInt64()
		{
			updateSize(8);
			return Reader.ReadInt64();
		}

		/// <summary>
		/// Reads a ulong value from the stream and advances the stream by eight bytes.
		/// </summary>
		public ulong ReadUInt64()
		{
			updateSize(8);
			return Reader.ReadUInt64();
		}

		/// <summary>
		/// Reads a float value from the stream and advances the stream by four bytes.
		/// </summary>
		public float ReadSingle()
		{
			updateSize(4);
			return Reader.ReadSingle();
		}

		/// <summary>
		/// Reads a double value from the stream and advances the stream by eight bytes.
		/// </summary>
		public double ReadDouble()
		{
			updateSize(8);
			return Reader.ReadDouble();
		}

		/// <summary>
		/// Reads a decimal value from the stream and advances the stream by sixteen bytes.
		/// </summary>
		public decimal ReadDecimal()
		{
			updateSize(16);
			return Reader.ReadDecimal();
		}

		/// <summary>
		/// Reads a number of bytes from the stream and advances the stream by the number of bytes read.
		/// </summary>
		/// <param name="count">The number of bytes to read.</param>
		public byte[] ReadBytes(uint count)
		{
			updateSize(count);
			return Reader.ReadBytes((int)count);
		}

		/// <summary>
		/// Reads a single character from the stream and advances the stream by the encoding size of the character.
		/// </summary>
		public unsafe char ReadChar()
		{
			if (Reader.PeekChar() == -1)
				throw new ContentLoadException(Item, "attempted to read past end of content stream.");
			var ch = Reader.ReadChar();
			var size = (uint)_Encoding.GetByteCount(&ch, 1);

			if (IsRelease)
				updateSize(size); // We may have read into the next item
			else
				_offset += size; // Dont need to check, as the debug items cant accidentally read into the next item

			return ch;
		}

		/// <summary>
		/// Reads a number of characters from the stream and advances the stream by the total encoding length of the
		/// characters.
		/// </summary>
		/// <param name="count">The number of characters to read.</param>
		// TODO: This is probably really inefficent for release content, find a faster way to do this
		public unsafe char[] ReadChars(uint count)
		{
			// This is a low order check against worst case, requesting more bytes than is available
			//   assuming all characters are only one byte long
			if (count > Remaining)
				throw new ContentLoadException(Item, "attempted to read past end of content stream.");

			if (IsRelease)
			{
				char[] chs = new char[count];
				for (uint ci = 0; ci < count; ++ci)
				{
					if (Reader.PeekChar() == -1)
						throw new ContentLoadException(Item, "attempted to read past end of content stream.");
					var ch = Reader.ReadChar();
					var size = (uint)_Encoding.GetByteCount(&ch, 1);
					updateSize(size);
					chs[ci] = ch;
				}
				return chs;
			}
			else
			{
				var chs = Reader.ReadChars((int)count);
				if ((uint)chs.Length != count)
					throw new ContentLoadException(Item, "attempted to read past end of content stream.");
				var size = (uint)_Encoding.GetByteCount(chs);
				_offset += size;
				return chs;
			}
		}

		/// <summary>
		/// Reads a length-prefixed string from the stream, and advances the stream by the number of bytes used to
		/// encode the string length and string data.
		/// </summary>
		public string ReadString()
		{
			try
			{
				string val = Reader.ReadString();
				var size = (uint)_Encoding.GetByteCount(val);

				if (IsRelease)
					updateSize(size);
				else
					_offset += size;

				return val;
			}
			catch (EndOfStreamException)
			{
				throw new ContentLoadException(Item, "attempted to read past end of content stream.");
			}
		}
		#endregion // Read Functions

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void updateSize(uint size)
		{
			if ((_offset + size) > UCSize)
				throw new ContentLoadException(Item, "attempted to read past end of content stream.");
			_offset += size;
		}

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!_isDisposed && disposing)
			{
				if (Compressed)
					_decompressor?.Dispose();
				Reader?.Dispose();
				if (_ownsFile)
					_file.Dispose();
			}
			_isDisposed = true;
		}
		#endregion // IDisposable
	}
}
