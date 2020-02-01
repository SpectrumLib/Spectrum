/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Spectrum.Content
{
	/// <summary>
	/// Provides additional information and utility to <see cref="ContentLoader{T}.Load"/>.
	/// </summary>
	public sealed class LoaderContext
	{
		#region Fields
		private readonly ContentPack.Entry _item;

		/// <summary>
		/// The name of the item being processed.
		/// </summary>
		public string ItemName => _item.Name;
		/// <summary>
		/// If the item data being processed is compressed.
		/// </summary>
		public bool IsCompressed => _item.Compress;
		/// <summary>
		/// The total size of the available item data, in bytes.
		/// </summary>
		public ulong DataSize => _item.DataSize;
		#endregion // Fields

		internal LoaderContext(ContentPack.Entry item)
		{
			_item = item;
		}

		#region Exceptions
		/// <summary>
		/// Fills and throws a <see cref="ContentLoadException"/> with the given message.
		/// </summary>
		/// <param name="msg">The exception message.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Throw(string msg) => throw new ContentLoadException(_item.Name, msg);

		/// <summary>
		/// Fills and throws a <see cref="ContentLoadException"/> with the given inner exception.
		/// </summary>
		/// <param name="ex">The exception to throw.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Throw(Exception ex) => throw new ContentLoadException(_item.Name, ex.Message, ex);

		/// <summary>
		/// Fills and throws a <see cref="ContentLoadException"/> with the given inner exception and message.
		/// </summary>
		/// <param name="msg">The exception message.</param>
		/// <param name="inner">The inner exception that generated the state that is throwing the exception.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Throw(string msg, Exception inner) => throw new ContentLoadException(_item.Name, msg, inner);
		#endregion // Exceptions
	}
}
