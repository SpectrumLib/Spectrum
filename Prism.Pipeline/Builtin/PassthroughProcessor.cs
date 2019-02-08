using System;

namespace Prism.Builtin
{
	// Passes the file's contents through to the writer unaltered
	[ContentProcessor("Passthrough Processor")]
	internal sealed class PassthroughProcessor : ContentProcessor<byte[], byte[], PassthroughWriter>
	{
		public override byte[] Process(byte[] input, ProcessorContext ctx)
		{
			return input;
		}
	}
}
