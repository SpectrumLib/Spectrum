using System;

namespace Prism.Builtin
{
	// Passes the file's contents through to the writer unaltered
	[ContentProcessor("Passthrough Processor")]
	internal sealed class PassthroughProcessor : ContentProcessor<byte[], byte[], PassthroughWriter>
	{
		[PipelineParameter]
		public int Test = 4;

		public override byte[] Process(byte[] input, ProcessorContext ctx)
		{
			ctx.Logger.Warn($"The value of test is {Test}.");

			return input;
		}
	}
}
