using System;

namespace Prism.Builtin
{
	// Writer type for shader set (.pss) files
	internal class ShaderSetWriter : ContentWriter<PSSInfo>
	{
		public override string LoaderName => "Spectrum:ShaderSetLoader";

		public override void Write(PSSInfo input, ContentStream writer, WriterContext ctx)
		{
			
		}
	}
}
