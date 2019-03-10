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
	public sealed class ContentStream : IDisposable
	{
		// The length of the headers (to add to the content offset to get to content data)
		internal const uint BIN_HEADER_LENGTH = 13;
		internal const uint DCI_HEADER_LENGTH = 16;

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
		private readonly LZ4DecoderStream _decompressor;
		internal readonly BinaryReader Reader;
		private bool _ownsFile;

		// Faster way to keep track of where we are in the stream
		private uint _pos;

		/// <summary>
		/// Gets the number of bytes remaining in the stream to read.
		/// </summary>
		public uint Remaining => UCSize - _pos;

		private bool _isDisposed = false;
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
			Reader = new BinaryReader(Compressed ? (Stream)_decompressor : (Stream)_file, s_encoding, true);
			_ownsFile = false;
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
			Reader = new BinaryReader(Compressed ? (Stream)_decompressor : (Stream)_file, s_encoding, true);
			_ownsFile = false;
			_pos = 0;
		}

		~ContentStream()
		{
			dispose(false);
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
				if ((_pos + offset) > UCSize)
					throw new InvalidOperationException("Cannot seek past the end of the content file data.");
				while (offset-- > 0)
					_decompressor.ReadByte();
				_pos = (uint)(_pos + offset);
			}
			else
			{
				if (o == SeekOrigin.Begin)
				{
					if (offset < 0)
						throw new InvalidOperationException("Cannot seek before the beginning of the content file data.");
					if (offset > UCSize)
						throw new InvalidOperationException("Cannot seek past the end of the content file data.");
					Reader.BaseStream.Seek(offset + Offset, SeekOrigin.Begin);
					_pos = (uint)offset;
				}
				else if (o == SeekOrigin.End)
				{
					if (offset > 0)
						throw new InvalidOperationException("Cannot seek past the end of the content file data.");
					if (offset < -UCSize)
						throw new InvalidOperationException("Cannot seek before the beginning of the content file data.");
					Reader.BaseStream.Seek(Offset + UCSize + offset, SeekOrigin.Begin);
					_pos = (uint)(UCSize + offset);
				}
				else
				{
					if ((_pos + offset) > UCSize)
						throw new InvalidOperationException("Cannot seek past the end of the content file data.");
					if ((_pos + offset) < 0)
						throw new InvalidOperationException("Cannot seek before the beginning of the content file data.");
					Reader.BaseStream.Seek(offset, SeekOrigin.Current);
					_pos = (uint)(_pos + offset);
				}
			}
		}

		/// <summary>
		/// Creates a copy of this stream, pointing to the same content item as the original stream, but at the
		/// beginning of the item.
		/// <para>
		/// Because ContentStreams are reused across multiple <see cref="ContentLoader{T}"/> instances, an instance
		/// cannot be saved and used again outside of the Load function. This function can be used to duplicate a
		/// content stream for use outside of the Load function, such as with streaming.
		/// </para>
		/// </summary>
		/// <returns>A duplicate of this content stream, which the user must dispose when finished with it.</returns>
		public ContentStream Duplicate()
		{
			var file = File.Open(_file.Name, FileMode.Open, FileAccess.Read, FileShare.Read);
			var stream = IsRelease ?
				new ContentStream(Item, file, Offset - BIN_HEADER_LENGTH, RealSize, UCSize) :
				new ContentStream(Item, file, RealSize, UCSize);
			stream._ownsFile = true;
			return stream;
		}

		#region Read Functions
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
		public char ReadChar()
		{
			var oldPos = (uint)Reader.BaseStream.Position;
			if (Reader.PeekChar() == -1)
				throw new ContentLoadException(Item, "attempted to read past end of content stream.");
			var ch = Reader.ReadChar();
			var size = (uint)Reader.BaseStream.Position - oldPos;

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
					var oldPos = (uint)Reader.BaseStream.Position;
					if (Reader.PeekChar() == -1)
						throw new ContentLoadException(Item, "attempted to read past end of content stream.");
					var ch = Reader.ReadChar();
					var size = (uint)Reader.BaseStream.Position - oldPos;
					updateSize(size);
					chs[ci] = ch;
				}
				return chs;
			}
			else
			{
				var oldPos = (uint)Reader.BaseStream.Position;
				var chs = Reader.ReadChars((int)count);
				if ((uint)chs.Length != count)
					throw new ContentLoadException(Item, "attempted to read past end of content stream.");
				var size = (uint)Reader.BaseStream.Position - oldPos;
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
				var oldPos = (uint)Reader.BaseStream.Position;
				string val = Reader.ReadString();
				var size = (uint)Reader.BaseStream.Position - oldPos;

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
