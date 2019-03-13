using System;

namespace Prism.Builtin
{
	// Writer type for shader set (.pss) files
	internal class ShaderSetWriter : ContentWriter<object>
	{
		public override string LoaderName => "Spectrum:ShaderSetLoader";

		public override void Write(object input, ContentStream writer, WriterContext ctx)
		{
			throw new NotImplementedException();
		}
	}
}
