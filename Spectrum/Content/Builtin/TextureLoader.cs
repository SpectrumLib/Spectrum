using System;
using Spectrum.Graphics;

namespace Spectrum.Content
{
	// Used to load 2D textures that have been procuded by the default TextureProcessor
	[ContentLoader("TextureLoader")]
	internal class TextureLoader : ContentLoader<Texture2D>
	{
		public override Texture2D Load(ContentStream stream, LoaderContext ctx)
		{
			// Read the ushort dimensions
			uint w = stream.ReadUInt16();
			uint h = stream.ReadUInt16();
			uint count = w * h;
			if ((count * 4) != stream.Remaining)
				throw new Exception($"the expected and available texture data lengths do not match ({count * 4} != {stream.Remaining}).");

			// Read in the entirety of the pixel data  (stored as RGBA packed uints)
			var data = new uint[count];
			for (uint i = 0; i < count; ++i)
				data[i] = stream.ReadUInt32();

			// Create the texture, upload the pixels, and return
			Texture2D tex = new Texture2D(w, h);
			tex.SetData(data);
			return tex;
		}
	}
}
