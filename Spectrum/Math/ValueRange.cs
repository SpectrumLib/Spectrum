/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.CompilerServices;

namespace Spectrum
{
	/// <summary>
	/// Represents a range of generic values defined by a minimum and maximum value.
	/// </summary>
	/// <typeparam name="T">The value type.</typeparam>
	public struct ValueRange<T> : IEquatable<ValueRange<T>>
		where T : struct, IComparable<T>, IEquatable<T>
	{
		/// <summary>
		/// Describes a zero size value range, where the boundary values are the same.
		/// </summary>
		public static readonly ValueRange<T> Empty = new ValueRange<T>(default, default);

		#region Fields
		/// <summary>
		/// The minimum value of the represented range.
		/// </summary>
		public T Min;
		/// <summary>
		/// The maximum value of the represented range.
		/// </summary>
		public T Max;

		/// <summary>
		/// Gets if the range represents an empty range.
		/// </summary>
		public readonly bool IsEmpty => Min.CompareTo(default) == 0 && Max.CompareTo(default) == 0;
		#endregion // Fields

		#region Ctor
		/// <summary>
		/// Creates a new value range from the given values. The values are checked for ordering.
		/// </summary>
		/// <param name="v1">The first boundary value for the range.</param>
		/// <param name="v2">The second boundary value for the range.</param>
		public ValueRange(T v1, T v2)
		{
			var swap = v1.CompareTo(v2) > 0;
			Min = swap ? v2 : v1;
			Max = swap ? v1 : v2;
		}
		#endregion // Ctor

		#region Overrides
		public readonly override bool Equals(object obj) => (obj is ValueRange<T>) && ((ValueRange<T>)obj == this);

		public readonly override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + Min.GetHashCode();
				hash = (hash * 23) + Max.GetHashCode();
				return hash;
			}
		}

		public readonly override string ToString() => $"{{{Min.ToString()} {Max.ToString()}}}";

		readonly bool IEquatable<ValueRange<T>>.Equals(ValueRange<T> obj) => obj.Min.Equals(Min) && obj.Max.Equals(Max);
		#endregion // Overrides

		#region Range Operations
		/// <summary>
		/// Checks the overlap condition of this range with the other range.
		/// </summary>
		/// <param name="r">The other range to check.</param>
		/// <param name="endpoints">If equal endpoints counts as a overlap.</param>
		/// <returns>The overlap condition of the ranges.</returns>
		public RangeOverlap Overlap(in ValueRange<T> r, bool endpoints = false) => Overlap(this, r, endpoints);

		/// <summary>
		/// Checks if the this range has any overlap with the other range.
		/// </summary>
		/// <param name="r">The other range to check.</param>
		/// <param name="endpoints">If equal endpoints counts as a overlap.</param>
		/// <returns>If the ranges overlap in any way.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool OverlapAny(in ValueRange<T> r, bool endpoints = false) => 
			Overlap(this, r, endpoints) != RangeOverlap.Disjoint;

		/// <summary>
		/// Checks the overlap condition of the two ranges.
		/// </summary>
		/// <param name="l">The first range to check.</param>
		/// <param name="r">The second range to check.</param>
		/// <param name="endpoints">If equal endpoints counts as a overlap.</param>
		/// <returns>The overlap condition of the ranges.</returns>
		public static RangeOverlap Overlap(in ValueRange<T> l, in ValueRange<T> r, bool endpoints = false)
		{
			int rcmp = r.Min.CompareTo(l.Max),
				lcmp = r.Max.CompareTo(l.Min);
			bool rin = endpoints ? (rcmp <= 0) : (rcmp < 0),
				 lin = endpoints ? (lcmp >= 0) : (lcmp > 0);

			if (rin && lin)
			{
				int minc = l.Min.CompareTo(r.Min), 
					maxc = l.Max.CompareTo(r.Max);
				if (minc == 0 && maxc == 0)
					return RangeOverlap.Equal;
				if (minc < 0 && maxc > 0)
					return RangeOverlap.FirstContains;
				if (minc > 0 && maxc < 0)
					return RangeOverlap.SecondContains;
				return RangeOverlap.Partial;
			}
			else
				return RangeOverlap.Disjoint;
		}

		/// <summary>
		/// Checks if the two ranges have any overlap.
		/// </summary>
		/// <param name="l">The first range to check.</param>
		/// <param name="r">The second range to check.</param>
		/// <param name="endpoints">If equal endpoints counts as a overlap.</param>
		/// <returns>If the ranges overlap in any way.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool OverlapAny(in ValueRange<T> l, in ValueRange<T> r, bool endpoints = false) => 
			Overlap(l, r, endpoints) != RangeOverlap.Disjoint;

		/// <summary>
		/// Gets the overlapping region with the other range.
		/// </summary>
		/// <param name="r">The other range to check.</param>
		/// <returns>The overlapping range, or <see cref="Empty"/> if there is no overlap.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ValueRange<T> Intersect(in ValueRange<T> r) => Intersect(this, r);

		/// <summary>
		/// Gets the overlapping region of the two ranges.
		/// </summary>
		/// <param name="l">The first range to check.</param>
		/// <param name="r">The second range to check.</param>
		/// <returns>The overlapping range, or <see cref="Empty"/> if there is no overlap.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ValueRange<T> Intersect(in ValueRange<T> l, in ValueRange<T> r)
		{
			Intersect(l, r, out var o);
			return o;
		}

		/// <summary>
		/// Gets the overlapping region of the two ranges.
		/// </summary>
		/// <param name="l">The first range to check.</param>
		/// <param name="r">The second range to check.</param>
		/// <param name="o">The output range.</param>
		public static void Intersect(in ValueRange<T> l, in ValueRange<T> r, out ValueRange<T> o)
		{
			bool rin = r.Min.CompareTo(l.Max) <= 0,
				 lin = r.Max.CompareTo(l.Min) >= 0;

			if (lin && rin)
			{
				T min = (l.Min.CompareTo(r.Min) <= 0) ? r.Min : l.Min,
				  max = (l.Max.CompareTo(r.Max) >= 0) ? r.Max : l.Max;
				o = new ValueRange<T>(min, max);
			}
			else
				o = ValueRange<T>.Empty;
		}

		/// <summary>
		/// Gets the range that minimally fully contains this and the other range.
		/// </summary>
		/// <param name="r">The other range to contain.</param>
		/// <returns>The containing region of the two ranges.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ValueRange<T> Union(in ValueRange<T> r) => Union(this, r);

		/// <summary>
		/// Gets the range that minimally fully contains the two ranges.
		/// </summary>
		/// <param name="l">The first range to contain.</param>
		/// <param name="r">The second range to contain.</param>
		/// <returns>The containing region of the two ranges.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ValueRange<T> Union(in ValueRange<T> l, in ValueRange<T> r)
		{
			Union(l, r, out var o);
			return o;
		}

		/// <summary>
		/// Gets the range that minimally fully contains the two ranges.
		/// </summary>
		/// <param name="l">The first range to contain.</param>
		/// <param name="r">The second range to contain.</param>
		/// <param name="o">The output range.</param>
		public static void Union(in ValueRange<T> l, in ValueRange<T> r, out ValueRange<T> o)
		{
			T min = (l.Min.CompareTo(r.Min) <= 0) ? l.Min : r.Min,
			  max = (l.Max.CompareTo(r.Max) >= 0) ? l.Max : r.Max;
			o = new ValueRange<T>(min, max);
		}

		/// <summary>
		/// Checks if this range contains the value.
		/// </summary>
		/// <param name="v">The value to check.</param>
		/// <returns>If this range contains the value.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(in T v) => Min.CompareTo(v) <= 0 && Max.CompareTo(v) >= 0;

		/// <summary>
		/// Checks if the range contains the value.
		/// </summary>
		/// <param name="r">The range to check.</param>
		/// <param name="v">The value to check.</param>
		/// <returns>If the range contains the value.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Contains(in ValueRange<T> r, in T v) => r.Min.CompareTo(v) <= 0 && r.Max.CompareTo(v) >= 0;
		#endregion // Range Operations

		#region Operators
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in ValueRange<T> l, in ValueRange<T> r) =>
			l.Min.Equals(r.Min) && l.Max.Equals(r.Max);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in ValueRange<T> l, in ValueRange<T> r) =>
			!l.Min.Equals(r.Min) || !l.Max.Equals(r.Max);
		#endregion // Operators

		#region Tuples
		public readonly void Deconstruct(out T min, out T max)
		{
			min = Min;
			max = Max;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator ValueRange<T> (in (T v1, T v2) tup) =>
			new ValueRange<T>(tup.v1, tup.v2);
		#endregion // Tuples
	}

	/// <summary>
	/// Describes the different ways in which two <see cref="ValueRange{T}"/> can overlap.
	/// </summary>
	public enum RangeOverlap
	{
		/// <summary>
		/// The two ranges share no overlap.
		/// </summary>
		Disjoint,
		/// <summary>
		/// The ranges partially overlap each other.
		/// </summary>
		Partial,
		/// <summary>
		/// The first range completely contains the second range.
		/// </summary>
		FirstContains,
		/// <summary>
		/// The second range completely contains the first range.
		/// </summary>
		SecondContains,
		/// <summary>
		/// The two ranges are equal.
		/// </summary>
		Equal
	}
}
