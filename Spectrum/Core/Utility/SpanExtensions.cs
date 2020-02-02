/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.InteropServices;

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
		/// <param name="separators">The values to split the span on.</param>
		/// <returns>An enumerator over the split span.</returns>
		public static SpanSplitEnumerator<T> Split<T>(this ReadOnlySpan<T> span, params T[] separators)
			where T : IEquatable<T> => 
			new SpanSplitEnumerator<T>(span, separators);

		/// <summary>
		/// Implements a foreach loop as a Linq-stle function call.
		/// </summary>
		/// <typeparam name="T">The type held by the enumerator.</typeparam>
		/// <param name="span">The enumerable collection to iterate over.</param>
		/// <param name="func">The function to run on each member of the enumerator.</param>
		public static void ForEach<T>(this ReadOnlySpan<T> span, Action<T> func)
		{
			foreach (var val in span)
				func(val);
		}

		/// <summary>
		/// Implements a foreach loop as a Linq-stle function call, providing the function with the collection index.
		/// </summary>
		/// <typeparam name="T">The type held by the enumerator.</typeparam>
		/// <param name="span">The enumerable collection to iterate over.</param>
		/// <param name="func">The function to run on each member of the enumerator.</param>
		public static void ForEach<T>(this ReadOnlySpan<T> span, Action<T, int> func)
		{
			int index = 0;
			foreach (var val in span)
				func(val, index++);
		}

		/// <summary>
		/// Gets the <see cref="ReadOnlySpan{T}"/> representation of the array.
		/// </summary>
		/// <typeparam name="T">The type contained in the array.</typeparam>
		/// <param name="arr">The array to get the span of.</param>
		/// <returns>A <see cref="ReadOnlySpan{T}"/> representing the contents of the array.</returns>
		public static ReadOnlySpan<T> AsReadOnlySpan<T>(this T[] arr)
			where T : struct => arr.AsSpan();

		/// <summary>
		/// Gets the byte span representation of the non-byte span.
		/// </summary>
		/// <typeparam name="T">The incoming span type.</typeparam>
		/// <param name="span">The span to convert.</param>
		/// <returns>The input span as a byte span.</returns>
		public static Span<byte> AsBytes<T>(this Span<T> span)
			where T : struct => MemoryMarshal.AsBytes(span);

		/// <summary>
		/// Gets the byte span representation of the non-byte span.
		/// </summary>
		/// <typeparam name="T">The incoming span type.</typeparam>
		/// <param name="span">The span to convert.</param>
		/// <returns>The input span as a byte span.</returns>
		public static ReadOnlySpan<byte> AsBytes<T>(this ReadOnlySpan<T> span)
			where T : struct => MemoryMarshal.AsBytes(span);

		/// <summary>
		/// Function call style casting of a span to the corresponding read only span type.
		/// </summary>
		/// <typeparam name="T">The span type.</typeparam>
		/// <param name="span">The span to cast.</param>
		/// <returns>The converted span.</returns>
		public static ReadOnlySpan<T> AsReadOnly<T>(this Span<T> span) => (ReadOnlySpan<T>)span;
		
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
			/// The separator values for splitting the span.
			/// </summary>
			public readonly T[] Separators;
			/// <summary>
			/// The span giving the current slice into the backing span.
			/// </summary>
			public ReadOnlySpan<T> Current { get; private set; }
			#endregion // Fields

			internal SpanSplitEnumerator(ReadOnlySpan<T> span, T[] separators)
			{
				Span = span;
				Separators = separators;
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

				var idx = Span.IndexOfAny(Separators);
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
