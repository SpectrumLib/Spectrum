using System;

namespace Prism.Builtin
{
	// Performs the processing of audio data from the raw PCM to the custom compressed format
	[ContentProcessor("AudioProcessor")]
	internal class AudioProcessor : ContentProcessor<RawAudio, RLADAudio, AudioWriter>
	{
		// Multiplicitive factor for converting f32 [-1,1] to s16
		private const float F2S_FACTOR = Int16.MaxValue - 1;

		public unsafe override RLADAudio Process(RawAudio input, ProcessorContext ctx)
		{
			// Preprocessing step for mp3, convert from float to s16
			short* data = (short*)input.Data.ToPointer();
			if (input.Format == AudioFormat.Mp3)
				ConvertF32ToS16(data, (float*)input.Data.ToPointer(), input.SampleCount);

			//uint small = 0;
			//uint med = 0;
			//uint smallRun1 = 0;
			//uint medRun1 = 0;
			//uint smallRun2 = 0;
			//uint medRun2 = 0;
			//uint smallRunTotal = 0;
			//uint medRunTotal = 0;
			//uint smallRunCount = 0;
			//uint medRunCount = 0;
			//uint mismatch = 0;
			//for (ulong si = 0; si < (input.SampleCount - 4); si += 4)
			//{
			//	short d1 = (short)Math.Abs(data[si + 2] - data[si]);
			//	short d2 = (short)Math.Abs(data[si + 3] - data[si + 1]);
			//	bool s1 = false, m1 = false, s2 = false, m2 = false;
			//	if (d1 <= 127)
			//	{
			//		++small;
			//		++smallRun1;
			//		s1 = true;
			//	}
			//	else if (d1 <= 2047)
			//	{
			//		++med;
			//		++medRun1;
			//		m1 = true;
			//	}
			//	if (d2 <= 127)
			//	{
			//		++small;
			//		++smallRun2;
			//		s2 = true;
			//	}
			//	else if (d2 <= 2047)
			//	{
			//		++med;
			//		++medRun2;
			//		m2 = true;
			//	}

			//	if ((s1 || s2) && (s1 != s2))
			//		++mismatch;
			//	if ((m1 || m2) && (m1 != m2))
			//		++mismatch;

			//	if (d1 > 127 && smallRun1 != 0)
			//	{
			//		smallRunTotal += smallRun1;
			//		++smallRunCount;
			//	}
			//	if (d1 > 127 && medRun1 != 0)
			//	{
			//		medRunTotal += medRun1;
			//		++medRunCount;
			//	}
			//	if (d2 > 127 && smallRun2 != 0)
			//	{
			//		smallRunTotal += smallRun2;
			//		++smallRunCount;
			//	}
			//	if (d2 > 127 && medRun2 != 0)
			//	{
			//		medRunTotal += medRun2;
			//		++medRunCount;
			//	}
			//}

			//ctx.Logger.Warn($"8-bit diff percentage: {(small / (float)input.SampleCount) * 100:0.000}%.");
			//ctx.Logger.Warn($"12-bit diff percentage: {(med / (float)input.SampleCount) * 100:0.000}%.");

			//ctx.Logger.Warn($"8-bit average run: {smallRunTotal / (float)smallRunCount:0.00}.");
			//ctx.Logger.Warn($"12-bit average run: {medRunTotal / (float)smallRunCount:0.00}.");

			//ctx.Logger.Warn($"Mismatch percentage: {(mismatch / input.SampleCount) * 100:0.000000}%.");

			// Passthrough for now, apply compression soon
			return new RLADAudio();
		}

		// For MP3, converts float PCM to s16 PCM
		// Since the array writes are always smaller and backwards, these can point to the same parts of memory
		private unsafe static void ConvertF32ToS16(short* dst, float* src, uint samples)
		{
			// Attempt at least a little loop unrolling (look into SIMD or Parallel for this in the future)
			uint loopCount = samples / 16u;
			uint rem = samples - loopCount;

			// Unrolled loop
			uint si = 0;
			for (uint lc = 0; lc < loopCount; ++lc, si += 16)
			{
				dst[si+ 0] = (short)(src[si+ 0] * F2S_FACTOR);
				dst[si+ 1] = (short)(src[si+ 1] * F2S_FACTOR);
				dst[si+ 2] = (short)(src[si+ 2] * F2S_FACTOR);
				dst[si+ 3] = (short)(src[si+ 3] * F2S_FACTOR);
				dst[si+ 4] = (short)(src[si+ 4] * F2S_FACTOR);
				dst[si+ 5] = (short)(src[si+ 5] * F2S_FACTOR);
				dst[si+ 6] = (short)(src[si+ 6] * F2S_FACTOR);
				dst[si+ 7] = (short)(src[si+ 7] * F2S_FACTOR);
				dst[si+ 8] = (short)(src[si+ 8] * F2S_FACTOR);
				dst[si+ 9] = (short)(src[si+ 9] * F2S_FACTOR);
				dst[si+10] = (short)(src[si+10] * F2S_FACTOR);
				dst[si+11] = (short)(src[si+11] * F2S_FACTOR);
				dst[si+12] = (short)(src[si+12] * F2S_FACTOR);
				dst[si+13] = (short)(src[si+13] * F2S_FACTOR);
				dst[si+14] = (short)(src[si+14] * F2S_FACTOR);
				dst[si+15] = (short)(src[si+15] * F2S_FACTOR);
			}

			// Remaining samples
			for (uint rc = 0; rc < rem; ++rc, ++si)
				dst[si] = (short)(src[si] * F2S_FACTOR);
		}
	}
}
