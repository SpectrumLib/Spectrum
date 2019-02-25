using System;
using System.IO;

namespace Prism.Builtin
{
	// Loads audio data as raw PCM data for use in sound effects or songs
	[ContentImporter("AudioImporter", typeof(AudioProcessor), "wav", "flac", "ogg", "mp3")]
	internal class AudioImporter : ContentImporter<RawAudio>
	{
		public override RawAudio Import(FileStream stream, ImporterContext ctx)
		{
			// Select loader based on the extension
			RawAudio ra = null;
			switch (ctx.FileExtension)
			{
				case ".wav": ra = NativeAudio.LoadWave(ctx.FilePath); break;
				case ".flac": ra = NativeAudio.LoadFlac(ctx.FilePath); break;
				case ".ogg": ra = NativeAudio.LoadVorbis(ctx.FilePath); break;
				case ".mp3": ra = NativeAudio.LoadMp3(ctx.FilePath); break;
				default:
					ctx.Logger.Error($"unsupported audio file format '{ctx.FileExtension.Substring(1)}'.");
					return null;
			}

			// Good to move forward
			return ra;
		}
	}
}
