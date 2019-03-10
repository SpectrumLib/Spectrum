using System;
using System.Runtime.InteropServices;
using Spectrum.Audio;

namespace Spectrum.Content
{
	// Used to load sound effects and songs
	[ContentLoader("AudioLoader")]
	internal class AudioLoader : ContentLoader<IAudioSource>
	{
		private static readonly Type SONG_TYPE = typeof(Song);
		private static readonly Type SOUNDEFFECT_TYPE = typeof(SoundEffect);

		public unsafe override IAudioSource Load(ContentStream stream, LoaderContext ctx)
		{
			uint frameCount = stream.ReadUInt32();
			uint sampleRate = stream.ReadUInt32();
			bool isStereo = stream.ReadBoolean();
			bool isLossy = stream.ReadBoolean();

			if (ctx.ContentType == SOUNDEFFECT_TYPE)
			{
				uint fullLen = frameCount * 2 * (isStereo ? 2u : 1u);
				var data = Marshal.AllocHGlobal((int)fullLen);

				try
				{
					if (isLossy) FSRStream.ReadSamples(stream.Reader, (short*)data.ToPointer(), frameCount - 1, isStereo);
					else RLADStream.DecodeAll(stream, (short*)data.ToPointer(), isStereo, frameCount);

					var sb = new SoundBuffer();
					sb.SetData(data, isStereo ? AudioFormat.Stereo16 : AudioFormat.Mono16, sampleRate, fullLen);
					return new SoundEffect(sb);
				}
				finally
				{
					Marshal.FreeHGlobal(data);
				} 
			}
			else // Song
			{
				if (!isLossy)
					throw new NotImplementedException($"Cannot use lossless data in Songs (yet).");

				var astream = new FSRStream(stream.FilePath, stream.CurrentOffset, isStereo, frameCount);
				return new Song(astream, sampleRate);
			}
		}
	}

	// Dummy type implemented by SoundEffect and Song to allow them both to be loaded by the same loader. This type
	//   may be formalized and used in a real capacity in the future.
	internal interface IAudioSource { }
}
