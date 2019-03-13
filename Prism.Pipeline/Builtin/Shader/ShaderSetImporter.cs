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
			throw new NotImplementedException();
		}
	}
}
