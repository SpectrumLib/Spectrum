using System;
using System.Diagnostics;

namespace Prism.Builtin
{
	// Lossless audio compression algorithm that encodes sample differences in runs of various sizes.
	internal class RLADAudio : ProcessedAudio
	{
		private const int MAX_CHUNK_SIZE = 64;
		private const int SMALL_DIFF = 0x7F; // 127 is the largest difference for 8 bits (7 available)
		private const int MED_DIFF = 0x7FF; // 2047 is the largest difference for 12 bits (11 available)
		public const int SMALL_TYPE = 0;
		public const int MED_TYPE = 1;
		public const int FULL_TYPE = 2;

		#region Fields
		public override uint FrameCount { get; protected set; }
		public override IntPtr Data { get; protected set; }
		public readonly uint DataLength;

		public readonly Chunk[] Chunks;
		public readonly uint ChunkCount;
		#endregion // Fields

		public RLADAudio(RawAudio raw, IntPtr data, uint fc, uint dl, Chunk[] chunks, uint cc) :
			base(raw)
		{
			FrameCount = fc;
			Data = data;
			DataLength = dl;
			Chunks = chunks;
			ChunkCount = cc;
		}

		// This can write to the same data, because it always gets the information it needs before back-writing
		public unsafe static RLADAudio Encode(RawAudio raw, bool stats, PipelineLogger logger)
		{
			// Get the pointers and the chunk sizes
			short* srcPtr = (short*)raw.Data.ToPointer();
			byte* dstPtr = (byte*)raw.Data.ToPointer();
			uint realSize = raw.FrameCount - (raw.FrameCount % 8);
			uint chunkCount = realSize / 8;

			// The chunks
			Chunk[] chunks = new Chunk[chunkCount];

			// Used to track the final data length
			byte* dstStart = dstPtr;

			// Generate all of the initial chunks
			Stopwatch timer = Stopwatch.StartNew();
			if (raw.Stereo)
			{
				// Tracks the running full sample value
				short c1f = srcPtr[14],
					  c2f = srcPtr[15];

				// The first chunk is always full 16-bit samples
				chunks[0].Type = FULL_TYPE;
				chunks[0].Extra = 0;
				srcPtr += 16;
				dstPtr += 32;

				// Temp array to hold the differences for each chunk
				int* diffs = stackalloc int[16];

				// Loop over each chunk and assign the chunk values on a first order (ignore runs)
				uint dstLen = 0; // Assign this before each loop end
				for (uint ci = 1; ci < chunkCount; ++ci, srcPtr += 16, dstPtr += dstLen)
				{
					// Generate the differences, and find the max difference at the same time
					int maxdiff = 0;
					for (uint si = 0; si < 16; si += 2)
					{
						int d1 = diffs[si] = srcPtr[si] - c1f;
						int d2 = diffs[si + 1] = srcPtr[si + 1] - c2f;
						c1f = srcPtr[si];
						c2f = srcPtr[si + 1];
						int md = Math.Max(Math.Abs(d1), Math.Abs(d2));
						if (md > maxdiff)
							maxdiff = md;
					}

					// Get the size type
					int stype = (maxdiff <= SMALL_DIFF) ? SMALL_TYPE : (maxdiff <= MED_DIFF) ? MED_TYPE : FULL_TYPE;

					// Write the input to the output
					if (stype == FULL_TYPE)
					{
						Buffer.MemoryCopy(srcPtr, dstPtr, 32, 32);
						dstLen = 32;
					}
					else if (stype == SMALL_TYPE)
					{
						sbyte* dp2 = (sbyte*)dstPtr;
						for (uint di = 0; di < 16; ++di)
							dp2[di] = (sbyte)diffs[di];
						dstLen = 16;
					}
					else
					{
						for (uint di = 0, dsti = 0; di < 16; di += 4, dsti += 6)
						{
							// Create the 12-bit differences
							int c1d1 = (diffs[di  ] & 0x7FF) | ((diffs[di  ] < 0) ? 0x800 : 0);
							int c2d1 = (diffs[di+1] & 0x7FF) | ((diffs[di+1] < 0) ? 0x800 : 0);
							int c1d2 = (diffs[di+2] & 0x7FF) | ((diffs[di+2] < 0) ? 0x800 : 0);
							int c2d2 = (diffs[di+3] & 0x7FF) | ((diffs[di+3] < 0) ? 0x800 : 0);

							// Write the packed values
							*((int*)(dstPtr+dsti  )) = c1d1 | (c2d1 << 12);
							*((int*)(dstPtr+dsti+3)) = c1d2 | (c2d2 << 12);
						}
						dstLen = 24;
					}

					// Save the chunk info
					chunks[ci].Type = (ushort)stype;
					chunks[ci].Extra = 0;
				}
			}
			else // Mono
			{
				// Tracks the running full sample value
				short c1f = srcPtr[7];

				// The first chunk is always full 16-bit samples
				chunks[0].Type = FULL_TYPE;
				chunks[0].Extra = 0;
				srcPtr += 8;
				dstPtr += 16;

				// Temp array to hold the differences for each chunk
				int* diffs = stackalloc int[8];

				// Loop over each chunk and assign the chunk values on a first order (ignore runs)
				uint dstLen = 0; // Assign this before each loop end
				for (uint ci = 1; ci < chunkCount; ++ci, srcPtr += 8, dstPtr += dstLen)
				{
					// Generate the differences, and find the max difference at the same time
					int maxdiff = 0;
					for (uint si = 0; si < 8; si += 2)
					{
						int d1 = diffs[si] = srcPtr[si] - c1f;
						int d2 = diffs[si + 1] = srcPtr[si + 1] - srcPtr[si];
						c1f = srcPtr[si + 1];
						int md = Math.Max(Math.Abs(d1), Math.Abs(d2));
						if (md > maxdiff)
							maxdiff = md;
					}

					// Get the size type
					int stype = (maxdiff <= SMALL_DIFF) ? SMALL_TYPE : (maxdiff <= MED_DIFF) ? MED_TYPE : FULL_TYPE;

					// Write the input to the output
					if (stype == FULL_TYPE)
					{
						Buffer.MemoryCopy(srcPtr, dstPtr, 16, 16);
						dstLen = 16;
					}
					else if (stype == SMALL_TYPE)
					{
						sbyte* dp2 = (sbyte*)dstPtr;
						for (uint di = 0; di < 8; ++di)
							dp2[di] = (sbyte)diffs[di];
						dstLen = 8;
					}
					else
					{
						for (uint di = 0, dsti = 0; di < 8; di += 4, dsti += 6)
						{
							// Create the 12-bit differences
							int d1 = (diffs[di  ] & 0x7FF) | ((diffs[di  ] < 0) ? 0x800 : 0);
							int d2 = (diffs[di+1] & 0x7FF) | ((diffs[di+1] < 0) ? 0x800 : 0);
							int d3 = (diffs[di+2] & 0x7FF) | ((diffs[di+2] < 0) ? 0x800 : 0);
							int d4 = (diffs[di+3] & 0x7FF) | ((diffs[di+3] < 0) ? 0x800 : 0);

							// Write the packed values
							*((int*)(dstPtr+dsti  )) = d1 | (d2 << 12);
							*((int*)(dstPtr+dsti+3)) = d3 | (d4 << 12);
						}
						dstLen = 12;
					}

					// Save the chunk info
					chunks[ci].Type = (ushort)stype;
					chunks[ci].Extra = 0;
				}
			}

			// Stats values
			uint* stc = stackalloc uint[3] { 0, 0, 0 };
			uint* stl = stackalloc uint[3] { 0, 0, 0 };
			uint* stmin = stackalloc uint[3] { 0, 0, 0 };
			uint* stmax = stackalloc uint[3] { 0, 0, 0 };

			// Compactify the chunks by combining adjacent ones of the same size type
			uint wi = 0, // The chunk index to write
				 ri = 0; // The chunk index to read
			while (ri < chunkCount)
			{
				ushort stype = chunks[ri].Type;
				uint rem = Math.Min(chunkCount - ri, 64);

				uint count = 1;
				while ((count < rem) && (chunks[ri+count].Type == stype)) ++count;

				chunks[wi  ].Type = stype;
				chunks[wi++].Extra = (ushort)(count - 1);
				ri += count;

				if (stats)
				{
					stc[stype] += 1;
					stl[stype] += count;
					if (count == 1) stmin[stype] += 1;
					else if (count == 64) stmax[stype] += 1;
				}
			}

			// Report stats
			uint dataLen = (uint)(dstPtr - dstStart);
			if (stats)
			{
				logger.Stats($"Chunk Size Types:   " +
					$"S={stc[0]} ({stc[0]*100/(float)wi:0.000}%)   " +
					$"M={stc[1]} ({stc[1]*100/(float)wi:0.000}%)   " +
					$"F={stc[2]} ({stc[2]*100/(float)wi:0.000}%)");
				logger.Stats($"Average Chunk Run Lengths:   " +
					$"S={stl[0]/(float)stc[0]:0.00}   " +
					$"M={stl[1]/(float)stc[1]:0.00}   " +
					$"F={stl[2]/(float)stc[2]:0.00}   " +
					$"Overall={(stl[0] + stl[1] + stl[2]) / (float)wi:0.00}");
				logger.Stats($"Chunk Length Extrema:   " +
					$"S={stmin[0]}/{stmax[0]} ({stmin[0]*100/(float)stc[0]:0.00}%/{stmax[0]*100/(float)stc[0]:0.00}%)   " +
					$"S={stmin[1]}/{stmax[1]} ({stmin[1]*100/(float)stc[1]:0.00}%/{stmax[1]*100/(float)stc[1]:0.00}%)   " +
					$"S={stmin[2]}/{stmax[2]} ({stmin[2]*100/(float)stc[2]:0.00}%/{stmax[2]*100/(float)stc[2]:0.00}%)");
				float startSize = realSize * 2 * (raw.Stereo ? 2 : 1);
				logger.Stats($"Compression Stats:   Ratio={dataLen/startSize:0.0000}   Speed={realSize/timer.Elapsed.TotalSeconds/1024/1024:0.00} MB/s");
			}

			// Return the object
			return new RLADAudio(raw, raw.TakeData(), chunkCount * 8, dataLen, chunks, wi);
		}

		// Type for tracking data about an RLAD chunk
		public struct Chunk
		{
			public ushort Type; // Will have a value between 0 and 3
			public ushort Extra; // The number of extra chunks in this chunk run (from 0 - 63)

			public Chunk(ushort t, ushort e)
			{
				Type = t;
				Extra = e;
			}
		}
	}
}
