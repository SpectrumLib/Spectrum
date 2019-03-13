using System;
using System.IO;

namespace Prism.Builtin
{
	// Imports shader set files
	[ContentImporter("ShaderSetImporter", typeof(ShaderSetProcessor), ".pss")]
	internal class ShaderSetImporter : ContentImporter<PSSFile>
	{
		public override PSSFile Import(FileStream stream, ImporterContext ctx)
		{
			var lines = File.ReadAllLines(stream.Name);
			var file = PSSFile.Parse(lines, ctx.Logger);
			if (file == null)
				return null;

			// Add the shader files as dependencies
			foreach (var mod in file.Modules)
			{
				if (!ctx.AddDependency(mod.SourceFile))
				{
					ctx.LError($"The shader file '{mod.SourceFile}' does not exist.");
					return null;
				}
			}

			// Return the file
			return file;
		}
	}
}
