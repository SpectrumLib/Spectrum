using System;

namespace Prism.Builtin
{
	// Fast, lossy compression that encodes every other sample as a residual ("simple staggered sample residuals")
	// This type writes to the same memory that the original audio data came in, so dont dispose it
	internal class S3RAudio : ProcessedAudio
	{
		private const int D_MIN = SByte.MinValue;
		private const int D_MAX = SByte.MaxValue;

		#region Fields
		public override uint FrameCount { get; protected set; }
		public override IntPtr Data { get; protected set; }
		public readonly uint DataLength;
		#endregion // Fields

		private S3RAudio(RawAudio raw, IntPtr data, uint fc, uint dl) :
			base(raw)
		{
			FrameCount = fc;
			Data = data;
			DataLength = dl;
		}
		
		// This can write to the same data, because it always gets the information it needs before back-writing
		public unsafe static S3RAudio Encode(RawAudio raw)
		{
			bool isOdd = (raw.FrameCount & 1) > 0;
			uint frameCount = raw.FrameCount - (isOdd ? 0u : 1u); // Can only encode an odd-length stream (tack on last sample at end)

			short* srcPtr = (short*)raw.Data.ToPointer();
			byte* dstPtr = (byte*)raw.Data.ToPointer();

			//return new S3RAudio(raw, raw.TakeData(), raw.FrameCount, (uint)raw.DataLength);

			uint dataLen = 0;
			if (raw.Stereo)
			{
				short c1_l = srcPtr[0];
				short c2_l = srcPtr[1];

				for (uint fi = 0; fi < frameCount; fi += 2, srcPtr += 4, dstPtr += 6)
				{
					// Get the actual samples and right-hand samples
					short c1_a = srcPtr[2], 
						  c2_a = srcPtr[3],
					      c1_r = srcPtr[4], 
					      c2_r = srcPtr[5];

					// Calculate the mean value between the left and right samples
					short c1_m = (short)((c1_l + c1_r) / 2),
						  c2_m = (short)((c2_l + c2_r) / 2);

					// Write the full left-hand samples (little endian)
					dstPtr[0] = (byte)(c1_l & 0xFF);
					dstPtr[1] = (byte)((c1_l >> 8) & 0xFF);
					dstPtr[2] = (byte)(c2_l & 0xFF);
					dstPtr[3] = (byte)((c2_l >> 8) & 0xFF);

					// Calculate the diffs
					int c1_d = c1_a - c1_m,
						c2_d = c2_a - c2_m;

					// Write the differences as signed bytes
					((sbyte*)dstPtr)[4] = (sbyte)((c1_d < D_MIN) ? D_MIN : (c1_d > D_MAX) ? D_MAX : c1_d);
					((sbyte*)dstPtr)[5] = (sbyte)((c2_d < D_MIN) ? D_MIN : (c2_d > D_MAX) ? D_MAX : c2_d);

					// Save the current right-hand samples as the new left-hand samples
					c1_l = c1_r;
					c2_l = c2_r;
				}

				dataLen = (uint)(dstPtr - (byte*)raw.Data.ToPointer());
			}
			else
			{
				short sl = srcPtr[0];

				for (uint fi = 0; fi < frameCount; fi += 2, srcPtr += 2, dstPtr += 3)
				{
					// Get the actual and right hand samples
					short sa = srcPtr[1],
						  sr = srcPtr[2];

					// Calculate the mean value between the left and right samples
					short sm = (short)((sl + sr) / 2);

					// Write the full left-hand samples (little endian)
					dstPtr[0] = (byte)(sl & 0xFF);
					dstPtr[1] = (byte)((sl >> 8) & 0xFF);

					// Calculate the diffs
					int sd = sa - sm;

					// Write the differences as signed bytes
					((sbyte*)dstPtr)[2] = (sbyte)((sd < D_MIN) ? D_MIN : (sd > D_MAX) ? D_MAX : sd);

					// Save the current right-hand samples as the new left-hand samples
					sl = sr;
				}

				dataLen = (uint)(dstPtr - (byte*)raw.Data.ToPointer());
			}

			// Return the setup
			return new S3RAudio(raw, raw.TakeData(), frameCount, dataLen);
		}
	}
}
