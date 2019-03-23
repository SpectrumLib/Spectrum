using System;
using System.Collections.Generic;
using System.Linq;
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
			var uniforms = new List<Unif[]>((int)modCount);
			var bindings = new List<Bind[]>((int)modCount);
			var ldev = SpectrumApp.Instance.GraphicsDevice.VkDevice;
			for (uint mi = 0; mi < modCount; ++mi)
			{
				// Read the meta info
				mods[mi].Name = stream.ReadString();
				mods[mi].EntryPoint = stream.ReadString();
				mods[mi].Stage = (ShaderStages)stream.ReadByte();

				// Read the uniforms
				var ulen = stream.ReadUInt32();
				var unis = new Unif[ulen];
				for (uint ui = 0; ui < ulen; ++ui)
				{
					unis[ui] = new Unif(
						stream.ReadString(),
						stream.ReadUInt32(),
						stream.ReadUInt32(),
						stream.ReadUInt32(),
						stream.ReadInt32()
					);
				}
				uniforms.Add(unis);

				// Read the bindings
				var blen = stream.ReadUInt32();
				var binds = new Bind[blen];
				for (uint bi = 0; bi < blen; ++bi)
				{
					binds[bi] = new Bind(
						stream.ReadString(),
						stream.ReadUInt32(),
						stream.ReadUInt32(),
						stream.ReadUInt32()
					);
				}
				bindings.Add(binds);

				// Read the bytecode
				uint len = stream.ReadUInt32();
				if ((len % 4) != 0)
					throw new ContentLoadException(ctx.ItemName, $"SPIR-V bytecode must be a multiple of 4 in length.");
				var code = stream.ReadBytes(len);

				// Build a shader module
				var ci = new Vk.ShaderModuleCreateInfo(code);
				mods[mi].Module = ldev.CreateShaderModule(ci);
			}

			// Build the uniform sets for each shader
			for (int si = 0; si < shaders.Length; ++si)
			{
				ref var shdr = ref shaders[si];
				var sunis = new List<Unif>();
				var sbnds = new List<Bind>();

				// Get the uniforms and bindings from all stages
				if (shdr.Vert.HasValue)
				{
					sunis.AddRange(uniforms[(int)shdr.Vert.Value]);
					sbnds.AddRange(bindings[(int)shdr.Vert.Value]);
				}
				if (shdr.Tesc.HasValue)
				{
					sunis.AddRange(uniforms[(int)shdr.Tesc.Value]);
					sbnds.AddRange(bindings[(int)shdr.Tesc.Value]);
				}
				if (shdr.Tese.HasValue)
				{
					sunis.AddRange(uniforms[(int)shdr.Tese.Value]);
					sbnds.AddRange(bindings[(int)shdr.Tese.Value]);
				}
				if (shdr.Geom.HasValue)
				{
					sunis.AddRange(uniforms[(int)shdr.Geom.Value]);
					sbnds.AddRange(bindings[(int)shdr.Geom.Value]);
				}
				if (shdr.Frag.HasValue)
				{
					sunis.AddRange(uniforms[(int)shdr.Frag.Value]);
					sbnds.AddRange(bindings[(int)shdr.Frag.Value]);
				}

				// Build the uniforms set
				var uset = BuildShaderUniformSet(shdr, sunis, sbnds);
			}

			return new ShaderSet(shaders, mods);
		}

		private static UniformSet BuildShaderUniformSet(in ShaderSet.SSShader shdr, List<Unif> uniforms, List<Bind> bindings)
		{
			var maxb = bindings.Max(b => b.Binding);

			// Get the bindings in order
			var sbinds = new UniformSet.Binding[maxb];
			return null;
		}

		// Temp type for storing read uniform data
		private struct Unif
		{
			public string Name;
			public uint Type;
			public uint Size;
			public uint Binding;
			public int Offset;

			public Unif(string n, uint t, uint s, uint b, int o)
			{
				Name = n;
				Type = t;
				Size = s;
				Binding = b;
				Offset = o;
			}
		}

		// Temp type for storing read binding data
		private struct Bind
		{
			public string Name;
			public uint Type;
			public uint Binding;
			public uint Size;

			public bool IsBlock => Type == 0xFFFFFFFF;

			public Bind(string n, uint t, uint b, uint s)
			{
				Name = n;
				Type = t;
				Binding = b;
				Size = s;
			}
		}
	}
}
