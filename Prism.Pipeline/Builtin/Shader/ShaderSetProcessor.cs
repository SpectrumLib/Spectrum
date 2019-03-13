using System;

namespace Prism.Builtin
{
	// Performs the compiling and checking for shader modules and shader set files
	[ContentProcessor("ShaderSetProcessor")]
	internal class ShaderSetProcessor : ContentProcessor<PSSFile, object, ShaderSetWriter>
	{
		public override object Process(PSSFile input, ProcessorContext ctx)
		{
			throw new NotImplementedException();
		}
	}
}
