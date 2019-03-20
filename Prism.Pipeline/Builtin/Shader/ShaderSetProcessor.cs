using System;
using System.Collections.Generic;

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
	}

	// Used to pass processed info to the shader writer
	internal class PSSInfo
	{
		public PSSFile File;
		public List<string> AsmFiles;
	}
}
