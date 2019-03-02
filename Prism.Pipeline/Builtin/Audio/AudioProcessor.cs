using System;

namespace Prism.Builtin
{
	// Performs the processing of audio data from the raw PCM to the custom compressed format
	[ContentProcessor("AudioProcessor")]
	internal class AudioProcessor : ContentProcessor<RawAudio, ProcessedAudio, AudioWriter>
	{
		// Multiplicitive factor for converting f32 [-1,1] to s16
		private const float F2S_FACTOR = Int16.MaxValue - 1;

		[PipelineParameter(description: "If the processor can use a faster, but slightly lossy compression algorithm.")]
		public bool Lossy = true;

		public unsafe override ProcessedAudio Process(RawAudio input, ProcessorContext ctx)
		{
			// Preprocessing step for mp3, convert from float to s16
			if (input.Format == AudioFormat.Mp3)
				ConvertF32ToS16((short*)input.Data.ToPointer(), (float*)input.Data.ToPointer(), input.SampleCount);

			// Perform the compression steps
			try
			{
				var proc = Lossy ? S3RAudio.Encode(input) : throw new InvalidOperationException("Lossless compression not yet implemented.");
				return proc;
			}
			finally
			{
				//input.Dispose(); // Wont do anything if the encoding functions work properly
			}
		}

		// For MP3, converts float PCM to s16 PCM
		// Since the array writes are always smaller and backwards, these can point to the same parts of memory
		private unsafe static void ConvertF32ToS16(short* dst, float* src, uint samples)
		{
			// Attempt at least a little loop unrolling (look into SIMD or Parallel for this in the future)
			uint loopCount = samples / 16u;
			uint rem = samples - (loopCount * 16u);

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

	// Base class for audio that has been compressed and processed
	internal abstract class ProcessedAudio : IDisposable
	{
		#region Fields
		public readonly AudioFormat Format; // Used to select the correct free function
		public readonly bool Stereo;
		public readonly uint SampleRate;
		public abstract uint FrameCount { get; protected set; }
		public abstract IntPtr Data { get; protected set; }

		protected bool _isDisposed { get; private set; } = false;
		#endregion // Fields

		protected ProcessedAudio(RawAudio raw)
		{
			Format = raw.Format;
			Stereo = raw.Stereo;
			SampleRate = raw.Rate;
		}
		~ProcessedAudio()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (!_isDisposed && (Data != IntPtr.Zero))
			{
				switch (Format)
				{
					case AudioFormat.Wav: NativeAudio.FreeWav(Data); break;
					case AudioFormat.Ogg: NativeAudio.Free(Data); break;
					case AudioFormat.Flac: NativeAudio.FreeFlac(Data); break;
					case AudioFormat.Mp3: NativeAudio.FreeMp3(Data); break;
				}
			}
			_isDisposed = true;
		}
	}
}
