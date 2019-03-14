using System;
using Spectrum.Graphics;

namespace Spectrum.Content
{
	// Used to load shader sets
	[ContentLoader("ShaderSetLoader")]
	internal class ShaderSetLoader : ContentLoader<object>
	{
		public override object Load(ContentStream stream, LoaderContext ctx)
		{
			// Read the sizes
			uint modCount = stream.ReadUInt32();
			uint shCount = stream.ReadUInt32();

			// Read the shaders
			ShaderSet.SSShader[] shaders = new ShaderSet.SSShader[shCount];
			for (uint si = 0; si < shCount; ++si)
			{
				shaders[si].Name = stream.ReadString();
				var st = shaders[si].Stages = (ShaderStage)stream.ReadByte();
				shaders[si].Vert = (st & ShaderStage.Vertex) > 0 ? stream.ReadUInt32() : 0;
				shaders[si].Tesc = (st & ShaderStage.TessControl) > 0 ? stream.ReadUInt32() : 0;
				shaders[si].Tese = (st & ShaderStage.TessEval) > 0 ? stream.ReadUInt32() : 0;
				shaders[si].Geom = (st & ShaderStage.Geometry) > 0 ? stream.ReadUInt32() : 0;
				shaders[si].Frag = (st & ShaderStage.Fragment) > 0 ? stream.ReadUInt32() : 0;
			}

			// Read the modules
			ShaderSet.SSModule[] mods = new ShaderSet.SSModule[modCount];
			for (uint mi = 0; mi < modCount; ++mi)
			{
				mods[mi].Name = stream.ReadString();
				uint len = stream.ReadUInt32();
				if ((len % 4) != 0)
					throw new ContentLoadException(ctx.ItemName, $"SPIR-V bytecode must be a multiple of 4 in length.");
				mods[mi].ByteCode = stream.ReadBytes(len);
			}

			return null;
		}
	}
}
