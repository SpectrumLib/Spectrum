using System;
using System.Runtime.CompilerServices;
using Spectrum.Content;

namespace Spectrum.Audio
{
	// Contains the logic required to decode and stream RLAD-encoded audio data from a file
	internal class RLADStream : IDisposable
	{
		private const uint MAX_BUFFER_SIZE = 512; // Any RLAD chunk can have a maximum of 512 (8 * 64) frames
		public const int SMALL_TYPE = 0;
		public const int MED_TYPE = 1;
		public const int FULL_TYPE = 2;

		#region Fields
		// Audio info
		public readonly bool Stereo;
		public readonly uint FrameCount;

		// Stream info
		private readonly ContentStream _stream;
		private uint _frameOffset;
		public uint RemainingFrames => FrameCount - _frameOffset;
		private Chunk _currChunk; // The current chunk that the stream is ready to read

		// Sample buffer
		private short[] _buffer;
		private uint _bufferOff; // The current offset into the buffer for the next sample
		private uint _bufferSize; // The remaining samples in the buffer
		private uint _bufferRem => _bufferSize - _bufferOff;

		private bool _isDisposed = false;
		#endregion // Fields

		public RLADStream(ContentStream stream, bool stereo, uint fc)
		{
			Stereo = stereo;
			FrameCount = fc;

			_stream = stream;
			_frameOffset = 0;

			_buffer = new short[MAX_BUFFER_SIZE * (stereo ? 2 : 1)];
			_bufferOff = 0;
			_bufferSize = 0;

			// Get first chunk
			ReadChunkHeader(stream, out _currChunk);
		}
		~RLADStream()
		{
			dispose(false);
		}

		// Read a number of samples into the passed array
		public unsafe uint ReadFrames(short[] dst, uint count)
		{
			if (count > RemainingFrames)
				count = RemainingFrames;
			if (count > dst.Length)
				throw new ArgumentException($"The array is not large enough to receive the requested number of samples.");

			// Tracking read info
			uint rem = count;
			uint dstOff = 0;

			// Read the rest of the buffered samples, if there are any
			if (_bufferSize > 0)
			{
				fixed (short* srcptr = _buffer, dstptr = dst)
				{
					Buffer.MemoryCopy(srcptr + _bufferOff, dstptr, _bufferRem * 2, _bufferRem * 2);
				}
				dstOff += _bufferRem;
				rem -= (_bufferRem / (Stereo ? 2u : 1));
				_bufferSize = 0;
				_bufferOff = 0;
			}

			// Perform as many direct writes into the dst array as possible
			fixed (short* dstptr = dst)
			{
				while (_currChunk.FrameCount <= rem)
				{
					DecodeChunk(_stream, dstptr + dstOff, ref _currChunk, Stereo);
					dstOff += (_currChunk.FrameCount * (Stereo ? 2u : 1));
					rem -= _currChunk.FrameCount;
					ReadChunkHeader(_stream, out _currChunk);
				}
			}

			// If needed, buffer the next chunk, and copy as much into the dst array as needed
			if (rem > 0)
			{
				// Buffer the next chunk
				fixed (short* buff = _buffer)
				{
					DecodeChunk(_stream, buff, ref _currChunk, Stereo);
					_bufferSize = (_currChunk.FrameCount * (Stereo ? 2u : 1));
				}

				// Copy as much as is needed into the dst array
				fixed (short* srcptr = _buffer, dstptr = dst)
				{
					uint toCopy = rem * (Stereo ? 2u : 1);
					Buffer.MemoryCopy(srcptr, dstptr + dstOff, toCopy * 2, toCopy * 2);
					_bufferOff = toCopy;
				}

				// Read the chunk for the next call
				ReadChunkHeader(_stream, out _currChunk);
			}

			// Return the number of frames actually read
			return count;
		}

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing) _stream.Dispose();
				_buffer = null;
			}
			_isDisposed = true;
		}
		#endregion // IDisposable

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ReadChunkHeader(ContentStream stream, out Chunk c)
		{
			byte header = stream.ReadByte();
			c.Type = (ushort)((header >> 6) & 0x03);
			c.Extra = (ushort)(header & 0x3F);
			c.Channel1 = 0;
			c.Channel2 = 0;
		}

		// Dst must be large enough to accept up to 512 samples (1024 for stereo)
		private static unsafe void DecodeChunk(ContentStream stream, short* dst, ref Chunk c, bool stereo)
		{
			uint rem = c.FrameCount * (stereo ? 2u : 1);

			if (c.Type == SMALL_TYPE)
			{
				// Local accumulators
				short acc1 = c.Channel1, acc2 = c.Channel2;

				// Read in 8 deltas at a time, and accumulate them
				for (uint si = 0; si < rem; si += 8)
				{
					if (stereo)
					{
						dst[si  ] = (short)(acc1 + stream.ReadSByte());
						dst[si+1] = (short)(acc2 + stream.ReadSByte());
						dst[si+2] = (short)(dst[si  ] + stream.ReadSByte());
						dst[si+3] = (short)(dst[si+1] + stream.ReadSByte());
						dst[si+4] = (short)(dst[si+2] + stream.ReadSByte());
						dst[si+5] = (short)(dst[si+3] + stream.ReadSByte());
						dst[si+6] = (short)(dst[si+4] + stream.ReadSByte());
						dst[si+7] = (short)(dst[si+5] + stream.ReadSByte());
						acc1 = dst[si + 6];
						acc2 = dst[si + 7];
					}
					else // mono
					{
						dst[si  ] = (short)(acc1 + stream.ReadSByte());
						dst[si+1] = (short)(dst[si  ] + stream.ReadSByte());
						dst[si+2] = (short)(dst[si+1] + stream.ReadSByte());
						dst[si+3] = (short)(dst[si+2] + stream.ReadSByte());
						dst[si+4] = (short)(dst[si+3] + stream.ReadSByte());
						dst[si+5] = (short)(dst[si+4] + stream.ReadSByte());
						dst[si+6] = (short)(dst[si+5] + stream.ReadSByte());
						dst[si+7] = (short)(dst[si+6] + stream.ReadSByte());
						acc1 = dst[si + 7];
					}
				}
			}
			else if (c.Type == MED_TYPE)
			{
				// Local accumulators
				short acc1 = c.Channel1, acc2 = c.Channel2;

				// Used to hold the current 8 samples in their packed forms (8 samples in 12 bytes)
				byte* tmp = stackalloc byte[12];

				// Read/decode/accumulate 8 samples at a time
				for (uint si = 0; si < rem; si += 8)
				{
					// Read the packed samples (12 bytes total, the types dont matter, chosen for brevity)
					*((ulong*)tmp) = stream.ReadUInt64();
					*((uint*)(tmp + 8)) = stream.ReadUInt32();

					// Extract the packed values
					int p1 = *((int*)tmp  );
					int p2 = *((int*)tmp+3);
					int p3 = *((int*)tmp+6);
					int p4 = *((int*)tmp+9);

					// Extract the differences from the packed values (arith. right shift to get 2s compliment)
					int d1 = ((p1 & 0xFFF) << 20) >> 20;
					int d2 = ((p1 & 0xFFF000) << 8) >> 20;
					int d3 = ((p2 & 0xFFF) << 20) >> 20;
					int d4 = ((p2 & 0xFFF000) << 8) >> 20;
					int d5 = ((p3 & 0xFFF) << 20) >> 20;
					int d6 = ((p3 & 0xFFF000) << 8) >> 20;
					int d7 = ((p4 & 0xFFF) << 20) >> 20;
					int d8 = ((p4 & 0xFFF000) << 8) >> 20;

					// Perform the accumulation
					if (stereo)
					{
						dst[si  ] = (short)(acc1 + d1);
						dst[si+1] = (short)(acc2 + d2);
						dst[si+2] = (short)(dst[si  ] + d3);
						dst[si+3] = (short)(dst[si+1] + d4);
						dst[si+4] = (short)(dst[si+2] + d5);
						dst[si+5] = (short)(dst[si+3] + d6);
						dst[si+6] = (short)(dst[si+4] + d7);
						dst[si+7] = (short)(dst[si+5] + d8);
						acc1 = dst[si + 6];
						acc2 = dst[si + 7];
					}
					else // mono
					{
						dst[si  ] = (short)(acc1 + d1);
						dst[si+1] = (short)(dst[si  ] + d2);
						dst[si+2] = (short)(dst[si+1] + d3);
						dst[si+3] = (short)(dst[si+2] + d4);
						dst[si+4] = (short)(dst[si+3] + d5);
						dst[si+5] = (short)(dst[si+4] + d6);
						dst[si+6] = (short)(dst[si+5] + d7);
						dst[si+7] = (short)(dst[si+6] + d8);
						acc1 = dst[si + 7];
					}
				}
			}
			else // Full samples
			{
				// Directly read the samples
				for (uint si = 0; si < rem; si += 8)
				{
					dst[si  ] = stream.ReadInt16();
					dst[si+1] = stream.ReadInt16();
					dst[si+2] = stream.ReadInt16();
					dst[si+3] = stream.ReadInt16();
					dst[si+4] = stream.ReadInt16();
					dst[si+5] = stream.ReadInt16();
					dst[si+6] = stream.ReadInt16();
					dst[si+7] = stream.ReadInt16();
				}
			}

			// Save the channels
			if (stereo)
			{
				c.Channel1 = dst[rem - 2];
				c.Channel2 = dst[rem - 1];
			}
			else
				c.Channel1 = dst[rem - 1];
		}

		// Holds RLAD chunk information
		private struct Chunk
		{
			public ushort Type;
			public ushort Extra;
			public short Channel1; // The last sample of the chunk, used to accumulate deltas, channel 1
			public short Channel2; // The last sample of the chunk, used to accumulate deltas, channel 2
			public uint FrameCount => (Extra + 1u) * 8;
		}
	}
}
