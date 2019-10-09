﻿/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;

namespace Spectrum
{
	/// <summary>
	/// Contains extension functionality for working with <see cref="Span{T}"/> and <see cref="ReadOnlySpan{T}"/>.
	/// </summary>
	public static class SpanExtensions
	{
		/// <summary>
		/// Create an enumerator that splits a <see cref="ReadOnlySpan{T}"/>, similar to the String.Split() function.
		/// </summary>
		/// <remarks>This is currently planned for .NET 5.0.</remarks>
		/// <typeparam name="T">The type contained in the span.</typeparam>
		/// <param name="span">The span to split.</param>
		/// <param name="separator">The value to split the span on.</param>
		/// <returns>An enumerator over the split span.</returns>
		public static SpanSplitEnumerator<T> Split<T>(this ReadOnlySpan<T> span, T separator)
			where T : IEquatable<T>
			=> new SpanSplitEnumerator<T>(span, separator);

		/// <summary>
		/// An enumerator type that is used to implement enumeration logic when splitting <see cref="ReadOnlySpan{T}"/>.
		/// </summary>
		/// <typeparam name="T">The type contained in the ReadOnlySpan.</typeparam>
		public ref struct SpanSplitEnumerator<T> where T : IEquatable<T>
		{
			#region Fields
			/// <summary>
			/// The full backing span for the enumerator.
			/// </summary>
			public ReadOnlySpan<T> Span { get; private set; }
			/// <summary>
			/// The separator value for splitting the span.
			/// </summary>
			public readonly T Separator;
			/// <summary>
			/// The span giving the current slice into the backing span.
			/// </summary>
			public ReadOnlySpan<T> Current { get; private set; }
			#endregion // Fields

			internal SpanSplitEnumerator(ReadOnlySpan<T> span, T separator)
			{
				Span = span;
				Separator = separator;
				Current = default;
			}

			/// <summary>
			/// Allows this type to be used as an enumerator object.
			/// </summary>
			/// <returns>The reference to this object.</returns>
			public SpanSplitEnumerator<T> GetEnumerator() => this;

			/// <summary>
			/// Moves the enumerator onto the next split span slice.
			/// </summary>
			/// <returns>If there is a new slice available.</returns>
			public bool MoveNext()
			{
				if (Span.IsEmpty)
				{
					Span = Current = default;
					return false;
				}

				var idx = Span.IndexOf(Separator);
				if (idx < 0)
				{
					Current = Span;
					Span = default;
				}
				else
				{
					Current = Span.Slice(0, idx);
					Span = Span.Slice(idx + 1);
				}
				return true;
			}
		}
	}
}
