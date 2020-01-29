/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.IO;
using System.Reflection;

namespace Prism.Pipeline
{
	/// <summary>
	/// Core type for implementing functionality for importing, processing, and writing content files in the Prism
	/// pipeline. Subclasses of this type are associated with a content type, and are responsible for all pipeline
	/// steps for that specific content type. 
	/// </summary>
	/// <remarks>
	/// <para>
	/// Subtypes must be decorated with <see cref="ContentProcessorAttribute"/> to be usable. They must also have
	/// a no-args constructor available.
	/// </para>
	/// <para>
	/// One instance of each subtype will exist on each build thread that uses the type. Because of this, static
	/// members are discouraged, and should be thread-safe if used.
	/// </para>
	/// <para>
	/// The processing loop is designed to work for both all-at-once and streaming processing. The processing functions
	/// are called in this order:
	/// <list type="number">
	/// <item><see cref="Reset"/></item>
	/// <item><see cref="Begin"/></item>
	/// <item><see cref="Read"/> -> <see cref="Process"/> -> <see cref="Write"/></item>
	/// <item>If the last <see cref="Read"/> call returned <c>true</c>, repeat the above sequence.</item>
	/// <item><see cref="End"/> - once the last call to <see cref="Read"/> returned <c>false</c>.</item>
	/// </list>
	/// </para>
	/// </remarks>
	public abstract class ContentProcessor : IDisposable
	{
		// Self-type reference
		internal static readonly Type TYPE = typeof(ContentProcessor);

		#region Fields
		/// <summary>
		/// The metadata attribute associated with this processor type, if there is one.
		/// </summary>
		public readonly ContentProcessorAttribute Attribute;

		/// <summary>
		/// Gets if the processor instance has been disposed.
		/// </summary>
		protected bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		/// <summary>
		/// Initializes the base objects for a <see cref="ContentProcessor"/> instance.
		/// </summary>
		protected ContentProcessor()
		{
			// Load the attribute
			Attribute = GetType().GetCustomAttribute(ContentProcessorAttribute.TYPE) as ContentProcessorAttribute;
		}
		~ContentProcessor()
		{
			dispose(false);
		}

		#region Pipeline Functions
		/// <summary>
		/// Prepares the processor for a new content item. This is the first function called in the pipeline.
		/// </summary>
		public abstract void Reset();

		/// <summary>
		/// Performs initial file reads, and prepares the processor for reading the content file.
		/// <para>
		/// This function should be used to load information about the content, not to perform any content
		/// processing.
		/// </para>
		/// </summary>
		/// <param name="ctx">Contextual information and utilities for the current item.</param>
		/// <param name="stream">The read-only stream to the input content file.</param>
		public abstract void Begin(PipelineContext ctx, BinaryReader stream);

		/// <summary>
		/// Reads in part (or all) of the data from the content file, which is then passed to <see cref="Process"/>.
		/// <para>
		/// Should return <c>true</c> if more data was read - <c>false</c> will end the processing loop.
		/// </para>
		/// </summary>
		/// <param name="ctx">Contextual information and utilities for the current item.</param>
		/// <param name="stream">
		/// Input content file stream. This is the same stream passed to <see cref="Begin"/>, with its state preserved
		/// between calls.
		/// </param>
		/// <returns>If there was more data read - <c>false</c> implies the content file is done being read.</returns>
		public abstract bool Read(PipelineContext ctx, BinaryReader stream);

		/// <summary>
		/// Called to process the last data loaded in the <see cref="Read"/> call, and prepare it for the
		/// <see cref="Write"/> function.
		/// </summary>
		/// <param name="ctx">Contextual information and utilities for the current item.</param>
		public abstract void Process(PipelineContext ctx);

		/// <summary>
		/// Called to write the processed data from the last <see cref="Process"/> call. This function should write
		/// any header data the first time it is called.
		/// </summary>
		/// <param name="ctx">Contextual information and utilities for the current item.</param>
		/// <param name="stream">The output stream to write the processed content data into.</param>
		public abstract void Write(PipelineContext ctx, BinaryWriter stream);

		/// <summary>
		/// Called to finalize the pipeline once all data has been processed. This is the last function call for any
		/// content item in the pipeline.
		/// </summary>
		/// <param name="ctx">Contextual information and utilities for the current item.</param>
		/// <param name="stream">The output stream to write the processed content data into.</param>
		/// <param name="compress">If the data generated by the last processor run should be compressed in release.</param>
		public abstract void End(PipelineContext ctx, BinaryWriter stream, out bool compress);
		#endregion // Pipeline Functions

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				onDispose(disposing);
			}
			IsDisposed = true;
		}

		/// <summary>
		/// Called when the processor instance is being disposed.
		/// </summary>
		/// <param name="disposing">If the disposal was instigated by <see cref="Dispose"/>.</param>
		protected abstract void onDispose(bool disposing);
		#endregion // IDisposable
	}
}
