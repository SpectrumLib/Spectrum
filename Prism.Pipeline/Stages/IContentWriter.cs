using System;

namespace Prism
{
	// Base non-generic handle type for working with ContentWriter instances
	internal interface IContentWriter
	{
		Type InputType { get; }
		string LoaderName { get; }
		CompressionPolicy Policy { get; }

		void Write(object input, ContentStream writer, WriterContext ctx);
	}
}
