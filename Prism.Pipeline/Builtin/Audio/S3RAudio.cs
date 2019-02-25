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
			byte* data = (byte*)raw.Data.ToPointer();
			bool isOdd = (raw.FrameCount % 2) == 1;
			uint encLen = raw.FrameCount - (isOdd ? 0u : 1u); // Can only encode an odd-length stream (tack on last sample at end)

			return new S3RAudio(raw, raw.TakeData(), raw.FrameCount, (uint)raw.DataLength);

			uint dataLen = 0;
			if (raw.Stereo)
			{
				short* dstPtr = (short*)data;
				short c1_l = *(short*)data;
				short c2_l = *(short*)(data + 2);

				for (uint fi = 0; fi < encLen; fi += 2)
				{
					uint soff = fi * 4; // * 2 for stereo frame, and another * 2 for 2-byte samples (since we are accessing byte*)
					byte* fPtr = data + fi; // Pointer to the base of the current frame

					// Actual samples
					short c1_a = *(short*)(fPtr + 4);
					short c2_a = *(short*)(fPtr + 6);
					// Right-hand samples
					short c1_r = *(short*)(fPtr + 8);
					short c2_r = *(short*)(fPtr + 10);
					// Mean samples
					short c1_m = (short)((c1_l + c1_r) / 2);
					short c2_m = (short)((c2_l + c2_r) / 2);

					// Difference
					int c1_d = c1_a - c1_m;
					int c2_d = c2_a - c2_m;

					// Write the full samples
					dstPtr[0] = c1_l;
					dstPtr[1] = c2_l;

					// Encode the diff samples
					*(sbyte*)((byte*)dstPtr + 4) = (sbyte)((c1_d < D_MIN) ? D_MIN : (c1_d > D_MAX) ? D_MAX : c1_d);
					*(sbyte*)((byte*)dstPtr + 5) = (sbyte)((c2_d < D_MIN) ? D_MIN : (c2_d > D_MAX) ? D_MAX : c2_d);

					// Save the old right-hand samples as the new left-hand samples, progress the dst pointer
					c1_l = c1_r;
					c2_l = c2_r;
					dstPtr += 3;
				}

				dataLen = (uint)((byte*)dstPtr - data);
			}
			else
			{
				byte* dstPtr = data;
				short cl = *(short*)data;

				for (uint fi = 0; fi < encLen; fi += 2)
				{
					uint soff = fi * 2; // * 2 for 2-byte samples (since we are accessing byte*)
					byte* fPtr = data + fi; // Pointer to the base of the current frame

					// Actual, right, and mean samples
					short ca = *(short*)(fPtr + 2);
					short cr = *(short*)(fPtr + 4);
					short cm = (short)((cl + cr) / 2);

					// Difference
					int cd = ca - cm;

					// Write the full sample
					*(short*)dstPtr = cl;

					// Encode the diff sample
					*(sbyte*)(dstPtr + 2) = (sbyte)((cd < D_MIN) ? D_MIN : (cd > D_MAX) ? D_MAX : cd);

					// Save the old right-hand sample as the new left-hand sample, progress the dst pointer
					cl = cr;
					dstPtr += 3;
				}

				dataLen = (uint)(dstPtr - data);
			}

			// Return the setup
			return new S3RAudio(raw, raw.TakeData(), encLen, dataLen);
		}
	}
}
