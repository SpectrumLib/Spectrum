using System;

namespace Spectrum.Content
{
	/// <summary>
	/// Base type for implementing runtime content file loading logic. Derived types should be relatively lightweight,
	/// as multiple instances may be created for different content packs.
	/// </summary>
	/// <typeparam name="T">
	/// The runtime type that this loader produces from content item files. This type must match up with the type that
	/// it is invoked to load.
	/// </typeparam>
	public abstract class ContentLoader<T> : IContentLoader
		where T : class
	{
		#region Fields
		/// <summary>
		/// The type instance describing the runtime type that this loader produces.
		/// </summary>
		public Type ContentType { get; } = typeof(T);
		#endregion // Fields

		/// <summary>
		/// Implements the loading logic of reading in a content file and producing a runtime type. Returning null
		/// from this function is considered an error.
		/// <para>
		/// It is important that this function does not reference the stream for later use, as each stream instance
		/// is shared across multiple <see cref="ContentLoader{T}"/> instances. If you need to read the content file
		/// outside of this function (such as for streaming), use <see cref="ContentStream.Duplicate"/> to create a 
		/// usable copy of the stream.
		/// </para>
		/// </summary>
		/// <param name="stream">The opaque stream to the content data on disk.</param>
		/// <param name="ctx">Extra information about the current load process.</param>
		/// <returns>A new runtime type loaded from the content item data on disk.</returns>
		public abstract T Load(ContentStream stream, LoaderContext ctx);

		/// <summary>
		/// None-generic version of the <see cref="Load(ContentStream, LoaderContext)"/> function. Please do not call
		/// this function directly.
		/// </summary>
		[Obsolete("Do not call the non-generic Load() function of a ContentLoader directly.", true)]
		object IContentLoader.Load(ContentStream stream, LoaderContext ctx) => Load(stream, ctx);
	}
}
