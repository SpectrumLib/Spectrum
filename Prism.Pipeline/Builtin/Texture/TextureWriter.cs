using System;

namespace Prism.Builtin
{
	// Writes image data in an efficient fashion
	internal class TextureWriter : ContentWriter<ImageData>
	{
		public override string LoaderName => "Spectrum:TextureLoader";

		public override CompressionPolicy Policy => CompressionPolicy.Always;

		public override void Write(ImageData input, ContentStream writer, WriterContext ctx)
		{
			try
			{
				writer.Write((ushort)input.Width);
				writer.Write((ushort)input.Height);

				// TODO: Write pixel data
			}
			finally
			{
				input.Dispose();
			}
		}
	}
}
