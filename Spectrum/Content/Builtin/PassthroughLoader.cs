/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;

namespace Spectrum.Content
{
	// Passthrough loader, no processing, only raw byte stream
	internal class PassthroughLoader : ContentLoader<byte[]>
	{
		public override byte[] Load(ContentStream stream, LoaderContext ctx)
		{
			byte[] data = new byte[ctx.DataLength];
			if (stream.Read(data.AsSpan()) != ctx.DataLength)
				throw new ContentLoadException(ctx.ItemName, "could not read expected byte count.");
			return data;
		}
	}
}
