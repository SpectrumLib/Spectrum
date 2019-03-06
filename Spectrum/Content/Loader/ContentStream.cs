using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using K4os.Compression.LZ4.Streams;

namespace Spectrum.Content
{
	/// <summary>
	/// Used to read in data from a content item file, hiding the details such as packing or compression.
	/// </summary>
	public sealed class ContentStream
	{
		// The length of the headers (to add to the content offset to get to content data)
		private const uint BIN_HEADER_LENGTH = 13;
		private const uint DCI_HEADER_LENGTH = 16;

		// The UTF8 encoding and associated encoder used to get string and character bytes
		private static readonly Encoding s_encoding = new UTF8Encoding(
			encoderShouldEmitUTF8Identifier: false,
			throwOnInvalidBytes: true
		);

		#region Fields
		internal readonly string Item;
		internal readonly uint Offset; // The offset into the file that is the beginning of the data
		internal readonly uint RealSize; // The real length of the available amount of data, in bytes
		internal readonly uint UCSize; // The size of the uncompressed data
		internal readonly bool Compressed;
		internal readonly bool IsRelease;

		// The streams
		private readonly FileStream _file;
		private readonly LZ4DecoderStream _decompressor;
		private readonly BinaryReader _reader;

		// Faster way to keep track of where we are in the stream
		private uint _pos;

		/// <summary>
		/// Gets the number of bytes remaining in the stream to read.
		/// </summary>
		public uint Remaining => UCSize - _pos;
		#endregion // Fields

		// Creates a content stream to read from a portion of a bin file
		internal ContentStream(string item, FileStream fstream, uint off, uint rlen, uint uclen)
		{
			Item = item;
			Offset = off + BIN_HEADER_LENGTH;
			RealSize = rlen;
			UCSize = uclen;
			Compressed = (rlen != uclen);
			IsRelease = true;

			_file = fstream;
			_file.Seek(Offset, SeekOrigin.Begin);
			if (Compressed)
				_decompressor = LZ4Stream.Decode(_file, 0, leaveOpen: true);
			_reader = new BinaryReader(Compressed ? (Stream)_decompressor : (Stream)_file, s_encoding, true);
			_pos = 0;
		}

		// Creates a content stream to read from a debug item
		internal ContentStream(string item, FileStream fstream, uint rlen, uint uclen)
		{
			Item = item;
			Offset = DCI_HEADER_LENGTH;
			RealSize = rlen;
			UCSize = uclen;
			Compressed = (rlen != uclen);
			IsRelease = false;

			_file = fstream;
			_file.Seek(Offset, SeekOrigin.Begin); // Probably already there, but should make sure
			if (Compressed)
				_decompressor = LZ4Stream.Decode(_file, 0, leaveOpen: true);
			_reader = new BinaryReader(Compressed ? (Stream)_decompressor : (Stream)_file, s_encoding, true);
			_pos = 0;
		}

		// Should be called for proper cleanup, but its not the end of the world as it does not hold the file stream open
		internal void Free()
		{
			if (Compressed)
				_decompressor?.Dispose();
			_reader?.Dispose();
		}

		/// <summary>
		/// Moves the read position of the stream by the given offset. Seeking a compressed stream is unsupported, outside
		/// of a forward seek from the current position (which is just a simple skip).
		/// </summary>
		/// <param name="offset">The offset to move the read position by.</param>
		/// <param name="o">The origin from which to seek.</param>
		public void Seek(long offset, SeekOrigin o)
		{
			if (Compressed)
			{
				if (o != SeekOrigin.Current || offset < 0)
					throw new InvalidOperationException("Cannot seek a compressed ContentStream other than to skip forward from the current position.");
				while (offset-- > 0)
					_decompressor.ReadByte();
			}
			else
				_reader.BaseStream.Seek(offset, o);
		}

		#region Read Functions
		/// <summary>
		/// Reads a boolean value from the stream and advances the stream by one byte.
		/// </summary>
		public bool ReadBoolean()
		{
			updateSize(1);
			return _reader.ReadBoolean();
		}

		/// <summary>
		/// Reads a sbyte value from the stream and advances the stream by one byte.
		/// </summary>
		public sbyte ReadSByte()
		{
			updateSize(1);
			return _reader.ReadSByte();
		}

		/// <summary>
		/// Reads a byte value from the stream and advances the stream by one byte.
		/// </summary>
		public byte ReadByte()
		{
			updateSize(1);
			return _reader.ReadByte();
		}

		/// <summary>
		/// Reads a short value from the stream and advances the stream by two bytes.
		/// </summary>
		public short ReadInt16()
		{
			updateSize(2);
			return _reader.ReadInt16();
		}

		/// <summary>
		/// Reads a ushort value from the stream and advances the stream by two bytes.
		/// </summary>
		public ushort ReadUInt16()
		{
			updateSize(2);
			return _reader.ReadUInt16();
		}

		/// <summary>
		/// Reads an int value from the stream and advances the stream by four bytes.
		/// </summary>
		public int ReadInt32()
		{
			updateSize(4);
			return _reader.ReadInt32();
		}

		/// <summary>
		/// Reads a uint value from the stream and advances the stream by four bytes.
		/// </summary>
		public uint ReadUInt32()
		{
			updateSize(4);
			return _reader.ReadUInt32();
		}

		/// <summary>
		/// Reads a long value from the stream and advances the stream by eight bytes.
		/// </summary>
		public long ReadInt64()
		{
			updateSize(8);
			return _reader.ReadInt64();
		}

		/// <summary>
		/// Reads a ulong value from the stream and advances the stream by eight bytes.
		/// </summary>
		public ulong ReadUInt64()
		{
			updateSize(8);
			return _reader.ReadUInt64();
		}

		/// <summary>
		/// Reads a float value from the stream and advances the stream by four bytes.
		/// </summary>
		public float ReadSingle()
		{
			updateSize(4);
			return _reader.ReadSingle();
		}

		/// <summary>
		/// Reads a double value from the stream and advances the stream by eight bytes.
		/// </summary>
		public double ReadDouble()
		{
			updateSize(8);
			return _reader.ReadDouble();
		}

		/// <summary>
		/// Reads a decimal value from the stream and advances the stream by sixteen bytes.
		/// </summary>
		public decimal ReadDecimal()
		{
			updateSize(16);
			return _reader.ReadDecimal();
		}

		/// <summary>
		/// Reads a number of bytes from the stream and advances the stream by the number of bytes read.
		/// </summary>
		/// <param name="count">The number of bytes to read.</param>
		public byte[] ReadBytes(uint count)
		{
			updateSize(count);
			return _reader.ReadBytes((int)count);
		}

		/// <summary>
		/// Reads a single character from the stream and advances the stream by the encoding size of the character.
		/// </summary>
		public char ReadChar()
		{
			var oldPos = (uint)_reader.BaseStream.Position;
			if (_reader.PeekChar() == -1)
				throw new ContentLoadException(Item, "attempted to read past end of content stream.");
			var ch = _reader.ReadChar();
			var size = (uint)_reader.BaseStream.Position - oldPos;

			if (IsRelease)
				updateSize(size); // We may have read into the next item
			else
				_pos += size; // Dont need to check, as the debug items cant accidentally read into the next item

			return ch;
		}

		/// <summary>
		/// Reads a number of characters from the stream and advances the stream by the total encoding length of the
		/// characters.
		/// </summary>
		/// <param name="count">The number of characters to read.</param>
		// TODO: This is probably really inefficent for release content, find a faster way to do this
		public char[] ReadChars(uint count)
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
					var oldPos = (uint)_reader.BaseStream.Position;
					if (_reader.PeekChar() == -1)
						throw new ContentLoadException(Item, "attempted to read past end of content stream.");
					var ch = _reader.ReadChar();
					var size = (uint)_reader.BaseStream.Position - oldPos;
					updateSize(size);
					chs[ci] = ch;
				}
				return chs;
			}
			else
			{
				var oldPos = (uint)_reader.BaseStream.Position;
				var chs = _reader.ReadChars((int)count);
				if ((uint)chs.Length != count)
					throw new ContentLoadException(Item, "attempted to read past end of content stream.");
				var size = (uint)_reader.BaseStream.Position - oldPos;
				_pos += size;
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
				var oldPos = (uint)_reader.BaseStream.Position;
				string val = _reader.ReadString();
				var size = (uint)_reader.BaseStream.Position - oldPos;

				if (IsRelease)
					updateSize(size);
				else
					_pos += size;

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
			if ((_pos + size) > UCSize)
				throw new ContentLoadException(Item, "attempted to read past end of content stream.");
			_pos += size;
		}
	}
}
