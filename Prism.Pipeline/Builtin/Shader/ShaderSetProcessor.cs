using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Prism.Builtin
{
	// Performs the compiling and checking for shader modules and shader set files
	[ContentProcessor("ShaderSetProcessor")]
	internal class ShaderSetProcessor : ContentProcessor<PSSFile, PSSInfo, ShaderSetWriter>
	{
		private static readonly Dictionary<string, string> ENTRY_POINT_TYPE_MAP = new Dictionary<string, string> {
			{ "vert", "Vertex" }, { "tesc", "TessellationControl" }, { "tese", "TessellationEvaluation" }, { "geom", "Geometry" },
			{ "frag", "Fragment" }
		};

		public override PSSInfo Process(PSSFile input, ProcessorContext ctx)
		{
			// Compile the modules
			List<string> asmFiles = new List<string>();
			List<string[]> reflect = new List<string[]>();
			List<string[]> spirv = new List<string[]>();
			foreach (var mod in input.Modules)
			{
				string of = ctx.GetTempFile();
				asmFiles.Add(of);
				if (!GLSLV.CompileModule(mod, ctx.FileDirectory, of, ctx.Logger, out string[] refl, out string[] sprv))
					return null;
				reflect.Add(refl);
				spirv.Add(sprv);
			}

			// Validate the entry points
			for (int i = 0; i < spirv.Count; ++i)
			{
				if (!ValidateEntryPoint(input.Modules[i], spirv[i], ctx.Logger))
					return null;
			}

			// Parse the vertex attributes and uniforms
			List<VertexAttrib[]> attrs = new List<VertexAttrib[]>(reflect.Count);
			List<Uniform[]> unifs = new List<Uniform[]>(reflect.Count);
			List<UniformBinding[]> bindings = new List<UniformBinding[]>(reflect.Count);
			for (int i = 0; i < reflect.Count; ++i)
			{
				if (!ParseVertexAttribs(input.Modules[i], reflect[i], spirv[i], out var atts, ctx.Logger))
					return null;
				if (!ParseUniforms(input.Modules[i], reflect[i], spirv[i], out var unis, out var binds, ctx.Logger))
					return null;
				attrs.Add(atts);
				unifs.Add(unis);
				bindings.Add(binds);

				if (ctx.UseStats)
				{
					ctx.LStats($"Module '{input.Modules[i].Name}':    {input.Modules[i].Type} @{input.Modules[i].EntryPoint}");
					ctx.LStats($"    > Vertex Attrs:  {String.Join(",  ", atts.Select(att => $"{att.Name} (t={att.Type:X} l={att.Location})"))}");
					ctx.LStats($"    > Uniforms:  {String.Join(",  ", unis.Select(uni => $"{uni.Name} (t={uni.Type:X}[{uni.Size}] o={uni.Offset} b={uni.Binding})"))}");
					ctx.LStats($"    > Bindings:  {String.Join(",  ", binds.Select(bnd => $"{bnd.Name} (t={bnd.Type:X} s={bnd.Size} b={bnd.Binding}"))})");
				}
			}

			return new PSSInfo { File = input, AsmFiles = asmFiles };
		}

		private static bool ValidateEntryPoint(in PSSModule mod, string[] spirv, PipelineLogger logger)
		{
			// Find the line
			var epi = Array.FindIndex(spirv, line => line.StartsWith("EntryPoint"));
			if (epi == -1)
			{
				logger.Error($"The module '{mod.Name}' does not contain an entry point in the SPIR-V.");
				return false;
			}

			// Parse the line
			var split = spirv[epi].Split(new [] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			var stage = split[1];
			var name = split[3].Substring(1, split[3].Length - 2); // Surrounded by quotes, remove these with substr

			// Validate the entry point name
			if (name != mod.EntryPoint)
			{
				logger.Error($"The module '{mod.Name}' does not contain an entry point with the name '{mod.EntryPoint}'.");
				return false;
			}

			// Validate the entry point stage
			if (stage != ENTRY_POINT_TYPE_MAP[mod.Type])
			{
				logger.Error($"The specified entry point for module '{mod.Name}' does not match the requested stage ({mod.EntryPoint}, {mod.Type}).");
				return false;
			}

			// Entry point is good
			return true;
		}

		private static bool ParseVertexAttribs(in PSSModule mod, string[] refl, string[] spirv, out VertexAttrib[] attrs, PipelineLogger logger)
		{
			attrs = null;

			// Find the line
			var ridx = Array.FindIndex(refl, line => line.StartsWith("Vertex attr"));
			if (ridx == -1)
			{
				logger.Error($"The module '{mod.Name}' does not have a vertex attribute reflection entry.");
				return false;
			}

			// Assign and check for early exit
			attrs = new VertexAttrib[refl.Length - ridx - 1];
			if (attrs.Length == 0)
				return true;

			// Pull the attribs
			for (int i = ridx + 1, ai = 0; i < refl.Length; ++i, ++ai)
			{
				var split = refl[i].Split(',');
				var name = split[0].Substring(0, split[0].IndexOf(':'));
				var tstr = split[1].Trim();
				tstr = tstr.Substring(tstr.IndexOf(' ') + 1);
				if (!UInt32.TryParse(tstr, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint type))
				{
					logger.Error($"The module '{mod.Name}' does not have a valid hex type specifier for the attribute '{name}'.");
					return false;
				}
				attrs[ai] = new VertexAttrib { Name = name, Type = type, Location = UInt32.MaxValue };
			}

			// Parse the spirv for the locations
			var decs = Array.FindAll(spirv, line => line.StartsWith("Decorate "));
			foreach (var line in decs)
			{
				var split = line.Split(' ');
				if (split[2] != "Location") // Not a Location decoration
					continue;

				var name = split[1].Substring(split[1].IndexOf('(') + 1).TrimEnd(')');
				var aidx = Array.FindIndex(attrs, att => att.Name == name);
				if (aidx != -1) // Vertex attribs arent the only names decorated with "Location"
				{
					if (!UInt32.TryParse(split[3], out uint loc))
					{
						logger.Error($"The module '{mod.Name}' did not specify a valid location for the attribute '{name}'.");
						return false;
					}
					attrs[aidx].Location = loc;
				}
			}

			// Make sure all attribs have a location
			foreach (var attr in attrs)
			{
				if (attr.Location == UInt32.MaxValue)
				{
					logger.Error($"The module '{mod.Name}' did not specify a location for attribute '{attr.Name}'.");
					return false;
				}
			}

			// Good to go
			return true;
		}

		private static bool ParseUniforms(in PSSModule mod, string[] refl, string[] spirv, out Uniform[] unis, out UniformBinding[] binds, PipelineLogger logger)
		{
			unis = null;
			binds = null;

			// The the lines
			var uidx = Array.FindIndex(refl, line => line.StartsWith("Uniform re"));
			var bidx = Array.FindIndex(refl, uidx, line => line.StartsWith("Uniform bl"));
			var ridx = Array.FindIndex(refl, bidx, line => line.StartsWith("Vertex at"));
			if (uidx == -1)
			{
				logger.Error($"The module '{mod.Name}' did not have a uniform reflection entry.");
				return false;
			}
			if (bidx == -1)
			{
				logger.Error($"The module '{mod.Name}' did not have a uniform block reflection entry.");
				return false;
			}
			var ulen = bidx - uidx - 1;
			var blen = ridx - bidx - 1;

			// Extract the blocks
			(string Name, uint Binding, uint Size, string[] Members)[] blocks = new (string, uint, uint, string[])[blen];
			for (int bi = 0; bi < blen; ++bi)
			{
				// Get block info from reflection dump
				var bsplit = refl[bidx + bi + 1].Split(':', ',');
				var name = bsplit[0];
				var size = UInt32.Parse(bsplit[3].Substring(bsplit[3].IndexOf(' ', 1)));
				var binding = UInt32.Parse(bsplit[5].Substring(bsplit[5].IndexOf(' ', 1)));
				blocks[bi] = (name, binding, size, null);

				// Get the block variable number
				var nidx = Array.FindIndex(spirv, line => line.StartsWith("Name") && line.EndsWith($"\"{name}\""));
				if (nidx == -1)
				{
					logger.Error($"The module '{mod.Name}' did not provide a spirv name for the block '{name}'.");
					return false;
				}
				nidx = Int32.Parse(spirv[nidx].Split(' ')[1]);

				// Find the descriptor set number
				var didx = Array.FindIndex(spirv, line => line == $"Decorate {nidx}({name}) Block");
				if (didx == -1 || didx == (spirv.Length - 1) || !spirv[didx + 1].Contains("DescriptorSet"))
				{
					logger.Error($"The module '{mod.Name}' did not provide a descriptor set index for the block '{name}'.");
					return false;
				}
				var dsi = UInt32.Parse(spirv[didx + 1].Split(' ')[3]);
				if (dsi != 0)
				{
					logger.Error($"The module '{mod.Name}' must assign the block '{name}' to descriptor set 0 (layout(set = 0, ...)).");
					return false;
				}

				// Get the block members
				var bmems = Array.FindAll(spirv, line => line.StartsWith($"MemberName {nidx}("));
				blocks[bi].Members = new string[bmems.Length];
				for (int mi = 0; mi < bmems.Length; ++mi)
				{
					var msplit = bmems[mi].Split(' ');
					blocks[bi].Members[mi] = msplit[msplit.Length - 1].Trim('"');
				}
			}

			// Extract the reflected uniforms
			unis = new Uniform[ulen];
			uint bcount = 0;
			for (int ui = 0; ui < ulen; ++ui)
			{
				// Get the uniform info
				var usplit = refl[uidx + ui + 1].Split(':', ',');
				var name = usplit[0];
				var offset = Int32.Parse(usplit[1].Substring(usplit[1].IndexOf(' ', 1)));
				var type = UInt32.Parse(usplit[2].Substring(usplit[2].IndexOf(' ', 1)), NumberStyles.HexNumber);
				var size = UInt32.Parse(usplit[3].Substring(usplit[3].IndexOf(' ', 1)));
				var binding = Int32.Parse(usplit[5].Substring(usplit[5].IndexOf(' ', 1)));
				unis[ui] = new Uniform { Name = name, Type = type, Offset = offset, Binding = binding, Size = size, Block = null };

				// If it is in a block, we need to find the binding point for the block
				if (binding == -1)
				{
					var bi = Array.FindIndex(blocks, b => Array.Exists(b.Members, mname => mname == name));
					if (bi == -1)
					{
						logger.Error($"The module '{mod.Name}' did not supply a block mapping for the uniform '{name}'.");
						return false;
					}
					unis[ui].Block = blocks[bi].Name;
					unis[ui].Binding = (int)blocks[bi].Binding;
					bcount += 1;
				}
			}

			// Generate the bindings
			binds = new UniformBinding[blen + (ulen - bcount)]; // One binding for each block AND opaque handle
			uint bindi = 0;
			foreach (var b in blocks)
			{
				binds[bindi++] = new UniformBinding { Name = b.Name, Type = 0xFFFFFFFF, Binding = b.Binding, Size = b.Size };
			}
			foreach (var u in unis)
			{
				if (u.Offset == -1)
					binds[bindi++] = new UniformBinding { Name = u.Name, Type = u.Type, Binding = (uint)u.Binding, Size = 4 };
			}
			Array.Sort(binds, (b1, b2) => b1.Binding.CompareTo(b2.Binding)); // Sort in ascending order by binding

			return true;
		}
	}

	// Used to pass processed info to the shader writer
	internal class PSSInfo
	{
		public PSSFile File;
		public List<string> AsmFiles;
	}

	// Holds information relating to a vertex attribute
	internal struct VertexAttrib
	{
		public string Name;
		public uint Type;
		public uint Location;
	}

	// Holds information relating to a uniform
	internal struct Uniform
	{
		public string Name;
		public uint Type;
		public int Offset; // Offset into block, for opaque handles this will be -1
		public int Binding; // Binding index, this will be the standard index for opaque handles, and the block binding for others
		public uint Size; // Array size
		public string Block; // The name of the block, if it is part of one
	}

	// Holds a binding in a shader module (may be an opaque handle, or a block)
	internal struct UniformBinding
	{
		public string Name;
		public uint Type;
		public uint Binding;
		public uint Size; // Size in bytes
	}
}
