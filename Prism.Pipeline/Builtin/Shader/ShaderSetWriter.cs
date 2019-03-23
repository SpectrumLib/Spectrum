using System;
using System.Collections.Generic;
using System.Linq;

namespace Prism.Builtin
{
	// Writer type for shader set (.pss) files
	internal class ShaderSetWriter : ContentWriter<PSSInfo>
	{
		private static readonly Dictionary<string, byte> TYPE_MAP = new Dictionary<string, byte>() {
			{ "vert", 0x01 }, { "tesc", 0x02 }, { "tese", 0x04 }, { "geom", 0x08 }, { "frag", 0x10 }
		};

		public override string LoaderName => "Spectrum:ShaderSetLoader";

		public override void Write(PSSInfo input, ContentStream writer, WriterContext ctx)
		{
			writer.Write((uint)input.File.Modules.Count);
			writer.Write((uint)input.File.Shaders.Count);

			Dictionary<string, uint> modMap = input.File.Modules.ToDictionary(mod => mod.Name, mod => (uint)input.File.Modules.IndexOf(mod));

			// Write the shaders as name, stage flags, then stage numbers in the order: vert, tesc, tese, geom, frag
			foreach (var shader in input.File.Shaders)
			{
				writer.Write(shader.Name);
				byte flags = (byte)(
					((shader.Vert != null) ? 0x01 : 0x00) |
					((shader.Tesc != null) ? 0x02 : 0x00) |
					((shader.Tese != null) ? 0x04 : 0x00) |
					((shader.Geom != null) ? 0x08 : 0x00) |
					((shader.Frag != null) ? 0x10 : 0x00));
				writer.Write(flags);
				if (shader.Vert != null)
					writer.Write(modMap[shader.Vert]);
				if (shader.Tesc != null)
					writer.Write(modMap[shader.Tesc]);
				if (shader.Tese != null)
					writer.Write(modMap[shader.Tese]);
				if (shader.Geom != null)
					writer.Write(modMap[shader.Geom]);
				if (shader.Frag != null)
					writer.Write(modMap[shader.Frag]);
			}

			// For each module
			for (int i = 0; i < input.File.Modules.Count; ++i)
			{
				// Write the meta info
				writer.Write(input.File.Modules[i].Name);
				writer.Write(input.File.Modules[i].EntryPoint);
				writer.Write(TYPE_MAP[input.File.Modules[i].Type]);

				// Write the uniform info
				writer.Write((uint)input.Uniforms[i].Length);
				foreach (var u in input.Uniforms[i])
				{
					writer.Write(u.Name);
					writer.Write(u.Type);
					writer.Write(u.ArraySize);
					writer.Write((uint)u.Binding);
					writer.Write(u.Offset);
				}

				// Write the binding info
				writer.Write((uint)input.Bindings[i].Length);
				foreach (var b in input.Bindings[i])
				{
					writer.Write(b.Name);
					writer.Write(b.Type);
					writer.Write(b.Binding);
					writer.Write(b.Size);
				}

				// Copy the spirv bytecode
				writer.CopyFrom(input.AsmFiles[i], writeLen: true);
			}
		}
	}
}
