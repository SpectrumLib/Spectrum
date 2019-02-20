using System;

namespace Prism.Builtin
{
	// Currently just a passthrough processor for image data, but will soon implement standard transforms
	[ContentProcessor("TextureProcessor")]
	internal class TextureProcessor : ContentProcessor<RawTextureData, RawTextureData, TextureWriter>
	{
		public override RawTextureData Process(RawTextureData input, ProcessorContext ctx)
		{
			throw new NotImplementedException();
		}
	}
}
