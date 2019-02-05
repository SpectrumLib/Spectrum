using System;

namespace Prism
{
	// Base non-generic handle type for working with ContentProcessor instances
	internal interface IContentProcessor
	{
		Type InputType { get; }
		Type OutputType { get; }
		Type WriterType { get; }

		object Process(object input, ProcessorContext ctx);
	}
}
