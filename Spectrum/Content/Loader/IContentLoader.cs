using System;

namespace Spectrum.Content
{
	// Base type for content loaders, used as a non-generic handle type
	internal interface IContentLoader
	{
		Type ContentType { get; }

		object Load(ContentStream stream, LoaderContext ctx);
	}
}
