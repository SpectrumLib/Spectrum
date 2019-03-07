using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Spectrum.Audio
{
	// Contains the logic required to decode and stream FSR-encoded audio data from a file
	internal class FSRStream : IDisposable
	{
		private const int MAX_FRAC = 127; // Max value encodable in 7 bits
		private const float MAX_FRAC_F = MAX_FRAC;

		#region Fields
		// If the data is stereo
		public readonly bool Stereo;

		// The total number of frames available in the data
		public readonly uint FrameCount;
		// The current frame offset into the stream
		private uint _frameOffset;
		// The remaining number of frames to available to read
		public uint RemainingFrames => FrameCount - _frameOffset;

		// The offset into the file for the start of the audio data
		public readonly uint Offset;
		// The file streams
		private readonly FileStream _file;
		private readonly BinaryReader _reader;

		private bool _isDisposed = false;
		#endregion // Fields

		public FSRStream(string file, uint offset, bool stereo, uint fc)
		{
			Offset = offset;

			Stereo = stereo;
			FrameCount = fc;
			_frameOffset = 0;

			_file = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
			_reader = new BinaryReader(_file);
			_file.Seek(offset, SeekOrigin.Begin);
		}
		~FSRStream()
		{
			dispose(false);
		}

		// Reads a number of frames into the array
		public unsafe uint Read(short[] dst, uint count)
		{
			if (count > RemainingFrames)
				count = RemainingFrames;

			fixed (short* ptr = dst)
			{
				ReadSamples(_reader, ptr, count, Stereo);
			}

			_frameOffset += count;
			return count;
		}

		// Resets the stream back to the beginning
		public void Reset()
		{
			if (_frameOffset != 0)
			{
				_frameOffset = 0;
				_file.Seek(Offset, SeekOrigin.Begin);
			}
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
				_reader?.Dispose();
				_file?.Dispose();
			}
			_isDisposed = true;
		}
		#endregion // IDisposable

		// Performs the actual reading logic, the dst array must be large enough to accept the number of requested samples
		// The reader *must* be on an FSR chunk boundary for this read to succeed
		// The reader used by this function must wrap a seekable stream
		public static unsafe void ReadSamples(BinaryReader reader, short* dstPtr, uint count, bool stereo)
		{
			short* ssamp = stackalloc short[2];
			uint chunkCount = (count - 1) / 4;
			byte* oPtr = (byte*)dstPtr;

			// Read initial values
			ssamp[0] = (short)(reader.ReadByte() | (reader.ReadByte() << 8));
			ssamp[1] = stereo ? (short)(reader.ReadByte() | (reader.ReadByte() << 8)) : (short)0;

			// Decode full chunks
			for (uint ci = 0; ci < chunkCount; ++ci, dstPtr += (stereo ? 8 : 4))
				DecodeChunk(reader, dstPtr, stereo, ref ssamp[0], ref ssamp[1]);

			// Check for a partial chunk decode
			if ((count % 4) != 0)
			{
				// Need to decode one final chunk into temp and copy as much as we need
				short* tmp = stackalloc short[stereo ? 8 : 4];
				DecodeChunk(reader, tmp, stereo, ref ssamp[0], ref ssamp[1]);
				long rem = (count * (stereo ? 4 : 2)) - (long)((byte*)dstPtr - oPtr);
				Buffer.MemoryCopy(tmp, dstPtr, rem, rem);
			}

			// Seek to nearest chunk start
			reader.BaseStream.Seek(stereo ? -4 : -2, SeekOrigin.Current);
		}

		// Decodes a single 4-sample chunk, returns the new reference sample values
		// Requires the base samples from the current chunk, returns the base samples of the next chunk
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void DecodeChunk(BinaryReader reader, short* dst, bool stereo, ref short c1_l, ref short c2_l)
		{
			dst[0] = c1_l;
			if (stereo) dst[1] = c2_l;

			if (stereo)
			{
				// Read the fractional residuals
				byte c1_d1 = reader.ReadByte(),
					 c2_d1 = reader.ReadByte(),
					 c1_d2 = reader.ReadByte(),
					 c2_d2 = reader.ReadByte(),
					 c1_d3 = reader.ReadByte(),
					 c2_d3 = reader.ReadByte();

				// Read the right border samples
				short c1_r = (short)(reader.ReadByte() | (reader.ReadByte() << 8)),
					  c2_r = (short)(reader.ReadByte() | (reader.ReadByte() << 8));

				// Calculate the slopes, slope steps, and mid values
				int c1_s = c1_r - c1_l,
					c2_s = c2_r - c2_l;
				ushort c1_ss = (ushort)Math.Max((c1_s < 0 ? -c1_s : c1_s) / MAX_FRAC_F, 1),
					   c2_ss = (ushort)Math.Max((c2_s < 0 ? -c2_s : c2_s) / MAX_FRAC_F, 1);
				short c1_m = (short)((c1_l + c1_r) / 2),
					  c2_m = (short)((c2_l + c2_r) / 2);

				// Decode and write the residual samples
				dst[2] = (short)(c1_m + (c1_ss * ((c1_d1 & 0x80) > 0 ? -(c1_d1 & 0x7F) : (c1_d1 & 0x7F))));
				dst[3] = (short)(c2_m + (c2_ss * ((c2_d1 & 0x80) > 0 ? -(c2_d1 & 0x7F) : (c2_d1 & 0x7F))));
				dst[4] = (short)(c1_m + (c1_ss * ((c1_d2 & 0x80) > 0 ? -(c1_d2 & 0x7F) : (c1_d2 & 0x7F))));
				dst[5] = (short)(c2_m + (c2_ss * ((c2_d2 & 0x80) > 0 ? -(c2_d2 & 0x7F) : (c2_d2 & 0x7F))));
				dst[6] = (short)(c1_m + (c1_ss * ((c1_d3 & 0x80) > 0 ? -(c1_d3 & 0x7F) : (c1_d3 & 0x7F))));
				dst[7] = (short)(c2_m + (c2_ss * ((c2_d3 & 0x80) > 0 ? -(c2_d3 & 0x7F) : (c2_d3 & 0x7F))));

				// Save the current right border samples as the new left border samples
				c1_l = c1_r;
				c2_l = c2_r;
			}
			else // Mono
			{
				// Read the fractional residuals
				byte cd1 = reader.ReadByte(),
					 cd2 = reader.ReadByte(),
					 cd3 = reader.ReadByte();

				// Read the right border samples
				short cr = (short)(reader.ReadByte() | (reader.ReadByte() << 8));

				// Calculate the slopes, slope steps, and mid values
				int cs = cr - c1_l;
				ushort css = (ushort)Math.Max((cs < 0 ? -cs : cs) / MAX_FRAC_F, 1);
				short cm = (short)((c1_l + cr) / 2);

				// Decode and write the residual samples
				dst[1] = (short)(cm + (css * ((cd1 & 0x80) > 0 ? -(cd1 & 0x7F) : (cd1 & 0x7F))));
				dst[2] = (short)(cm + (css * ((cd2 & 0x80) > 0 ? -(cd2 & 0x7F) : (cd2 & 0x7F))));
				dst[3] = (short)(cm + (css * ((cd3 & 0x80) > 0 ? -(cd3 & 0x7F) : (cd3 & 0x7F))));
			}
		}
	}
}
