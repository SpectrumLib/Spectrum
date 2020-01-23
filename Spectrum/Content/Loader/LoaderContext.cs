/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;

namespace Spectrum.Content
{
	/// <summary>
	/// Passes information relating to a content load operation to a <see cref="ContentLoader{T}"/> instance.
	/// </summary>
	public sealed class LoaderContext
	{
		#region Fields
		/// <summary>
		/// The name of the item currently being processed.
		/// </summary>
		public readonly string ItemName;
		/// <summary>
		/// Gets if the current content pack was built in release mode.
		/// </summary>
		public readonly bool IsRelease;
		/// <summary>
		/// Gets if the current content item is compressed.
		/// </summary>
		public readonly bool IsCompressed;
		/// <summary>
		/// The total amount of data available in the current content stream, in bytes. For compressed items, this is
		/// the total size of the uncompressed data.
		/// </summary>
		public readonly uint DataLength;
		/// <summary>
		/// The type passed as the type parameter to the <see cref="ContentManager.Load{T}(string, bool, bool)"/>
		/// function that is invoking the current loader.
		/// </summary>
		public readonly Type ContentType;
		#endregion // Fields

		internal LoaderContext(string name, bool rel, bool comp, uint size, Type cType)
		{
			ItemName = name;
			IsRelease = rel;
			IsCompressed = comp;
			DataLength = size;
			ContentType = cType;
		}
	}
}
