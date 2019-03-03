using System;

namespace Prism.Builtin
{
	// Lossy audio compression algorithm that groups up fractional residuals into chunks, and achieves a 
	//   constant 62.5% compression ratio.
	internal class FSRAudio : ProcessedAudio
	{
		private const int MAX_FRAC = 127; // Max value encodable in 7 bits
		private const float MAX_FRAC_F = MAX_FRAC;

		#region Fields
		public override uint FrameCount { get; protected set; }
		public override IntPtr Data { get; protected set; }
		public readonly uint DataLength;
		#endregion // Fields

		public FSRAudio(RawAudio raw, IntPtr data, uint fc, uint dl) :
			base(raw)
		{
			FrameCount = fc;
			Data = data;
			DataLength = dl;
		}

		// This can write to the same data, because it always gets the information it needs before back-writing
		public unsafe static FSRAudio Encode(RawAudio raw)
		{
			uint chunkCount = (raw.FrameCount - 1) / 4; // A chunk is 4 samples: 1 full sample and 3 fractional residuals

			short* srcPtr = (short*)raw.Data.ToPointer();
			byte* dstPtr = (byte*)raw.Data.ToPointer();

			uint dataSize = 0;
			if (raw.Stereo)
			{
				// Get the initial left border samples for chunk 1
				short c1_l = srcPtr[0],
				      c2_l = srcPtr[1];

				for (uint ci = 0; ci < chunkCount; ++ci, srcPtr += 8, dstPtr += 10)
				{
					// Write the full samples
					dstPtr[0] = (byte)(c1_l & 0xFF);
					dstPtr[1] = (byte)((c1_l >> 8) & 0xFF);
					dstPtr[2] = (byte)(c2_l & 0xFF);
					dstPtr[3] = (byte)((c2_l >> 8) & 0xFF);

					// Get the right border samples
					short c1_r = srcPtr[8],
						  c2_r = srcPtr[9];

					// Calculate the slopes and slope steps
					int c1_s = c1_r - c1_l,
						c2_s = c2_r - c2_l;
					ushort c1_ss = (ushort)Math.Max((c1_s < 0 ? -c1_s : c1_s) / MAX_FRAC_F, 1),
						   c2_ss = (ushort)Math.Max((c2_s < 0 ? -c2_s : c2_s) / MAX_FRAC_F, 1);

					// Calculate the mid values
					short c1_m = (short)((c1_l + c1_r) / 2),
						  c2_m = (short)((c2_l + c2_r) / 2);

					// Calculate the differences
					ushort c1_d1 = (ushort)Math.Abs(srcPtr[2] - c1_m),
						   c1_d2 = (ushort)Math.Abs(srcPtr[4] - c1_m),
						   c1_d3 = (ushort)Math.Abs(srcPtr[6] - c1_m),
						   c2_d1 = (ushort)Math.Abs(srcPtr[3] - c2_m),
						   c2_d2 = (ushort)Math.Abs(srcPtr[5] - c2_m),
						   c2_d3 = (ushort)Math.Abs(srcPtr[7] - c2_m);

					// Calculate the clamped step differences
					byte c1_s1 = (byte)(Math.Min(c1_d1 / c1_ss, MAX_FRAC) & 0x7F),
						 c1_s2 = (byte)(Math.Min(c1_d2 / c1_ss, MAX_FRAC) & 0x7F),
						 c1_s3 = (byte)(Math.Min(c1_d3 / c1_ss, MAX_FRAC) & 0x7F),
						 c2_s1 = (byte)(Math.Min(c2_d1 / c2_ss, MAX_FRAC) & 0x7F),
						 c2_s2 = (byte)(Math.Min(c2_d2 / c2_ss, MAX_FRAC) & 0x7F),
						 c2_s3 = (byte)(Math.Min(c2_d3 / c2_ss, MAX_FRAC) & 0x7F);

					// Write the fractional residuals (adding sign bit, where necessary)
					dstPtr[4] = ((srcPtr[2] - c1_m) >= 0) ? c1_s1 : (byte)(c1_s1 | 0x80);
					dstPtr[5] = ((srcPtr[3] - c2_m) >= 0) ? c2_s1 : (byte)(c2_s1 | 0x80);
					dstPtr[6] = ((srcPtr[4] - c1_m) >= 0) ? c1_s2 : (byte)(c1_s2 | 0x80);
					dstPtr[7] = ((srcPtr[5] - c2_m) >= 0) ? c2_s2 : (byte)(c2_s2 | 0x80);
					dstPtr[8] = ((srcPtr[6] - c1_m) >= 0) ? c1_s3 : (byte)(c1_s3 | 0x80);
					dstPtr[9] = ((srcPtr[7] - c2_m) >= 0) ? c2_s3 : (byte)(c2_s3 | 0x80);

					// Save the current right border samples as the new left border samples
					c1_l = c1_r;
					c2_l = c2_r;
				}

				// Write the final border samples to the stream
				dstPtr[0] = (byte)(c1_l & 0xFF);
				dstPtr[1] = (byte)((c1_l >> 8) & 0xFF);
				dstPtr[2] = (byte)(c2_l & 0xFF);
				dstPtr[3] = (byte)((c2_l >> 8) & 0xFF);

				// Save the data size
				dataSize = (uint)(dstPtr - (byte*)raw.Data.ToPointer());
			}
			else // Mono
			{
				// Get the initial left border sample for chunk 1
				short sl = srcPtr[0];

				for (uint ci = 0; ci < chunkCount; ++ci, srcPtr += 4, dstPtr += 5)
				{
					// Write the full sample
					dstPtr[0] = (byte)(sl & 0xFF);
					dstPtr[1] = (byte)((sl >> 8) & 0xFF);

					// Get the right border sample
					short sr = srcPtr[4];

					// Calculate the slope and slope step
					int ss = sr - sl;
					ushort sss = (ushort)Math.Max((ss < 0 ? -ss : ss) / MAX_FRAC_F, 1);

					// Calculate the mid value
					short sm = (short)((sl + sr) / 2);

					// Calculate the differences
					ushort sd1 = (ushort)Math.Abs(srcPtr[1] - sm),
						   sd2 = (ushort)Math.Abs(srcPtr[2] - sm),
						   sd3 = (ushort)Math.Abs(srcPtr[3] - sm);

					// Calculate the clamped step differences
					byte ss1 = (byte)Math.Min(sd1 / sss, MAX_FRAC),
						 ss2 = (byte)Math.Min(sd2 / sss, MAX_FRAC),
						 ss3 = (byte)Math.Min(sd3 / sss, MAX_FRAC);

					// Write the fractional residuals (adding sign bit, where necessary)
					dstPtr[2] = ((srcPtr[1] - sm) >= 0) ? ss1 : (byte)(ss1 | 0x80);
					dstPtr[3] = ((srcPtr[2] - sm) >= 0) ? ss2 : (byte)(ss2 | 0x80);
					dstPtr[4] = ((srcPtr[3] - sm) >= 0) ? ss3 : (byte)(ss3 | 0x80);

					// Save the current right border sample as the new left border sample
					sl = sr;
				}

				// Write the final border samples to the stream
				dstPtr[0] = (byte)(sl & 0xFF);
				dstPtr[1] = (byte)((sl >> 8) & 0xFF);

				// Save the data size
				dataSize = (uint)(dstPtr - (byte*)raw.Data.ToPointer());
			}

			// Return the setup
			return new FSRAudio(raw, raw.TakeData(), (chunkCount * 4) + 1, dataSize);
		}
	}
}
