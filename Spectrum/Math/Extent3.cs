/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
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
	/// Describes the size of a rectangular volume with integer dimensions.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size = 3*sizeof(uint))]
	public struct Extent3 : IEquatable<Extent3>
	{
		/// <summary>
		/// An volume of zero dimension.
		/// </summary>
		public static readonly Extent3 Zero = new Extent3(0, 0, 0);

		#region Fields
		/// <summary>
		/// The width of the volume (x-axis dimension).
		/// </summary>
		[FieldOffset(0)]
		public uint Width;
		/// <summary>
		/// The height of the volume (y-axis dimension).
		/// </summary>
		[FieldOffset(sizeof(uint))]
		public uint Height;
		/// <summary>
		/// The depth of the volume (z-axis dimension).
		/// </summary>
		[FieldOffset(2*sizeof(uint))]
		public uint Depth;

		/// <summary>
		/// The total volume of the described dimensions.
		/// </summary>
		public readonly uint Volume => Width * Height * Depth;
		#endregion // Fields

		/// <summary>
		/// Constructs a new size.
		/// </summary>
		/// <param name="w">The width of the new volume.</param>
		/// <param name="h">The height of the new volume.</param>
		/// <param name="d">The depth of the new volume.</param>
		public Extent3(uint w, uint h, uint d)
		{
			Width = w;
			Height = h;
			Depth = d;
		}

		#region Overrides
		public readonly override bool Equals(object obj) => (obj is Extent3) && ((Extent3)obj == this);

		public readonly override int GetHashCode()
		{
			unchecked
			{
				uint hash = 17;
				hash = (hash * 23) + Width;
				hash = (hash * 23) + Height;
				hash = (hash * 23) + Depth;
				return (int)hash;
			}
		}

		public readonly override string ToString() => $"{{{Width} {Height} {Depth}}}";

		readonly bool IEquatable<Extent3>.Equals(Extent3 other) =>
			(Width == other.Width) && (Height == other.Height) && (Depth == other.Depth);
		#endregion // Overrides

		#region Basic Math
		/// <summary>
		/// Finds the component-wise minimum of the two extents.
		/// </summary>
		/// <param name="l">The first extent.</param>
		/// <param name="r">The second extent.</param>
		/// <returns>The component-wise mimimum.</returns>
		public static Extent3 Min(in Extent3 l, in Extent3 r)
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
		public static void Min(in Extent3 l, in Extent3 r, out Extent3 o) =>
			o = new Extent3(Math.Min(l.Width, r.Width), Math.Min(l.Height, r.Height), Math.Min(l.Depth, r.Depth));

		/// <summary>
		/// Finds the component-wise maximum of the two extents.
		/// </summary>
		/// <param name="l">The first extent.</param>
		/// <param name="r">The second extent.</param>
		/// <returns>The component-wise maximum.</returns>
		public static Extent3 Max(in Extent3 l, in Extent3 r)
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
		public static void Max(in Extent3 l, in Extent3 r, out Extent3 o) =>
			o = new Extent3(Math.Max(l.Width, r.Width), Math.Max(l.Height, r.Height), Math.Max(l.Depth, r.Depth));

		/// <summary>
		/// Component-wise clamp the extent between two limiting extents.
		/// </summary>
		/// <param name="e">The extent to clamp.</param>
		/// <param name="min">The minimum extent.</param>
		/// <param name="max">The maximum extent.</param>
		/// <returns>The component-wise clamp.</returns>
		public static Extent3 Clamp(in Extent3 e, in Extent3 min, in Extent3 max)
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
		public static void Clamp(in Extent3 e, in Extent3 min, in Extent3 max, out Extent3 o) =>
			o = new Extent3(Math.Clamp(e.Width, min.Width, max.Width), Math.Clamp(e.Height, min.Height, max.Height), Math.Clamp(e.Depth, min.Depth, max.Depth));
		#endregion // Basic Math

		#region Operators
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Extent3 l, in Extent3 r) => 
			(l.Width == r.Width) && (l.Height == r.Height) && (l.Depth == r.Depth);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Extent3 l, in Extent3 r) => 
			(l.Width != r.Width) || (l.Height != r.Height) || (l.Depth != r.Depth);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Extent3 operator * (in Extent3 l, uint r) => new Extent3(l.Width * r, l.Height * r, l.Depth * r);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Extent3 operator * (uint l, in Extent3 r) => new Extent3(l * r.Width, l * r.Height, l * r.Depth);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Extent3 operator / (in Extent3 l, uint r) => new Extent3(l.Width / r, l.Height / r, l.Depth / r);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Extent3 (in Vk.Extent3D e) => new Extent3(e.Width, e.Height, e.Depth);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Vk.Extent3D (in Extent3 e) => new Vk.Extent3D(e.Width, e.Height, e.Depth);
		#endregion // Operators

		#region Tuples
		public readonly void Deconstruct(out uint w, out uint h, out uint d)
		{
			w = Width;
			h = Height;
			d = Depth;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Extent3 (in (uint w, uint h, uint d) tup) =>
			new Extent3(tup.w, tup.h, tup.d);
		#endregion // Tuples
	}
}
