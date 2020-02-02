/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using Spectrum.Content;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Spectrum.Audio
{
	// Streams RLAD encoded audio from the disk
	internal class RLADStream : IDisposable, IAudioStreamer
	{
		private const uint MAX_BUFFER_SIZE = 512; // Any RLAD chunk can have a maximum of 512 (8 * 64) frames
		public const int SMALL_TYPE = 0;
		public const int MED_TYPE = 1;
		public const int FULL_TYPE = 2;

		#region Fields
		public uint TotalFrames => _totalFrames;
		private readonly uint _totalFrames;
		public uint SampleRate => _sampleRate;
		private readonly uint _sampleRate;
		public AudioFormat Format => _format;
		private readonly AudioFormat _format;
		public bool IsLossy => _isLossy;
		private readonly bool _isLossy;

		// Stream info
		private readonly ContentReader _reader;
		private uint _frameOffset = 0;
		public uint RemainingFrames => TotalFrames - _frameOffset;
		private Chunk _currChunk; // The current chunk that the stream is ready to read

		// Sample buffer
		private short[] _buffer;
		private uint _bufferOff = 0; // The current offset into the buffer for the next sample
		private uint _bufferSize = 0; // The remaining samples in the buffer
		private uint _bufferRem => _bufferSize - _bufferOff;
		#endregion // Fields

		public RLADStream(ContentReader reader)
		{
			_reader = reader;

			// Read in the header
			ReadStreamHeader(reader, out _totalFrames, out _sampleRate, out _format, out _isLossy);
			_buffer = new short[MAX_BUFFER_SIZE * Format.GetChannelCount()];

			// Read first chunk
			_currChunk = default;
			ReadChunkHeader(reader, ref _currChunk);
		}
		~RLADStream()
		{
			dispose(false);
		}

		public uint ReadFrames(Span<byte> data, uint fcount)
		{
			var dst = MemoryMarshal.Cast<byte, short>(data);
			if (fcount > RemainingFrames)
				fcount = RemainingFrames;
			if ((fcount * Format.GetChannelCount()) > dst.Length)
				throw new ArgumentException($"The array is not large enough to receive the requested number of samples.");

			// Tracking read info
			uint rem = fcount;
			uint dstOff = 0;

			// Read the rest of the buffered samples, if there are any
			if (_bufferSize > 0)
			{
				_buffer.AsSpan().Slice((int)_bufferOff, (int)_bufferRem).CopyTo(dst);
				dstOff += _bufferRem;
				rem -= (_bufferRem / Format.GetChannelCount());
				_bufferSize = 0;
				_bufferOff = 0;
			}

			// Perform as many direct writes into the dst array as possible
			while (_currChunk.FrameCount <= rem)
			{
				DecodeChunk(_reader, dst.Slice((int)dstOff), ref _currChunk, Format.GetChannelCount() > 1);
				dstOff += (_currChunk.FrameCount * Format.GetChannelCount());
				rem -= _currChunk.FrameCount;
				ReadChunkHeader(_reader, ref _currChunk);
			}

			// If needed, buffer the next chunk, and copy as much into the dst array as needed
			if (rem > 0)
			{
				// Buffer the next chunk
				DecodeChunk(_reader, _buffer.AsSpan(), ref _currChunk, Format.GetChannelCount() > 1);
				_bufferSize = (_currChunk.FrameCount * Format.GetChannelCount());

				// Copy as much as is needed into the dst array
				_bufferOff = rem * Format.GetChannelCount();
				_buffer.AsSpan().Slice(0, (int)_bufferOff).CopyTo(dst.Slice((int)dstOff));

				// Read the chunk for the next call
				ReadChunkHeader(_reader, ref _currChunk);
			}

			// Return the number of frames actually read
			_frameOffset += fcount;
			return fcount;
		}

		public void Reset()
		{
			_reader.Reset();
			ReadStreamHeader(_reader, out _, out _, out _, out _);

			_frameOffset = 0;
			_bufferSize = 0;
			_bufferOff = 0;

			_currChunk = default;
			ReadChunkHeader(_reader, ref _currChunk); // Read in the first chunk as expected
		}

		public static void ReadStreamHeader(ContentReader reader, out uint fcount, out uint rate, out AudioFormat format, out bool lossy)
		{
			reader.Reset();
			fcount = reader.ReadUInt32();
			rate = reader.ReadUInt32();
			format = reader.ReadBoolean() ? AudioFormat.Stereo16 : AudioFormat.Mono16;
			lossy = reader.ReadBoolean();
		}

		#region Decoding
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ReadChunkHeader(ContentReader reader, ref Chunk c)
		{
			if (reader.Remaining > 0)
			{
				byte header = reader.ReadByte();
				c.Type = (ushort)((header >> 6) & 0x03);
				c.Extra = (ushort)(header & 0x3F);
				if (c.Type == 3)
					throw new InvalidDataException("Invalid RLAD header size type.");
			}
		}

		// Dst must be large enough to accept up to 512 samples (1024 for stereo)
		private static unsafe void DecodeChunk(ContentReader reader, Span<short> dst, ref Chunk c, bool stereo)
		{
			uint scount = c.FrameCount * (stereo ? 2u : 1);

			if (c.Type == SMALL_TYPE)
			{
				// Local accumulators
				short acc1 = c.Acc1, acc2 = c.Acc2;
				Span<sbyte> deltas = stackalloc sbyte[(int)scount];
				reader.Read(deltas);

				// Read in 8 deltas at a time, and accumulate them
				for (int si = 0; si < scount; si += 8)
				{
					if (stereo)
					{
						dst[si]   = (short)(acc1      + deltas[si]);
						dst[si+1] = (short)(acc2      + deltas[si+1]);
						dst[si+2] = (short)(dst[si]   + deltas[si+2]);
						dst[si+3] = (short)(dst[si+1] + deltas[si+3]);
						dst[si+4] = (short)(dst[si+2] + deltas[si+4]);
						dst[si+5] = (short)(dst[si+3] + deltas[si+5]);
						dst[si+6] = (short)(dst[si+4] + deltas[si+6]);
						dst[si+7] = (short)(dst[si+5] + deltas[si+7]);
						acc1      = dst[si+6];
						acc2      = dst[si+7];
					}
					else // mono
					{
						dst[si]   = (short)(acc1      + deltas[si]);
						dst[si+1] = (short)(dst[si]   + deltas[si+1]);
						dst[si+2] = (short)(dst[si+1] + deltas[si+2]);
						dst[si+3] = (short)(dst[si+2] + deltas[si+3]);
						dst[si+4] = (short)(dst[si+3] + deltas[si+4]);
						dst[si+5] = (short)(dst[si+4] + deltas[si+5]);
						dst[si+6] = (short)(dst[si+5] + deltas[si+6]);
						dst[si+7] = (short)(dst[si+6] + deltas[si+7]);
						acc1      = dst[si+7];
					}
				}
			}
			else if (c.Type == MED_TYPE)
			{
				// Local accumulators
				short acc1 = c.Acc1, acc2 = c.Acc2;
				Span<byte> deltas = stackalloc byte[(int)(scount * 3) / 2];
				reader.Read(deltas);

				// Read/decode/accumulate 8 samples at a time
				for (int si = 0, doff = 0; si < scount; si += 8, doff += 12)
				{
					// Extract the deltas
					int p1, p2, p3, p4;
					fixed (byte* tmp = deltas.Slice(doff))
					{
						p1 = *((int*)tmp);
						p2 = *((int*)(tmp + 3));
						p3 = *((int*)(tmp + 6));
						p4 = *((int*)(tmp + 9));
					}

					// Extract the differences from the packed values (arith. right shift to get 2s compliment)
					int d1 = ((p1 & 0xFFF)    << 20) >> 20;
					int d2 = ((p1 & 0xFFF000) << 8)  >> 20;
					int d3 = ((p2 & 0xFFF)    << 20) >> 20;
					int d4 = ((p2 & 0xFFF000) << 8)  >> 20;
					int d5 = ((p3 & 0xFFF)    << 20) >> 20;
					int d6 = ((p3 & 0xFFF000) << 8)  >> 20;
					int d7 = ((p4 & 0xFFF)    << 20) >> 20;
					int d8 = ((p4 & 0xFFF000) << 8)  >> 20;

					// Perform the accumulation
					if (stereo)
					{
						dst[si]   = (short)(acc1      + d1);
						dst[si+1] = (short)(acc2      + d2);
						dst[si+2] = (short)(dst[si]   + d3);
						dst[si+3] = (short)(dst[si+1] + d4);
						dst[si+4] = (short)(dst[si+2] + d5);
						dst[si+5] = (short)(dst[si+3] + d6);
						dst[si+6] = (short)(dst[si+4] + d7);
						dst[si+7] = (short)(dst[si+5] + d8);
						acc1      = dst[si+6];
						acc2      = dst[si+7];
					}
					else // mono
					{
						dst[si]   = (short)(acc1      + d1);
						dst[si+1] = (short)(dst[si]   + d2);
						dst[si+2] = (short)(dst[si+1] + d3);
						dst[si+3] = (short)(dst[si+2] + d4);
						dst[si+4] = (short)(dst[si+3] + d5);
						dst[si+5] = (short)(dst[si+4] + d6);
						dst[si+6] = (short)(dst[si+5] + d7);
						dst[si+7] = (short)(dst[si+6] + d8);
						acc1      = dst[si+7];
					}
				}
			}
			else // Full samples
			{
				// Directly read the samples into the buffer
				reader.Read(dst);
			}

			// Save the channels
			if (stereo)
			{
				c.Acc1 = dst[(int)scount - 2];
				c.Acc2 = dst[(int)scount - 1];
			}
			else
				c.Acc1 = dst[(int)scount - 1];
		}

		public unsafe static void DecodeAll(ContentReader reader, Span<short> dst, AudioFormat fmt, uint fcount)
		{
			uint rem = fcount;
			uint dstOff = 0;

			Chunk c = default;
			c.Acc1 = c.Acc2 = 0;

			while (rem > 0)
			{
				ReadChunkHeader(reader, ref c);
				if (c.FrameCount > rem) break; // Prevents an accidental infinite loop for malformatted input data

				DecodeChunk(reader, dst.Slice((int)dstOff), ref c, fmt.GetChannelCount() > 1);
				dstOff += (c.FrameCount * fmt.GetChannelCount());
				rem -= c.FrameCount;
			}
		}
		#endregion // Decoding

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (_buffer != null && disposing)
			{
				_reader.Dispose();
			}
			_buffer = null;
		}
		#endregion IDisposable

		// Holds RLAD chunk information
		private struct Chunk
		{
			public ushort Type;
			public ushort Extra;
			public short Acc1; // The last sample of the chunk, used to accumulate deltas, channel 1
			public short Acc2; // The last sample of the chunk, used to accumulate deltas, channel 2
			public uint FrameCount => (Extra + 1u) * 8;
		}
	}
}
