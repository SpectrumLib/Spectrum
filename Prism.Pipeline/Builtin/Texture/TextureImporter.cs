using System;
using System.IO;
using System.Linq;

namespace Prism.Builtin
{
	// Implements loading of pixel data from standard image file formats
	[ContentImporter("TextureImporter", typeof(TextureProcessor))]
	internal class TextureImporter : ContentImporter<object>
	{
		public override object Import(FileStream stream, ImporterContext ctx)
		{
			return null;
		}
	}
}
