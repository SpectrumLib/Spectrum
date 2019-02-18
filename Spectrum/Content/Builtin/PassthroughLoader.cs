using System;

namespace Spectrum.Content
{
	// Used for items that are processed with the PassthroughProcessor, and loaded as byte[]
	[ContentLoader("PassthroughLoader")]
	internal class PassthroughLoader : ContentLoader<byte[]>
	{
		public override byte[] Load(ContentStream stream, LoaderContext ctx)
		{
			return stream.ReadBytes(ctx.DataLength);
		}
	}
}
