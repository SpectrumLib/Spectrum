using System;

namespace Prism.Builtin
{
	// Writes image data in an efficient fashion
	internal class TextureWriter : ContentWriter<object>
	{
		public override string LoaderName => "Spectrum:TextureLoader";

		public override CompressionPolicy Policy => CompressionPolicy.Always;

		public override void Write(object input, ContentStream writer, WriterContext ctx)
		{

		}
	}
}
