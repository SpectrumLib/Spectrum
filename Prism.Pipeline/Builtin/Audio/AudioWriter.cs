using System;

namespace Prism.Builtin
{
	// Writes audio data
	internal class AudioWriter : ContentWriter<ProcessedAudio>
	{
		public override string LoaderName => "Spectrum:AudioLoader";

		public override CompressionPolicy Policy => CompressionPolicy.Never;

		public unsafe override void Write(ProcessedAudio input, ContentStream writer, WriterContext ctx)
		{
			try
			{
				writer.Write(input.FrameCount);
				writer.Write(input.SampleRate);
				writer.Write(input.Stereo);
				writer.Write(input is S3RAudio); // true = lossy, false = lossless

				if (input is S3RAudio)
					writer.Write((byte*)input.Data.ToPointer(), (input as S3RAudio).DataLength);
				else
					throw new InvalidOperationException("Writing uncompressed audio is not yet implmeneted.");
			}
			finally
			{
				input.Dispose();
			}
		}
	}
}
