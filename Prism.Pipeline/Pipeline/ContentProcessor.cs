/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Reflection;

namespace Prism.Pipeline
{
	/// <summary>
	/// Core type for implementing functionality for importing, processing, and writing content files in the Prism
	/// pipeline. Subclasses of this type are associated with a content type, and are responsible for all pipeline
	/// steps for that specific content type. 
	/// <para>
	/// Subtypes must be decorated with <see cref="ContentProcessorAttribute"/> to be usable. They must also have
	/// a no-args constructor available.
	/// </para>
	/// <para>
	/// One instance of each subtype will exist on each build thread that uses the type. Because of this, static
	/// members are discouraged, and should be thread-safe if used.
	/// </para>
	/// </summary>
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
