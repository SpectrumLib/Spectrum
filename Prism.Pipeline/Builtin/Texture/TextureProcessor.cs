using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Prism.Builtin
{
	// Currently just a passthrough processor for image data, but will soon implement standard transforms
	[ContentProcessor("TextureProcessor")]
	internal class TextureProcessor : ContentProcessor<ImportedTextureData, Image<Rgba32>, TextureWriter>
	{
		public override Image<Rgba32> Process(ImportedTextureData input, ProcessorContext ctx)
		{
			if (input.Image.Width > UInt16.MaxValue || input.Image.Height > UInt16.MaxValue)
			{
				ctx.Logger.Error($"cannot process textures larger than {UInt16.MaxValue} ({input.Image.Width}x{input.Image.Height}).");
				return null;
			}

			return input.Image;
		}
	}
}
