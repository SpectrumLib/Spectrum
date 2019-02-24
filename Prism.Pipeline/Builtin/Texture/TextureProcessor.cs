using System;

namespace Prism.Builtin
{
	// Currently just a passthrough processor for image data, but will soon implement standard transforms
	[ContentProcessor("TextureProcessor")]
	internal class TextureProcessor : ContentProcessor<ImageData, ImageData, TextureWriter>
	{
		public override ImageData Process(ImageData input, ProcessorContext ctx)
		{
			return input;
		}
	}
}
