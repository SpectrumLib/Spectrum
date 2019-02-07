using System;

namespace Prism
{
	// To remain compatible with the BinaryReader class, all Write() functionality should be taken from the CoreCLR
	//   BinaryWriter class at:
	//   https://github.com/dotnet/coreclr/blob/master/src/System.Private.CoreLib/shared/System/IO/BinaryWriter.cs
	public sealed partial class ContentStream
    {
		public void Write(bool val)
		{
			if ((_bufferPos + 1) > BUFFER_SIZE)
				flushInternal();
			_memBuffer[_bufferPos++] = (byte)(val ? 1 : 0);
		}

		public void Write(sbyte val)
		{
			if ((_bufferPos + 1) > BUFFER_SIZE)
				flushInternal();
			_memBuffer[_bufferPos++] = (byte)val;
		}

		public void Write(byte val)
		{
			if ((_bufferPos + 1) > BUFFER_SIZE)
				flushInternal();
			_memBuffer[_bufferPos++] = val;
		}

		public void Write(short val)
		{
			if ((_bufferPos + 2) > BUFFER_SIZE)
				flushInternal();
			_memBuffer[_bufferPos++] = (byte)val;
			_memBuffer[_bufferPos++] = (byte)(val >> 8);
		}

		public void Write(ushort val)
		{
			if ((_bufferPos + 2) > BUFFER_SIZE)
				flushInternal();
			_memBuffer[_bufferPos++] = (byte)val;
			_memBuffer[_bufferPos++] = (byte)(val >> 8);
		}

		public void Write(int val)
		{
			if ((_bufferPos + 4) > BUFFER_SIZE)
				flushInternal();
			_memBuffer[_bufferPos++] = (byte)val;
			_memBuffer[_bufferPos++] = (byte)(val >> 8);
			_memBuffer[_bufferPos++] = (byte)(val >> 16);
			_memBuffer[_bufferPos++] = (byte)(val >> 24);
		}

		public void Write(uint val)
		{
			if ((_bufferPos + 4) > BUFFER_SIZE)
				flushInternal();
			_memBuffer[_bufferPos++] = (byte)val;
			_memBuffer[_bufferPos++] = (byte)(val >> 8);
			_memBuffer[_bufferPos++] = (byte)(val >> 16);
			_memBuffer[_bufferPos++] = (byte)(val >> 24);
		}

		public void Write(long val)
		{
			if ((_bufferPos + 8) > BUFFER_SIZE)
				flushInternal();
			_memBuffer[_bufferPos++] = (byte)val;
			_memBuffer[_bufferPos++] = (byte)(val >> 8);
			_memBuffer[_bufferPos++] = (byte)(val >> 16);
			_memBuffer[_bufferPos++] = (byte)(val >> 24);
			_memBuffer[_bufferPos++] = (byte)(val >> 32);
			_memBuffer[_bufferPos++] = (byte)(val >> 40);
			_memBuffer[_bufferPos++] = (byte)(val >> 48);
			_memBuffer[_bufferPos++] = (byte)(val >> 56);
		}

		public void Write(ulong val)
		{
			if ((_bufferPos + 8) > BUFFER_SIZE)
				flushInternal();
			_memBuffer[_bufferPos++] = (byte)val;
			_memBuffer[_bufferPos++] = (byte)(val >> 8);
			_memBuffer[_bufferPos++] = (byte)(val >> 16);
			_memBuffer[_bufferPos++] = (byte)(val >> 24);
			_memBuffer[_bufferPos++] = (byte)(val >> 32);
			_memBuffer[_bufferPos++] = (byte)(val >> 40);
			_memBuffer[_bufferPos++] = (byte)(val >> 48);
			_memBuffer[_bufferPos++] = (byte)(val >> 56);
		}

		public unsafe void Write(float val)
		{
			if ((_bufferPos + 4) > BUFFER_SIZE)
				flushInternal();
			uint tmpval = *(uint*)&val;
			_memBuffer[_bufferPos++] = (byte)tmpval;
			_memBuffer[_bufferPos++] = (byte)(tmpval >> 8);
			_memBuffer[_bufferPos++] = (byte)(tmpval >> 16);
			_memBuffer[_bufferPos++] = (byte)(tmpval >> 24);
		}

		public unsafe void Write(double val)
		{
			if ((_bufferPos + 8) > BUFFER_SIZE)
				flushInternal();
			ulong tmpval = *(ulong*)&val;
			_memBuffer[_bufferPos++] = (byte)tmpval;
			_memBuffer[_bufferPos++] = (byte)(tmpval >> 8);
			_memBuffer[_bufferPos++] = (byte)(tmpval >> 16);
			_memBuffer[_bufferPos++] = (byte)(tmpval >> 24);
			_memBuffer[_bufferPos++] = (byte)(tmpval >> 32);
			_memBuffer[_bufferPos++] = (byte)(tmpval >> 40);
			_memBuffer[_bufferPos++] = (byte)(tmpval >> 48);
			_memBuffer[_bufferPos++] = (byte)(tmpval >> 56);
		}

		public unsafe void Write(decimal val)
		{
			if ((_bufferPos + 16) > BUFFER_SIZE)
				flushInternal();

			int* bits = (int*)&val;
			_memBuffer[_bufferPos++] = (byte)bits[0];
			_memBuffer[_bufferPos++] = (byte)(bits[0] >> 8);
			_memBuffer[_bufferPos++] = (byte)(bits[0] >> 16);
			_memBuffer[_bufferPos++] = (byte)(bits[0] >> 24);
			_memBuffer[_bufferPos++] = (byte)bits[1];
			_memBuffer[_bufferPos++] = (byte)(bits[1] >> 8);
			_memBuffer[_bufferPos++] = (byte)(bits[1] >> 16);
			_memBuffer[_bufferPos++] = (byte)(bits[1] >> 24);
			_memBuffer[_bufferPos++] = (byte)bits[2];
			_memBuffer[_bufferPos++] = (byte)(bits[2] >> 8);
			_memBuffer[_bufferPos++] = (byte)(bits[2] >> 16);
			_memBuffer[_bufferPos++] = (byte)(bits[2] >> 24);
			_memBuffer[_bufferPos++] = (byte)bits[3];
			_memBuffer[_bufferPos++] = (byte)(bits[3] >> 8);
			_memBuffer[_bufferPos++] = (byte)(bits[3] >> 16);
			_memBuffer[_bufferPos++] = (byte)(bits[3] >> 24);
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
					Buffer.MemoryCopy(src, dst, count, count);
					_bufferPos += count;
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
				Buffer.MemoryCopy(bytes, dst, 4, numBytes);
				_bufferPos += (uint)numBytes;
			}
		}

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

			var bytes = stackalloc byte[chars.Length * 4]; // Assumed worst case scenario, but is time-efficient and not the end of the world
			int numBytes;
			fixed (char *src = chars)
			{
				numBytes = _encoder.GetBytes(src, (int)count, bytes, chars.Length * 4, true);
			}

			bool direct = numBytes >= DIRECT_WRITE_THRESHOLD;
			if (direct || (_bufferPos + numBytes) > BUFFER_SIZE)
				flushInternal();

			// Write direct to file if over threshold
			if (direct)
				flushDirect(bytes, (uint)numBytes);
			else
			{
				fixed (byte* dst = _memBuffer)
				{
					Buffer.MemoryCopy(bytes, dst, numBytes, numBytes);
					_bufferPos += (uint)numBytes;
				}
			}
		}

		public unsafe void Write(string val)
		{
			if (val == null)
				throw new ArgumentNullException(nameof(val));
			uint numBytes = (uint)_encoding.GetByteCount(val);
			// Needs to fit entirely into the available buffer (gross, probably fix this later)
			//  Still, this equates to ~8mil characters in the worst case (4-byte code points), and you should rethink what you are doing
			//  if you have a string that is this long (there are only a few published books in history longer than this)
			if (numBytes > (BUFFER_SIZE - 4)) 
				throw new ArgumentException("Cannot write a string whose bytes cannot fit in 32MB", nameof(val));

			if ((_bufferPos + numBytes + 4) > BUFFER_SIZE) // +4 for the length encoding
				flushInternal();

			// Write the encoded length
			{
				uint tmpNum = numBytes;
				while (tmpNum >= 0x80)
				{
					_memBuffer[_bufferPos++] = (byte)(tmpNum | 0x80);
					tmpNum >>= 7;
				}
				_memBuffer[_bufferPos++] = (byte)tmpNum;
			}

			// Write the bytes directly into the buffer
			fixed (char* src = val)
			fixed (byte* dst = _memBuffer)
			{
				_encoding.GetBytes(src, val.Length, dst + _bufferPos, (int)numBytes);
				_bufferPos += numBytes;
			}
		}
	}
}
