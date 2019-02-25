using System;

namespace Prism.Builtin
{
	// Writes audio data
	internal class AudioWriter : ContentWriter<RawAudio>
	{
		public override string LoaderName => "Spectrum:AudioLoader";

		public override void Write(RawAudio input, ContentStream writer, WriterContext ctx)
		{
			try
			{
				writer.Write(input.FrameCount);
				writer.Write(input.ChannelCount == 2); // true = stereo, false = mono
				writer.Write(input.Rate);

				unsafe
				{
					ulong len = (input.Format == AudioFormat.Mp3) ? input.DataLength / 2 : input.DataLength;
					writer.Write((byte*)input.Data, (uint)len);
				}
			}
			finally
			{
				input.Dispose();
			}
		}
	}
}
