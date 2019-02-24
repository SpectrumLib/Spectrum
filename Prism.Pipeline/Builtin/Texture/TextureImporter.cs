using System;
using System.IO;

namespace Prism.Builtin
{
	// Implements loading of pixel data from standard image file formats
	[ContentImporter("TextureImporter", typeof(TextureProcessor), "png", "jpg", "jpeg", "bmp", "tga")]
	internal class TextureImporter : ContentImporter<ImageData>
	{
		public override ImageData Import(FileStream stream, ImporterContext ctx)
		{
			// Ensure it is a supported image type
			switch (ctx.FileExtension)
			{
				case ".png":
				case ".jpg":
				case ".jpeg":
				case ".bmp":
				case ".tga": break;
				default:
					ctx.Logger.Error($"unsupported image file format '{ctx.FileExtension.Substring(1)}'.");
					return null;
			}

			// Load as a 4-channel rgba
			return NativeImage.Load(ctx.FilePath);
		}
	}
}
