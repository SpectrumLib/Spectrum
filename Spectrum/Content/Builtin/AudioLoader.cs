using System;
using System.Runtime.InteropServices;
using Spectrum.Audio;

namespace Spectrum.Content
{
	// Used to load sound effects and songs
	[ContentLoader("AudioLoader")]
	internal class AudioLoader : ContentLoader<SoundEffect>
	{
		public unsafe override SoundEffect Load(ContentStream stream, LoaderContext ctx)
		{
			uint frameCount = stream.ReadUInt32() - 1;
			uint sampleRate = stream.ReadUInt32();
			bool isStereo = stream.ReadBoolean();
			bool isLossy = stream.ReadBoolean();

			if (!isLossy)
				throw new NotImplementedException("Loading lossless audio data not yet implemented.");

			uint fullLen = frameCount * 2 * (isStereo ? 2u : 1u);
			var data = Marshal.AllocHGlobal((int)fullLen);

			try
			{
				FSRStream.ReadSamples(stream, (short*)data.ToPointer(), frameCount, isStereo);

				var sb = new SoundBuffer();
				sb.SetData(data, isStereo ? AudioFormat.Stereo16 : AudioFormat.Mono16, sampleRate, fullLen);
				return new SoundEffect(sb);
			}
			finally
			{
				Marshal.FreeHGlobal(data);
			}
		}
	}
}
