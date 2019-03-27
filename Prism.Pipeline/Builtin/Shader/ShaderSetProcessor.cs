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

			// Validate the versions
			for (int i = 0; i < spirv.Count; ++i)
			{
				if (!spirv[i].Contains("Source GLSL 450"))
				{
					ctx.LError($"The module '{input.Modules[i].Name}' must be declared with '#version 450'.");
					return null;
				}
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
					ctx.LStats($"    > Uniforms:  {String.Join(",  ", unis.Select(uni => $"{uni.Name} (t={uni.Type:X}[{uni.ArraySize}] o={uni.Offset} b={uni.Binding})"))}");
					ctx.LStats($"    > Bindings:  {String.Join(",  ", binds.Select(bnd => $"{bnd.Name} (t={bnd.Type:X} s={bnd.Size} b={bnd.Binding}"))})");
				}
			}

			// Validate the bindings for each shader
			foreach (var shdr in input.Shaders)
			{
				// Get the module indices
				var vert = (shdr.Vert != null) ? input.Modules.FindIndex(mod => mod.Name == shdr.Vert) : -1;
				var tesc = (shdr.Tesc != null) ? input.Modules.FindIndex(mod => mod.Name == shdr.Tesc) : -1;
				var tese = (shdr.Tese != null) ? input.Modules.FindIndex(mod => mod.Name == shdr.Tese) : -1;
				var geom = (shdr.Geom != null) ? input.Modules.FindIndex(mod => mod.Name == shdr.Geom) : -1;
				var frag = (shdr.Frag != null) ? input.Modules.FindIndex(mod => mod.Name == shdr.Frag) : -1;

				// Check the bindings for compatibility
				Dictionary<uint, (UniformBinding B, string S)> bfound = new Dictionary<uint, (UniformBinding, string)>();
				if (vert != -1 && !CheckBindings(shdr.Name, bfound, bindings[vert], "vert", ctx.Logger)) return null;
				if (tesc != -1 && !CheckBindings(shdr.Name, bfound, bindings[tesc], "tesc", ctx.Logger)) return null;
				if (tese != -1 && !CheckBindings(shdr.Name, bfound, bindings[tese], "tese", ctx.Logger)) return null;
				if (geom != -1 && !CheckBindings(shdr.Name, bfound, bindings[geom], "geom", ctx.Logger)) return null;
				if (frag != -1 && !CheckBindings(shdr.Name, bfound, bindings[frag], "frag", ctx.Logger)) return null;

				// Check the uniforms for compatibility
				Dictionary<string, (Uniform U, string S)> ufound = new Dictionary<string, (Uniform, string)>();
				if (vert != -1 && !CheckUniformNames(shdr.Name, ufound, unifs[vert], "vert", ctx.Logger)) return null;
				if (tesc != -1 && !CheckUniformNames(shdr.Name, ufound, unifs[tesc], "tesc", ctx.Logger)) return null;
				if (tese != -1 && !CheckUniformNames(shdr.Name, ufound, unifs[tese], "tese", ctx.Logger)) return null;
				if (geom != -1 && !CheckUniformNames(shdr.Name, ufound, unifs[geom], "geom", ctx.Logger)) return null;
				if (frag != -1 && !CheckUniformNames(shdr.Name, ufound, unifs[frag], "frag", ctx.Logger)) return null;

				// Warn about non-contiguous bindings (not an error, just sub-optimal)
				var maxb = bfound.Keys.Max();
				var missb = Enumerable.Range(0, (int)maxb + 1).Where(bi => !bfound.ContainsKey((uint)bi)).ToArray();
				if (missb.Length > 0)
					ctx.LWarn($"The shader '{shdr.Name}' does not have contiguous bindings ({String.Join(", ", missb)}).");
			}

			return new PSSInfo { File = input, AsmFiles = asmFiles, Attribs = attrs, Uniforms = unifs, Bindings = bindings };
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
				unis[ui] = new Uniform { Name = name, Type = type, Offset = offset, Binding = binding, ArraySize = size, Block = null };

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

		private static bool CheckBindings(string sname, Dictionary<uint, (UniformBinding B, string S)> found, UniformBinding[] binds, string stage, PipelineLogger logger)
		{
			foreach (var ub in binds)
			{
				if (found.ContainsKey(ub.Binding))
				{
					var other = found[ub.Binding];
					if (other.B.Name != ub.Name || other.B.Size != ub.Size || other.B.Type != ub.Type)
					{
						logger.Error($"The shader '{sname}' is binding two incompatible uniforms to the same index ({other.S}:{other.B.Name}, {stage}:{ub.Name}).");
						logger.Error($"    Shaders can only bind two uniforms to the same index if they match in name, size, and type.");
						return false;
					}
				}
				else
					found.Add(ub.Binding, (ub, stage));
			}
			return true;
		}

		private static bool CheckUniformNames(string sname, Dictionary<string, (Uniform U, string S)> found, Uniform[] unis, string stage, PipelineLogger logger)
		{
			foreach (var u in unis)
			{
				if (found.ContainsKey(u.Name))
				{
					var other = found[u.Name];
					if (other.U.Binding != u.Binding || other.U.Type != u.Type || other.U.Offset != u.Offset || other.U.ArraySize != u.ArraySize)
					{
						logger.Error($"The shader '{sname}' has incompatible uniforms with the name '{u.Name}' (stages {other.S}, {stage}).");
						logger.Error($"    Shaders can only share uniforms across modules if the uniforms match in binding, type, size, and offset.");
						return false;
					}
				}
				else
					found.Add(u.Name, (u, stage));
			}
			return true;
		}
	}

	// Used to pass processed info to the shader writer
	internal class PSSInfo
	{
		public PSSFile File;
		public List<string> AsmFiles;
		public List<VertexAttrib[]> Attribs;
		public List<Uniform[]> Uniforms;
		public List<UniformBinding[]> Bindings;
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
		public uint ArraySize; // Array size
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

	// Used to validate the types that are used in GLSL, since Spectrum only supports a subset of valid glsl types
	// The glslangValidator tool uses the types as defined in the OpenGL headers (GL_FLOAT, GL_FLOAT_VEC2, ect...)
	// In the source code, the mapping can be found in 'glslang/MachineIndependent/reflection.cpp' in the functions
	//    `mapToGlType()` and `mapSamplerToGlType()`
	// The actual values themselves are found in the `gl_types.h` file in the same directory as `reflection.cpp`
	// The regex pattern for matching against gl_types.h is `#define (GL[A-Z0-9_]+)\W+(0x[A-F0-9]+)` and `("$1", $2),`
	internal static class GLSLTypeValidator
	{

		public static readonly (string Name, uint Value)[] ALL_TYPES = new (string, uint)[] {
			// Data types
			("float", 0x1406), ("vec2", 0x8B50), ("vec3", 0x8B51), ("vec4", 0x8B52),				// FLOAT
			("double", 0x140A), ("dvec2", 0x8FFC), ("dvec3", 0x8FFD), ("dvec4", 0x8FFE),			// DOUBLE
			("int", 0x1404), ("ivec2", 0x8B53), ("ivec3", 0x8B54), ("ivec4", 0x8B55),				// INT32
			("uint", 0x1405), ("uvec2", 0x8DC6), ("uvec3", 0x8DC7), ("uvec4", 0x8DC8),				// UINT32
			("int64_t", 0x140E), ("i64vec2", 0x8FE9), ("i64vec3", 0x8FEA), ("i64vec4", 0x8FEB),		// INT64
			("uint64_t", 0x140F), ("u64vec2", 0x8FE5), ("u64vec3", 0x8FE6), ("u64vec4", 0x8FE7),	// UINT64
			("bool", 0x8B56), ("bvec2", 0x8B57), ("bvec3", 0x8B58), ("bvec4", 0x8B59),				// BOOL
			("mat2", 0x8B5A), ("mat3", 0x8B5B), ("mat4", 0x8B5C),									// SQUARE FLOAT MATRIX
			("mat2x3", 0x8B65), ("mat2x4", 0x8B66), ("mat3x2", 0x8B67), ("mat3x4", 0x8B68), ("mat4x2", 0x8B69), ("mat4x3", 0x8B6A),			// RECT FLOAT MATRIX
			("dmat2", 0x8F46), ("dmat3", 0x8F47), ("dmat4", 0x8F48),								// SQUARE DOUBLE MATRIX
			("dmat2x3", 0x8F49), ("dmat2x4", 0x8F4A), ("dmat3x2", 0x8F4B), ("dmat3x4", 0x8F4C), ("dmat4x2", 0x8F4D), ("dmat4x3", 0x8F4E),	// RECT DOUBLE MATRIX

			// Extended float data types
			("float16_t", 0x8FF8), ("f16vec2", 0x8FF9), ("f16vec3", 0x8FFA), ("f16vec4", 0x8FFB),	// HALF FLOAT
			("f16mat2", 0x91C5), ("f16mat3", 0x91C6), ("f16mat4", 0x91C7),							// SQUARE HALF FLOAT MATRIX
			("f16mat2x3", 0x91C8), ("f16mat2x4", 0x91C9), ("f16mat3x2", 0x91CA), ("f16mat3x4", 0x91CB), ("f16mat4x2", 0x91CC), ("f16mat4x3", 0x91CD), // RECT HALF FLOAT MATRIX

			// Standard combined image-samplers
			("sampler1D", 0x8B5D), ("sampler2D", 0x8B5E), ("sampler3D", 0x8B5F), ("samplerCube", 0x8B60),		// STANDARD NON-ARRAY SAMPLERS
			("samplerBuffer", 0x8DC2),																			// SAMPLER BUFFER / UNIFORM TEXEL BUFFER
			("sampler1DArray", 0x8DC0), ("sampler2DArray", 0x8DC1),												// STANDARD ARRAY SAMPLERS
			("sampler1DArrayShadow", 0x8DC3), ("sampler2DArrayShadow", 0x8DC4), ("samplerCubeShadow", 0x8DC5), ("sampler1DShadow", 0x8B61), ("sampler2DShadow", 0x8B62), // STANDARD SHADOW SAMPLERS
			("sampler2DRect", 0x8B63), ("sampler2DRectShadow", 0x8B64),											// RECT SAMPLERS
			("sampler2DMS", 0x9108), ("sampler2DMSArray", 0x910B),												// MULTISAMPLE SAMPLERS
			("samplerCubeArray", 0x900C), ("samplerCubeArrayShadow", 0x900D),									// CUBEMAP ARRAY SAMPLERS

			// Extended float storage images and image-samplers
			("f16sampler1D", 0x91CE), ("f16sampler2D", 0x91CF), ("f16sampler3D", 0x91D0), ("f16samplerCube", 0x91D1),	// STANDARD NON_ARRAY HALF FLOAT SAMPLERS
			("f16Sampler2DRect", 0x91D2), ("f16sampler1DArray", 0x91D3), ("f16sampler2DArray", 0x91D4), ("f16samplerCubeArray", 0x91D5),	// HALF FLOAT RECT AND STANDARD ARRAY SAMPLERS
			("f16SamplerBuffer", 0x91D6),																				// HALF FLOAT SAMPLER BUFFER
			("f16sampler2DMS", 0x91D7), ("f16sampler2DMSArray", 0x91D8),												// HALF FLOAT MULTISAMPLE SAMPLERS
			("f16sampler1DShadow", 0x91D9), ("f16sampler2DShadow", 0x91DA), ("f16sampler2DRectShadow", 0x91DB),			// STANDARD HALF FLOAT SHADOW SAMPLERS
			("f16sampler1DArrayShadow", 0x91DC), ("f16sampler2DArrayShadow", 0x91DD),									// HALF FLOAT ARRAY SHADOW SAMPLERS
			("f16samplerCubeShadow", 0x91DE), ("f16samplerCubeArrayShadow", 0x91DF),									// HALF FLOAT SHADOW CUBE SAMPLERS
			("f16image1D", 0x91E0),	("f16image2D", 0x91E1),	("f16image3D", 0x91E2), ("f16image2DRect", 0x91E3),			// STANDARD HALF FLOAT STORAGE IMAGES
			("f16imageCube", 0x91E4),																					// HALF FLOAT CUBE STORAGE IMAGE
			("f16image1DArray", 0x91E5), ("f16image2DArray", 0x91E6), ("f16imageCubeArray", 0x91E7),					// HALF FLOAT STORAGE IMAGE ARRAYS
			("f16imageBuffer", 0x91E8),																					// HALF FLOAT IMAGE BUFFER
			("f16image2DMS", 0x91E9), ("f16image2DMSArray", 0x91EA),													// HALF FLOAT MULTISAMPLE IMAGES

			// Standard integer image-samplers
			("isampler1D", 0x8DC9), ("isampler2D", 0x8DCA), ("isampler3D", 0x8DCB), ("isamplerCube", 0x8DCC),			// STANDARD INT32 SAMPLERS
			("isampler1DArray", 0x8DCE), ("isampler2DArray", 0x8DCF), ("isampler2DRect", 0x8DCD),						// INT32 ARRAY AND RECT SAMPLERS
			("isamplerBuffer", 0x8DD0),																					// INT32 SAMPLER BUFFER
			("isampler2DMS", 0x9109), ("isampler2DMSArray", 0x910C),													// INT32 MULTISAMPLER SAMPLERS
			("isamplerCubeArray", 0x900E),																				// INT32 CUBE ARRAY SAMPLER
			("usampler1D", 0x8DD1), ("usampler2D", 0x8DD2), ("usampler3D", 0x8DD3), ("usamplerCube", 0x8DD4),			// STANDARD UINT32 SAMPLERS
			("usampler1DArray", 0x8DD6), ("usampler2DArray", 0x8DD7), ("usampler2DRect", 0x8DD5),						// UINT32 ARRAY AND RECT SAMPLERS
			("usamplerBuffer", 0x8DD8),																					// UINT32 SAMPLER BUFFER
			("usampler2DMSArray", 0x910D),																				// UINT32 MULTISAMPLE ARRAY SAMPLER
			("usamplerCubeArray", 0x900F),																				// UINT32 CUBE ARRAY SAMPLER
			("usampler2DMS", 0x910A),																					// UINT32 MULTISAMPLE SAMPLER

			// Standard storage images
			("image1D", 0x904C), ("image2D", 0x904D), ("image3D", 0x904E), ("image2DRect", 0x904F),	("imageCube", 0x9050),			// STANDARD STORAGE IMAGES
			("imageBuffer", 0x9051),																								// STORAGE IMAGE BUFFER
			("image1DArray", 0x9052), ("image2DArray", 0x9053),	("imageCubeArray", 0x9054),											// STORAGE IMAGE ARRAYS
			("image2DMS", 0x9055), ("image2DMSArray", 0x9056),																		// MULTISAMPLE STORAGE IMAGES
			("iimage1D", 0x9057), ("iimage2D", 0x9058), ("iimage3D", 0x9059), ("iimage2DRect", 0x905A),	("iimageCube", 0x905B),		// INT32 STORAGE IMAGES
			("iimageBuffer", 0x905C),																								// INT32 STORAGE IMAGE BUFFER
			("iimage1DArray", 0x905D), ("iimage2DArray", 0x905E), ("iimageCubeArray", 0x905F),										// INT32 STORAGE IMAGE ARRAYS
			("iimage2DMS", 0x9060), ("iimage2DMSArray", 0x9061),																	// MULTISAMPLE INT32 STORAGE IMAGES
			("uimage1D", 0x9062), ("uimage2D", 0x9063), ("uimage3D", 0x9064), ("uimage2DRect", 0x9065), ("uimageCube", 0x9066),		// UINT32 STORAGE IMAGES
			("uimageBuffer", 0x9067),																								// UINT32 STORAGE IMAGE BUFFER
			("uimage1DArray", 0x9068), ("uimage2DArray", 0x9069), ("uimageCubeArray", 0x906A),										// UINT32 STORAGE IMAGE ARRAYS
			("uimage2DMS", 0x906B), ("uimage2DMSArray", 0x906C),																	// MULTISAMPLE UINT32 STORAGE IMAGES

			// Atomic counter
			("atomic_uint", 0x92DB),

			// Subpass inputs (dont have an OpenGL equivalent, so the reflection dump assigns them a value of 0)
			// The reflection dump also assigns blocks to have a value of 0, but they are never mixed with subpass inputs, so this will work
			// TODO: This does not allow us to differentiate between different subpass input types, so we need to look into this
			("subpassInput", 0x0000)
		};
	}
}
