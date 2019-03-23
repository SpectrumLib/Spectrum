using System;
using Spectrum.Graphics;
using Vk = VulkanCore;

namespace Spectrum.Content
{
	// Used to load shader sets
	[ContentLoader("ShaderSetLoader")]
	internal class ShaderSetLoader : ContentLoader<ShaderSet>
	{
		public override ShaderSet Load(ContentStream stream, LoaderContext ctx)
		{
			// Read the sizes
			uint modCount = stream.ReadUInt32();
			uint shCount = stream.ReadUInt32();

			// Read the shaders
			ShaderSet.SSShader[] shaders = new ShaderSet.SSShader[shCount];
			for (uint si = 0; si < shCount; ++si)
			{
				shaders[si].Name = stream.ReadString();
				var st = shaders[si].Stages = (ShaderStages)stream.ReadByte();
				shaders[si].Vert = (st & ShaderStages.Vertex) > 0      ? stream.ReadUInt32() : (uint?)null;
				shaders[si].Tesc = (st & ShaderStages.TessControl) > 0 ? stream.ReadUInt32() : (uint?)null;
				shaders[si].Tese = (st & ShaderStages.TessEval) > 0    ? stream.ReadUInt32() : (uint?)null;
				shaders[si].Geom = (st & ShaderStages.Geometry) > 0    ? stream.ReadUInt32() : (uint?)null;
				shaders[si].Frag = (st & ShaderStages.Fragment) > 0    ? stream.ReadUInt32() : (uint?)null;
			}

			// Read the modules
			ShaderSet.SSModule[] mods = new ShaderSet.SSModule[modCount];
			var ldev = SpectrumApp.Instance.GraphicsDevice.VkDevice;
			for (uint mi = 0; mi < modCount; ++mi)
			{
				// Read the meta info
				mods[mi].Name = stream.ReadString();
				mods[mi].EntryPoint = stream.ReadString();
				mods[mi].Stage = (ShaderStages)stream.ReadByte();

				// Read the uniforms
				var ulen = stream.ReadUInt32();
				for (uint ui = 0; ui < ulen; ++ui)
				{
					var name = stream.ReadString();
					var type = stream.ReadUInt32();
					var size = stream.ReadUInt32();
					var bind = stream.ReadUInt32();
					var off = stream.ReadInt32(); // Offset is signed
				}

				// Read the bindings
				var blen = stream.ReadUInt32();
				for (uint bi = 0; bi < blen; ++bi)
				{
					var name = stream.ReadString();
					var type = stream.ReadUInt32();
					var bind = stream.ReadUInt32();
					var size = stream.ReadUInt32();
				}

				// Read the bytecode
				uint len = stream.ReadUInt32();
				if ((len % 4) != 0)
					throw new ContentLoadException(ctx.ItemName, $"SPIR-V bytecode must be a multiple of 4 in length.");
				var code = stream.ReadBytes(len);

				// Build a shader module
				var ci = new Vk.ShaderModuleCreateInfo(code);
				mods[mi].Module = ldev.CreateShaderModule(ci);
			}

			return new ShaderSet(shaders, mods);
		}
	}
}
