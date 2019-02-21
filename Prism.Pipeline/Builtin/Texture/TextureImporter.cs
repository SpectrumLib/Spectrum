using System;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Prism.Builtin
{
	// Implements loading of pixel data from standard image file formats
	[ContentImporter("TextureImporter", typeof(TextureProcessor), ".png", ".jpg", ".jpeg", ".bmp")]
	internal class TextureImporter : ContentImporter<ImportedTextureData>
	{
		private static readonly string[] VALID_FORMATS = {
			PngFormat.Instance.Name, JpegFormat.Instance.Name, BmpFormat.Instance.Name
		};

		public override ImportedTextureData Import(FileStream stream, ImporterContext ctx)
		{
			var ifmt = Image.DetectFormat(stream).Name;
			if (!VALID_FORMATS.Contains(ifmt))
			{
				ctx.Logger.Error($"The image format {ifmt} is not supported by the pipeline.");
				return null;
			}

			return new ImportedTextureData(ifmt, Image.Load(stream));
		}
	}

	internal class ImportedTextureData
	{
		public readonly string Format;
		public readonly Image<Rgba32> Image;

		public ImportedTextureData(string fmt, Image<Rgba32> image)
		{
			Format = fmt;
			Image = image;
		}
	}
}
