using System;

namespace Prism.Builtin
{
	// Writes audio data
	internal class AudioWriter : ContentWriter<RLADAudio>
	{
		public override string LoaderName => "Spectrum:AudioLoader";

		public override void Write(RLADAudio input, ContentStream writer, WriterContext ctx)
		{
			try
			{

			}
			finally
			{
				input.Dispose();
			}
		}
	}
}
