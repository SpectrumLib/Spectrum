using System;

namespace Prism.Builtin
{
	// Currently just a passthrough processor for image data, but will soon implement standard transforms
	[ContentProcessor("TextureProcessor")]
	internal class TextureProcessor : ContentProcessor<object, object, TextureWriter>
	{
		public override object Process(object input, ProcessorContext ctx)
		{
			return null;
		}
	}
}
