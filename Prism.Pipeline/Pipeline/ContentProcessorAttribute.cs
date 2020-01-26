/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.Linq;

namespace Prism.Pipeline
{
	/// <summary>
	/// Decorator attribute containing metadata for <see cref="ContentProcessor"/> subtypes.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public sealed class ContentProcessorAttribute : Attribute
	{
		// Self-type reference
		internal static readonly Type TYPE = typeof(ContentProcessorAttribute);

		#region Fields
		/// <summary>
		/// The display name of the content processor type.
		/// </summary>
		public readonly string DisplayName;
		/// <summary>
		/// The name of the content type that is handled by the decorated processor. The type name is case-insensitive.
		/// </summary>
		public readonly string ContentType;

		private readonly string[] _extensions;
		/// <summary>
		/// A list of file extensions (with a leading '.') that this processor is the default for.
		/// </summary>
		public IReadOnlyCollection<string> Extensions => _extensions;
		#endregion // Fields

		/// <summary>
		/// Describes metadata about a <see cref="ContentProcessor"/> type.
		/// </summary>
		/// <param name="name">The display name of the processor type.</param>
		/// <param name="cType">The content type associated with the processor.</param>
		/// <param name="exts">File extensions (comma-separated) that default to the processor type.</param>
		public ContentProcessorAttribute(string name, string cType, string exts)
		{
			DisplayName = name;
			ContentType = cType.ToLowerInvariant();
			_extensions = exts?.Split(',').Select(ex => (ex[0] == '.') ? ex : '.' + ex).ToArray();
		}
	}
}
