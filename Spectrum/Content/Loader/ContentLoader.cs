/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;

namespace Spectrum.Content
{
	/// <summary>
	/// Base type for implementing logic for loading a content item. One instance of each ContentLoader subtype will
	/// be created for each content pack that requries them.
	/// </summary>
	/// <typeparam name="T">The content type that is loaded by this type.</typeparam>
	public abstract class ContentLoader<T> : IContentLoader
		where T : class
	{
		/// <summary>
		/// The type that is loaded by the content loader.
		/// </summary>
		public Type ContentType => typeof(T);

		/// <summary>
		/// Implements the logic of reading in and loading a runtime content type. Returning null from this function
		/// will produce a <see cref="ContentLoadException"/>.
		/// </summary>
		/// <param name="stream">The opaque stream to read file data from.</param>
		/// <param name="ctx">Additional information about the item being loaded.</param>
		/// <returns>A new runtime type loaded from the filesystem.</returns>
		public abstract T Load(ContentStream stream, LoaderContext ctx);

		/// <summary>
		/// Non-typed version of the <see cref="Load(ContentStream, LoaderContext)"/> function. Do not call directly.
		/// </summary>
		[Obsolete("Do not call non-generic ContentLoader.Load() directly.", true)]
		object IContentLoader.Load(ContentStream stream, LoaderContext ctx) => Load(stream, ctx);
	}

	// Internal type for generic-free references to content loaders
	internal interface IContentLoader
	{
		Type ContentType { get; }

		object Load(ContentStream stream, LoaderContext ctx);
	}
}
