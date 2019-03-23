using System;
using System.IO;

namespace Prism
{
	// Implements the write functions for the stream
	public sealed partial class ContentStream
    {
		private const uint MEM_MB = 1_048_576;

		public void Write(bool val)
		{
			if ((_bufferPos + 1) > BUFFER_SIZE)
				flushInternal();
			_writer.Write(val);
		}

		public void Write(sbyte val)
		{
			if ((_bufferPos + 1) > BUFFER_SIZE)
				flushInternal();
			_writer.Write(val);
		}

		public void Write(byte val)
		{
			if ((_bufferPos + 1) > BUFFER_SIZE)
				flushInternal();
			_writer.Write(val);
		}

		public void Write(short val)
		{
			if ((_bufferPos + 2) > BUFFER_SIZE)
				flushInternal();
			_writer.Write(val);
		}

		public void Write(ushort val)
		{
			if ((_bufferPos + 2) > BUFFER_SIZE)
				flushInternal();
			_writer.Write(val);
		}

		public void Write(int val)
		{
			if ((_bufferPos + 4) > BUFFER_SIZE)
				flushInternal();
			_writer.Write(val);
		}

		public void Write(uint val)
		{
			if ((_bufferPos + 4) > BUFFER_SIZE)
				flushInternal();
			_writer.Write(val);
		}

		public void Write(long val)
		{
			if ((_bufferPos + 8) > BUFFER_SIZE)
				flushInternal();
			_writer.Write(val);
		}

		public void Write(ulong val)
		{
			if ((_bufferPos + 8) > BUFFER_SIZE)
				flushInternal();
			_writer.Write(val);
		}

		public void Write(float val)
		{
			if ((_bufferPos + 4) > BUFFER_SIZE)
				flushInternal();
			_writer.Write(val);
		}

		public void Write(double val)
		{
			if ((_bufferPos + 8) > BUFFER_SIZE)
				flushInternal();
			_writer.Write(val);
		}

		public void Write(decimal val)
		{
			if ((_bufferPos + 16) > BUFFER_SIZE)
				flushInternal();
			_writer.Write(val);
		}

		public unsafe void Write(byte[] data, uint start = 0, uint count = UInt32.MaxValue)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));
			if (start >= data.Length)
				throw new ArgumentOutOfRangeException(nameof(start), "The starting index for the array was bigger than the array");
			if (count == UInt32.MaxValue)
				count = (uint)data.Length - start;
			if ((start + count) > data.Length)
				throw new ArgumentOutOfRangeException(nameof(count), "The array is not large enough to supply the requested amount of data");

			bool direct = count >= DIRECT_WRITE_THRESHOLD;
			if (direct || (_bufferPos + count) > BUFFER_SIZE)
				flushInternal();

			// Write direct to file if over threshold
			if (direct)
			{
				fixed (byte* ptr = data)
				{
					flushDirect(ptr, count);
				}
			}
			else
			{
				fixed (byte *src = data, dst = _memBuffer)
				{
					Buffer.MemoryCopy(src, dst + _bufferPos, count, count);
					_memStream.Seek(count, SeekOrigin.Current);
				}
			}
		}

		// Maybe expose this later, but will probably change after Span<T> is available
		internal unsafe void Write(byte *data, uint count)
		{
			bool direct = count >= DIRECT_WRITE_THRESHOLD;
			if (direct || (_bufferPos + count) > BUFFER_SIZE)
				flushInternal();

			if (direct)
				flushDirect(data, count);
			else
			{
				fixed (byte *dst = _memBuffer)
				{
					Buffer.MemoryCopy(data, dst + _bufferPos, count, count);
					_memStream.Seek(count, SeekOrigin.Current);
				}
			}
		}

		public unsafe void Write(char val)
		{
			if (Char.IsSurrogate(val))
				throw new ArgumentException("A unicode surrogate is not allowed as a single character", nameof(val));

			var bytes = stackalloc byte[4];
			int numBytes = _encoder.GetBytes(&val, 1, bytes, 4, true);

			if ((_bufferPos + numBytes) > BUFFER_SIZE)
				flushInternal();

			fixed (byte *dst = _memBuffer)
			{
				Buffer.MemoryCopy(bytes, dst + _bufferPos, 4, numBytes);
				_memStream.Seek(numBytes, SeekOrigin.Current);
			}
		}

		// This function assumes (probably correctly) that nearly all character arrays will fit into the memory buffer on the first try, 
		//  and 99% of the rest will fit after the buffer is flushed. Only in cases where literally millions of characters are being
		//  written at once will we need to perform a direct flush to the file.
		public unsafe void Write(char[] chars, uint start = 0, uint count = UInt32.MaxValue)
		{
			if (chars == null)
				throw new ArgumentNullException(nameof(chars));
			if (start >= chars.Length)
				throw new ArgumentOutOfRangeException(nameof(start), "The starting index for the array was bigger than the array");
			if (count == UInt32.MaxValue)
				count = (uint)chars.Length - start;
			if ((start + count) > chars.Length)
				throw new ArgumentOutOfRangeException(nameof(count), "The array is not large enough to supply the requested amount of data");

			// Will almost always succeed
			uint savedOff = _bufferPos;
			try
			{
				_writer.Write(chars, (int)start, (int)count);
			}
			catch (NotSupportedException) // We went over, and need to flush the buffer and try again
			{
				_memStream.Seek(savedOff, SeekOrigin.Begin); // Restore old position, may be changed in error
				flushInternal();

				// Try again with the full buffer space
				try
				{
					_writer.Write(chars, (int)start, (int)count);
				}
				catch (NotSupportedException) // They are trying to write millions of characters at once, we are angry
				{
					flushDirect(chars, (int)start, (int)count);
				}
			}
		}

		// This function assumes (probably correctly) that nearly all strings will fit into the memory buffer on the first try, and
		//  99% of the rest will fit after the buffer is flushed. Only in cases where literally millions of characters are being
		//  written at once will we need to perform a direct flush to the file.
		public unsafe void Write(string val)
		{
			if (val == null)
				throw new ArgumentNullException(nameof(val));

			// Will almost always succeed
			uint savedOff = _bufferPos;
			try
			{
				_writer.Write(val);
			}
			catch (NotSupportedException) // We went over, and need to flush the buffer and try again
			{
				_memStream.Seek(savedOff, SeekOrigin.Begin); // Restore old position, may be changed in error
				flushInternal();

				// Try again with the full buffer space
				try
				{
					_writer.Write(val);
				}
				catch (NotSupportedException) // They are trying to write millions of characters at once, we are angry
				{
					flushDirect(val);
				}
			}
		}

		/// <summary>
		/// Copies data from a stream into this content stream. The stream must be readable, and provide proper support for
		/// the <see cref="Stream.Length"/> and <see cref="Stream.Position"/> fields.
		/// </summary>
		/// <param name="stream">The stream to copy data from.</param>
		/// <param name="count">
		/// The max amount of data to copy (in bytes), or <see cref="UInt32.MaxValue"/> to copy the rest of the stream.
		/// </param>
		public uint CopyFrom(Stream stream, uint count = UInt32.MaxValue)
		{
			if (!stream.CanRead)
				throw new InvalidOperationException("Cannot copy from a stream that does not support reading.");
			uint srem = (uint)(stream.Length - stream.Position);
			if (count > srem)
				count = srem;

			bool direct = count >= DIRECT_WRITE_THRESHOLD;
			if (direct || (_bufferPos + count) > BUFFER_SIZE)
				flushInternal();

			if (direct)
			{
				if (count == srem)
					stream.CopyTo(_file);
				else
				{
					// Use the memory buffer as the temp buffer
					while (count > 0)
					{
						var amt = stream.Read(_memBuffer, 0, (int)MEM_MB);
						_file.Write(_memBuffer, 0, amt);
						count -= (uint)amt;
					}
				}
				_file.Flush();
			}
			else
			{
				if (count == srem)
					stream.CopyTo(_memStream);
				else
				{
					stream.Read(_memBuffer, (int)_bufferPos, (int)count);
					_memStream.Seek(count, SeekOrigin.Current);
				}
			}

			return count;
		}

		/// <summary>
		/// Copies the entire contents of another file into this stream. The file must exist and be readable.
		/// </summary>
		/// <param name="file">The path to the file to copy from.</param>
		/// <param name="writeLen">
		/// If <c>true</c>, the length of the file will be written to the stream before the contents of the source stream,
		/// as a unsigned 4-byte integer.
		/// </param>
		/// <returns>The number of bytes copied from the file. This value includes the 4 bytes if `writeLen` is true.</returns>
		public uint CopyFrom(string file, bool writeLen = false)
		{
			if (String.IsNullOrEmpty(file))
				throw new ArgumentException("The path cannot be null or empty.", nameof(file));
			if (!PathUtils.TryGetFullPath(file, out var path, Path.GetDirectoryName(_currentFile)))
				throw new ArgumentException($"The path '{file}' is not a valid filesystem path.", nameof(file));

			FileInfo fi = new FileInfo(path);
			if (!fi.Exists)
				throw new FileNotFoundException($"The path '{path}' does not point to a file that exists.", path);

			using (var reader = fi.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				if (writeLen)
					Write((uint)fi.Length);
				return CopyFrom(reader, (uint)fi.Length) + (writeLen ? 4u : 0);
			}
		}
	}
}
