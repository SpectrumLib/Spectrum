/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;

namespace Prism
{
	// Contains extension functionality for working with spans
	internal static class SpanExtensions
	{
		public static SpanSplitEnumerator<T> Split<T>(this ReadOnlySpan<T> span, params T[] separators)
			where T : IEquatable<T> => 
			new SpanSplitEnumerator<T>(span, separators);
		
		public static void ForEach<T>(this ReadOnlySpan<T> span, Action<T> func)
		{
			foreach (var val in span)
				func(val);
		}

		public static void ForEach<T>(this ReadOnlySpan<T> span, Action<T, int> func)
		{
			int index = 0;
			foreach (var val in span)
				func(val, index++);
		}

		public static ReadOnlySpan<T> AsReadOnlySpan<T>(this T[] arr)
			where T : struct => arr.AsSpan();

		public ref struct SpanSplitEnumerator<T> where T : IEquatable<T>
		{
			#region Fields
			public ReadOnlySpan<T> Span { get; private set; }
			public readonly T[] Separators;
			public ReadOnlySpan<T> Current { get; private set; }
			#endregion // Fields

			internal SpanSplitEnumerator(ReadOnlySpan<T> span, T[] separators)
			{
				Span = span;
				Separators = separators;
				Current = default;
			}

			public SpanSplitEnumerator<T> GetEnumerator() => this;

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
