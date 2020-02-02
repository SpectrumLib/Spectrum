/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Reflection;

namespace Spectrum.Content
{
	// Internal default type for "None" content, which is passthrough loaded as a raw byte array
	[ContentLoader("PassthroughLoader", "None")]
	internal sealed class PassthroughLoader : ContentLoader<byte[]>
	{
		public static readonly Type TYPE = typeof(PassthroughLoader);
		public static readonly ContentLoaderAttribute ATTR = TYPE.GetCustomAttribute<ContentLoaderAttribute>();
		public static readonly ConstructorInfo CTOR = TYPE.GetConstructor(Type.EmptyTypes);

		public override void Reset() { }

		public override byte[] Load(ContentReader reader, LoaderContext ctx)
		{
			var buffer = new byte[reader.DataSize];
			if ((ulong)reader.BaseStream.Read(buffer.AsSpan()) != reader.DataSize)
				ctx.Throw("Could not read expected number of bytes");
			return buffer;
		}
	}
}
