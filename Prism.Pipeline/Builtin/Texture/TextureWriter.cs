using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Prism.Builtin
{
	// Writes image data in an efficient fashion
	internal class TextureWriter : ContentWriter<Image<Rgba32>>
	{
		public override string LoaderName => "Spectrum:TextureLoader";

		public override CompressionPolicy Policy => CompressionPolicy.ReleaseOnly;

		public override void Write(Image<Rgba32> input, ContentStream writer, WriterContext ctx)
		{
			try
			{
				writer.Write((ushort)input.Width);
				writer.Write((ushort)input.Height);

				var frame = input.Frames.RootFrame;
				for (int y = 0; y < frame.Height; ++y)
				{
					for (int x = 0; x < frame.Width; ++x)
					{
						writer.Write(frame[x, y].Rgba);
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
