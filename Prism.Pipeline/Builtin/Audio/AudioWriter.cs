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
				writer.Write(input is FSRAudio); // true = lossy, false = lossless

				if (input is FSRAudio)
					writer.Write((byte*)input.Data.ToPointer(), (input as FSRAudio).DataLength);
				else
				{
					byte* data = (byte*)input.Data.ToPointer();
					RLADAudio rlad = input as RLADAudio;
					uint lmod = input.Stereo ? 2u : 1;

					uint len = 0;
					for (uint ci = 0; ci < rlad.ChunkCount; ++ci, data += len)
					{
						var stype = rlad.Chunks[ci].Type;
						var extra = rlad.Chunks[ci].Extra;
						var head = ((stype & 0x03) << 6) | (extra & 0x3F);
						writer.Write((byte)(head & 0xFF));

						// First calculate the length assuming one chunk of mono data, then correct for stereo and chunk count
						len = (stype == RLADAudio.SMALL_TYPE) ? 8u : (stype == RLADAudio.MED_TYPE) ? 12u : 16u;
						len *= (lmod * (extra + 1u) * 8u);

						writer.Write(data, len);
					}
				}
			}
			finally
			{
				input.Dispose();
			}
		}
	}
}
