/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vk = SharpVk;

namespace Spectrum
{
	/// <summary>
	/// Describes the size of a rectangular area with integer dimensions.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size = 2*sizeof(uint))]
	public struct Extent : IEquatable<Extent>
	{
		/// <summary>
		/// An area of zero dimension.
		/// </summary>
		public static readonly Extent Zero = new Extent(0, 0);

		#region Fields
		/// <summary>
		/// The width of the area (x-axis dimension).
		/// </summary>
		[FieldOffset(0)]
		public uint Width;
		/// <summary>
		/// The height of the area (y-axis dimension).
		/// </summary>
		[FieldOffset(sizeof(uint))]
		public uint Height;

		/// <summary>
		/// The total area of the described dimensions.
		/// </summary>
		public readonly uint Area => Width * Height;
		#endregion // Fields

		/// <summary>
		/// Constructs a new size.
		/// </summary>
		/// <param name="w">The width of the new area.</param>
		/// <param name="h">The height of the new area.</param>
		public Extent(uint w, uint h)
		{
			Width = w;
			Height = h;
		}

		#region Overrides
		public readonly override bool Equals(object obj) => (obj is Extent) && ((Extent)obj == this);

		public readonly override int GetHashCode()
		{
			unchecked
			{
				uint hash = 17;
				hash = (hash * 23) + Width;
				hash = (hash * 23) + Height;
				return (int)hash;
			}
		}

		public readonly override string ToString() => $"{{{Width} {Height}}}";

		readonly bool IEquatable<Extent>.Equals(Extent other) =>
			(Width == other.Width) && (Height == other.Height);
		#endregion // Overrides

		#region Basic Math
		/// <summary>
		/// Finds the component-wise minimum of the two extents.
		/// </summary>
		/// <param name="l">The first extent.</param>
		/// <param name="r">The second extent.</param>
		/// <returns>The component-wise mimimum.</returns>
		public static Extent Min(in Extent l, in Extent r)
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
		public static void Min(in Extent l, in Extent r, out Extent o) =>
			o = new Extent(Math.Min(l.Width, r.Width), Math.Min(l.Height, r.Height));

		/// <summary>
		/// Finds the component-wise maximum of the two extents.
		/// </summary>
		/// <param name="l">The first extent.</param>
		/// <param name="r">The second extent.</param>
		/// <returns>The component-wise maximum.</returns>
		public static Extent Max(in Extent l, in Extent r)
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
		public static void Max(in Extent l, in Extent r, out Extent o) =>
			o = new Extent(Math.Max(l.Width, r.Width), Math.Max(l.Height, r.Height));

		/// <summary>
		/// Component-wise clamp the extent between two limiting extents.
		/// </summary>
		/// <param name="e">The extent to clamp.</param>
		/// <param name="min">The minimum extent.</param>
		/// <param name="max">The maximum extent.</param>
		/// <returns>The component-wise clamp.</returns>
		public static Extent Clamp(in Extent e, in Extent min, in Extent max)
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
		public static void Clamp(in Extent e, in Extent min, in Extent max, out Extent o) =>
			o = new Extent(Math.Clamp(e.Width, min.Width, max.Width), Math.Clamp(e.Height, min.Height, max.Height));
		#endregion // Basic Math

		#region Operators
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Extent l, in Extent r) => (l.Width == r.Width) && (l.Height == r.Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Extent l, in Extent r) => (l.Width != r.Width) || (l.Height != r.Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Extent operator * (in Extent l, uint r) => new Extent(l.Width * r, l.Height * r);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Extent operator * (uint l, in Extent r) => new Extent(l * r.Width, l * r.Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Extent operator / (in Extent l, uint r) => new Extent(l.Width / r, l.Height / r);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Extent (in Extentf e) => new Extent((uint)e.Width, (uint)e.Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Extent (in Vk.Extent2D e) => new Extent(e.Width, e.Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Vk.Extent2D (in Extent e) => new Vk.Extent2D(e.Width, e.Height);
		#endregion // Operators

		#region Tuples
		public readonly void Deconstruct(out uint w, out uint h)
		{
			w = Width;
			h = Height;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Extent (in (uint w, uint h) tup) =>
			new Extent(tup.w, tup.h);
		#endregion // Tuples
	}
}
