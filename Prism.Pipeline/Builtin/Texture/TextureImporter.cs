using System;
using System.IO;

namespace Prism.Builtin
{
	// Implements loading of pixel data from standard image file formats
	[ContentImporter("TextureImporter", typeof(TextureProcessor), ".png", ".jpg", ".jpeg", ".gif", ".bmp")]
	internal class TextureImporter : ContentImporter<RawTextureData>
	{
		public override RawTextureData Import(FileStream stream, ImporterContext ctx)
		{
			throw new NotImplementedException();
		}
	}
}
