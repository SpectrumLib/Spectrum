using System;

namespace Prism.Builtin
{
	// Writes image data in an efficient fashion
	internal class TextureWriter : ContentWriter<RawTextureData>
	{
		public override string LoaderName => "Spectrum:TextureLoader";

		public override void Write(RawTextureData input, ContentStream writer, WriterContext ctx)
		{
			throw new NotImplementedException();
		}
	}
}
