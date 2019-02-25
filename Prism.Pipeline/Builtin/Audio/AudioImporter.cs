using System;
using System.IO;

namespace Prism.Builtin
{
	[ContentImporter("Audio Importer", typeof(object), "wav", "flac", "ogg", "mp3")]
	internal class AudioImporter : ContentImporter<RawAudio>
	{
		public override RawAudio Import(FileStream stream, ImporterContext ctx)
		{
			// Select loader based on the extension
			switch (ctx.FileExtension)
			{
				case ".wav": return NativeAudio.LoadWave(ctx.FilePath);
				case ".flac": return NativeAudio.LoadFlac(ctx.FilePath);
				case ".ogg": return NativeAudio.LoadVorbis(ctx.FilePath);
				case ".mp3": return NativeAudio.LoadMp3(ctx.FilePath);
				default:
					ctx.Logger.Error($"unsupported audio file format '{ctx.FileExtension.Substring(1)}'.");
					return null;
			}
		}
	}
}
