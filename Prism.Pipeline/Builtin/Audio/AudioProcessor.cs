using System;

namespace Prism.Builtin
{
	// Performs the processing of audio data from the raw PCM to the custom compressed format
	[ContentProcessor("AudioProcessor")]
	internal class AudioProcessor : ContentProcessor<RawAudio, ProcessedAudio, AudioWriter>
	{
		// Multiplicitive factor for converting f32 [-1,1] to s16
		private const float F2S_FACTOR = Int16.MaxValue - 1;

		[PipelineParameter(name: "lossy", description: "If the processor can use a faster, but slightly lossy compression algorithm.")]
		public bool Lossy = true;

		public unsafe override ProcessedAudio Process(RawAudio input, ProcessorContext ctx)
		{
			// Preprocessing step for mp3, convert from float to s16
			if (input.Format == AudioFormat.Mp3)
				ConvertF32ToS16((short*)input.Data.ToPointer(), (float*)input.Data.ToPointer(), input.SampleCount);

			// Perform the compression steps
			try
			{
				var proc = Lossy ? FSRAudio.Encode(input, ctx.UseStats, ctx.Logger) : throw new InvalidOperationException("Lossless compression not yet implemented.");
				return proc;
			}
			finally
			{
				input.Dispose(); // Wont do anything if the encoding functions work properly
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
			for (uint lc = 0; lc < loopCount; ++lc, src += 16, dst += 16)
			{
				dst[ 0] = (short)(src[ 0] * F2S_FACTOR);
				dst[ 1] = (short)(src[ 1] * F2S_FACTOR);
				dst[ 2] = (short)(src[ 2] * F2S_FACTOR);
				dst[ 3] = (short)(src[ 3] * F2S_FACTOR);
				dst[ 4] = (short)(src[ 4] * F2S_FACTOR);
				dst[ 5] = (short)(src[ 5] * F2S_FACTOR);
				dst[ 6] = (short)(src[ 6] * F2S_FACTOR);
				dst[ 7] = (short)(src[ 7] * F2S_FACTOR);
				dst[ 8] = (short)(src[ 8] * F2S_FACTOR);
				dst[ 9] = (short)(src[ 9] * F2S_FACTOR);
				dst[10] = (short)(src[10] * F2S_FACTOR);
				dst[11] = (short)(src[11] * F2S_FACTOR);
				dst[12] = (short)(src[12] * F2S_FACTOR);
				dst[13] = (short)(src[13] * F2S_FACTOR);
				dst[14] = (short)(src[14] * F2S_FACTOR);
				dst[15] = (short)(src[15] * F2S_FACTOR);
			}

			// Remaining samples
			for (uint rc = 0; rc < rem; ++rc, ++src, ++dst)
				dst[0] = (short)(src[0] * F2S_FACTOR);
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
