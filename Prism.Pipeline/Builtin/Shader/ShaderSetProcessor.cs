using System;
using System.Collections.Generic;
using System.Globalization;

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

			// Parse the vertex attributes
			List<VertexAttrib[]> attrs = new List<VertexAttrib[]>(reflect.Count);
			for (int i = 0; i < reflect.Count; ++i)
			{
				if (!ParseVertexAttribs(input.Modules[i], reflect[i], spirv[i], out var atts, ctx.Logger))
					return null;
				attrs.Add(atts);
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
				if (!UInt32.TryParse(tstr, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out uint type))
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
}
