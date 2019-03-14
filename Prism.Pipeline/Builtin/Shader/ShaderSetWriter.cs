using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Prism.Builtin
{
	// Writer type for shader set (.pss) files
	internal class ShaderSetWriter : ContentWriter<PSSInfo>
	{
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

			// For each module, write the bytecode length, and then copy the compiled bytecode in from the temp files
			byte[] tmp = new byte[8192];
			for (int i = 0; i < input.File.Modules.Count; ++i)
			{
				FileInfo fi = new FileInfo(input.AsmFiles[i]);
				writer.Write(input.File.Modules[i].Name);
				writer.Write((uint)fi.Length);

				// Copy the bytecode into the output file
				int rem = (int)fi.Length;
				using (var reader = fi.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					while (rem > 0)
					{
						int amt = reader.Read(tmp, 0, Math.Min(tmp.Length, rem));
						writer.Write(tmp, 0, (uint)amt);
						rem -= amt;
					}
				}
			}
		}
	}
}
