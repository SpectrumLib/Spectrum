/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Spectrum
{
	/// <summary>
	/// Describes the size of a rectangular area. This type does not perform checking for negative dimensions.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size=2*sizeof(float))]
	public struct Extentf : IEquatable<Extentf>
	{
		/// <summary>
		/// An area of zero dimension.
		/// </summary>
		public static readonly Extentf Zero = new Extentf(0f, 0f);

		#region Fields
		/// <summary>
		/// The width of the area (x-axis dimension).
		/// </summary>
		[FieldOffset(0)]
		public float Width;
		/// <summary>
		/// The height of the area (y-axis dimension).
		/// </summary>
		[FieldOffset(sizeof(float))]
		public float Height;

		/// <summary>
		/// The total area of the described dimensions.
		/// </summary>
		public readonly float Area => Width * Height;
		/// <summary>
		/// Gets if the dimensions of the extent are positive.
		/// </summary>
		public readonly bool IsPositive => (Width >= 0f) && (Height >= 0f);
		#endregion // Fields

		/// <summary>
		/// Constructs a new size.
		/// </summary>
		/// <param name="w">The width of the new area.</param>
		/// <param name="h">The height of the new area.</param>
		public Extentf(float w, float h)
		{
			Width = w;
			Height = h;
		}

		#region Overrides
		public readonly override bool Equals(object obj) => (obj is Extentf) && ((Extentf)obj == this);

		public readonly override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + Width.GetHashCode();
				hash = (hash * 23) + Height.GetHashCode();
				return hash;
			}
		}

		public readonly override string ToString() => $"{{{Width} {Height}}}";

		readonly bool IEquatable<Extentf>.Equals(Extentf other) =>
			(Width == other.Width) && (Height == other.Height);
		#endregion // Overrides

		#region Basic Math
		/// <summary>
		/// Finds the component-wise minimum of the two extents.
		/// </summary>
		/// <param name="l">The first extent.</param>
		/// <param name="r">The second extent.</param>
		/// <returns>The component-wise mimimum.</returns>
		public static Extentf Min(in Extentf l, in Extentf r)
		{
			Min(l, r, out var o);
			return o;
		}

		/// <summary>
		/// Finds the component-wise minimum of the two extents.
		/// </summary>
		/// <param name="l">The first extent.</param>
		/// <param name="r">The second extent.</param>
		/// <param name="o">The output extent.</param>
		public static void Min(in Extentf l, in Extentf r, out Extentf o) =>
			o = new Extentf(Math.Min(l.Width, r.Width), Math.Min(l.Height, r.Height));

		/// <summary>
		/// Finds the component-wise maximum of the two extents.
		/// </summary>
		/// <param name="l">The first extent.</param>
		/// <param name="r">The second extent.</param>
		/// <returns>The component-wise maximum.</returns>
		public static Extentf Max(in Extentf l, in Extentf r)
		{
			Max(l, r, out var o);
			return o;
		}

		/// <summary>
		/// Finds the component-wise maximum of the two extents.
		/// </summary>
		/// <param name="l">The first extent.</param>
		/// <param name="r">The second extent.</param>
		/// <param name="o">The output extent.</param>
		public static void Max(in Extentf l, in Extentf r, out Extentf o) =>
			o = new Extentf(Math.Max(l.Width, r.Width), Math.Max(l.Height, r.Height));

		/// <summary>
		/// Component-wise clamp the extent between two limiting extents.
		/// </summary>
		/// <param name="e">The extent to clamp.</param>
		/// <param name="min">The minimum extent.</param>
		/// <param name="max">The maximum extent.</param>
		/// <returns>The component-wise clamp.</returns>
		public static Extentf Clamp(in Extentf e, in Extentf min, in Extentf max)
		{
			Clamp(e, min, max, out var o);
			return o;
		}

		/// <summary>
		/// Component-wise clamp the extent between two limiting extents.
		/// </summary>
		/// <param name="e">The extent to clamp.</param>
		/// <param name="min">The minimum extent.</param>
		/// <param name="max">The maximum extent.</param>
		/// <param name="o">The component-wise clamp.</param>
		public static Extentf Clamp(in Extentf e, in Extentf min, in Extentf max, out Extentf o) =>
			o = new Extentf(Math.Clamp(e.Width, min.Width, max.Width), Math.Clamp(e.Height, min.Height, max.Height));
		#endregion // Basic Math

		#region Operators
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Extentf l, in Extentf r) => (l.Width == r.Width) && (l.Height == r.Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Extentf l, in Extentf r) => (l.Width != r.Width) || (l.Height != r.Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Extentf operator * (in Extentf l, float r) => new Extentf(l.Width * r, l.Height * r);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Extentf operator * (float l, in Extentf r) => new Extentf(l * r.Width, l * r.Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Extentf operator / (in Extentf l, float r) => new Extentf(l.Width / r, l.Height / r);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Extentf (in Extent e) => new Extentf(e.Width, e.Height);
		#endregion // Operators

		#region Tuples
		public readonly void Deconstruct(out float w, out float h)
		{
			w = Width;
			h = Height;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Extentf(in (float w, float h) tup) =>
			new Extentf(tup.w, tup.h);
		#endregion // Tuples
	}
}
