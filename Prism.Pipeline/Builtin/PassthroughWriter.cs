using System;

namespace Prism.Builtin
{
	// Writes the raw data passed to it directly to the output file unaltered.
	internal sealed class PassthroughWriter : ContentWriter<byte[]>
	{
		public override void Write(byte[] input, ContentStream writer)
		{
			writer.Write(input);
		}
	}
}
