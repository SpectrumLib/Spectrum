using System;
using System.Collections.Generic;

namespace Prism.Builtin
{
	// Performs the compiling and checking for shader modules and shader set files
	[ContentProcessor("ShaderSetProcessor")]
	internal class ShaderSetProcessor : ContentProcessor<PSSFile, PSSInfo, ShaderSetWriter>
	{
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

			return new PSSInfo { File = input, AsmFiles = asmFiles };
		}
	}

	// Used to pass processed info to the shader writer
	internal class PSSInfo
	{
		public PSSFile File;
		public List<string> AsmFiles;
	}
}
