/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.IO;

namespace Spectrum.Content
{
	/// <summary>
	/// Base type for implementing runtime loading of content items. One instance of each type will be created for
	/// each <see cref="ContentManager"/> instance that uses it.
	/// <para>
	/// A "valid" ContentLoader type meets these criteria:
	/// <list type="bullet">
	/// <item>Subclass of <see cref="ContentLoader{T}"/>.</item>
	/// <item>Annotated with a valid <see cref="ContentLoaderAttribute"/>.</item>
	/// <item>Contains a public, no-args constructor.</item>
	/// <item>Is not a generic type.</item>
	/// </list>
	/// </para>
	/// </summary>
	/// <typeparam name="T">The type (or common base type) of content objects created by the loader.</typeparam>
	public abstract class ContentLoader<T> : IContentLoader
		where T : class
	{
		/// <summary>
		/// The type of the content loaded by this type (i.e. the type of <typeparamref name="T"/>).
		/// </summary>
		public Type ContentType => typeof(T);

		/// <summary>
		/// Called before <see cref="Load"/> to prepare the instance to process a new content item.
		/// </summary>
		public abstract void Reset();

		/// <summary>
		/// Called to perform the logic of loading a content item file into a runtime object.
		/// </summary>
		/// <param name="reader">
		/// The binary stream used to read in the content data from the disk. The content, if compressed, is
		/// decompressed silently by the reader. Note that seeking is supported, but will be slow for compressed
		/// streams (particularly backwards seeks).
		/// </param>
		/// <param name="ctx">Additional information about the load process.</param>
		/// <returns>The runtime content object loaded from the stream.</returns>
		public abstract T Load(BinaryReader reader, LoaderContext ctx);

		// This is not accessible outside of Spectrum (explicit implementation of internal interface)
		object IContentLoader.Load(BinaryReader reader, LoaderContext ctx) => Load(reader, ctx);
	}

	// Internal type for generic-free references to content loader instances
	internal interface IContentLoader
	{
		internal static readonly Type TYPE = typeof(IContentLoader);

		Type ContentType { get; }

		void Reset();

		object Load(BinaryReader reader, LoaderContext ctx);
	}
}
