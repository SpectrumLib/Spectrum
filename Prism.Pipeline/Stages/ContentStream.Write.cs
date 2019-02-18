using System;
using System.IO;

namespace Prism
{
	// Implements the write functions for the stream
	public sealed partial class ContentStream
    {
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
	}
}
