/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;

namespace Spectrum.Content
{
	/// <summary>
	/// Annotates <see cref="ContentLoader{T}"/> types with metadata required for loading them at runtime.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public sealed class ContentLoaderAttribute : Attribute
	{
		#region Fields
		/// <summary>
		/// The display name of the annotated <see cref="ContentLoader{T}"/> type.
		/// </summary>
		public readonly string DisplayName;
		/// <summary>
		/// The type of content loaded by this type, used to match against ContentProcessor types in the Prism 
		/// pipeline.
		/// </summary>
		public readonly string ContentType;
		#endregion // Fields

		/// <summary>
		/// Creates a new attribute for a <see cref="ContentLoader{T}"/> type.
		/// </summary>
		/// <param name="name">The display name.</param>
		/// <param name="type">The loaded content type name.</param>
		public ContentLoaderAttribute(string name, string type)
		{
			DisplayName = name;
			ContentType = type;
		}
	}
}
