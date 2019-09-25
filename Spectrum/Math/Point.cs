/*
 * GNU LGPLv3 License - Copyright (c) The Spectrum Team
 * This file is subject to the terms and conditions of the GNU LGPLv3 license, the text of which can be found in the
 * 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/LGPL-3.0>.
 */
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Spectrum
{
	/// <summary>
	/// Describes a location in 2D cartesian integer space.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size = 2*sizeof(int))]
	public struct Point : IEquatable<Point>
	{
		/// <summary>
		/// Represents the origin of the representable space, at coordinates (0, 0).
		/// </summary>
		public static readonly Point Zero = new Point(0);

		#region Fields
		/// <summary>
		/// The x-coordinate of the point.
		/// </summary>
		[FieldOffset(0)]
		public int X;
		/// <summary>
		/// The y-coordinate of the point.
		/// </summary>
		[FieldOffset(sizeof(int))]
		public int Y;
		#endregion // Fields

		#region Ctor
		/// <summary>
		/// Constructs a point with all components equal to the value.
		/// </summary>
		/// <param name="i">The coordinate value.</param>
		public Point(int i) => X = Y = i;

		/// <summary>
		/// Constructs a point with the given component values.
		/// </summary>
		/// <param name="x">The x-coordinate value.</param>
		/// <param name="y">The y-coordinate value.</param>
		public Point(int x, int y)
		{
			X = x;
			Y = y;
		}
		#endregion // Ctor

		#region Overrides
		public readonly override bool Equals(object obj) => (obj is Point) && ((Point)obj == this);

		public readonly override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + X;
				hash = (hash * 23) + Y;
				return hash;
			}
		}

		public readonly override string ToString() => $"{{{X} {Y}}}";

		readonly bool IEquatable<Point>.Equals(Point other) =>
			(X == other.X) && (Y == other.Y);
		#endregion // Overrides

		#region Distance
		/// <summary>
		/// Gets the distance between this point and another.
		/// </summary>
		/// <param name="p">The point to get the distance to.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly float Distance(in Point p)
		{
			int dx = X - p.X, dy = Y - p.Y;
			return MathF.Sqrt(dx * dx + dy * dy);
		}

		/// <summary>
		/// Gets the distance between two points.
		/// </summary>
		/// <param name="l">The first point.</param>
		/// <param name="r">The second point.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Distance(in Point l, in Point r)
		{
			int dx = l.X - r.X, dy = l.Y - r.Y;
			return MathF.Sqrt(dx * dx + dy * dy);
		}

		/// <summary>
		/// Gets the distance squared between this point and another.
		/// </summary>
		/// <param name="p">The point to get the distance to.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly float DistanceSquared(in Point p)
		{
			int dx = X - p.X, dy = Y - p.Y;
			return dx * dx + dy * dy;
		}

		/// <summary>
		/// Gets the distance squared between two points.
		/// </summary>
		/// <param name="l">The first point.</param>
		/// <param name="r">The second point.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float DistanceSquared(in Point l, in Point r)
		{
			int dx = l.X - r.X, dy = l.Y - r.Y;
			return dx * dx + dy * dy;
		}
		#endregion // Distance

		#region Operators
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Point l, in Point r) => (l.X == r.X) && (l.Y == r.Y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Point l, in Point r) => (l.X != r.X) || (l.Y != r.Y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point operator + (in Point l, in Point r) => new Point(l.X + r.X, l.Y + r.Y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point operator - (in Point l, in Point r) => new Point(l.X - r.X, l.Y - r.Y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point operator * (in Point l, int r) => new Point(l.X * r, l.Y * r);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point operator * (int l, in Point r) => new Point(l * r.X, l * r.Y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point operator / (in Point l, int r) => new Point(l.X / r, l.Y / r);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Point (in Point3 p) => new Point(p.X, p.Y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Point (in Extent e) => new Point((int)e.Width, (int)e.Height);

		// TODO: Casting with other types
		#endregion // Operators

		#region Standard Math
		/// <summary>
		/// Component-wise maximum of the two input points.
		/// </summary>
		/// <param name="l">The first point.</param>
		/// <param name="r">The second point.</param>
		/// <param name="p">The output value for the minimized point.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Min(in Point l, in Point r, out Point p) => (p.X, p.Y) = 
			(l.X < r.X ? l.X : r.X, l.Y < r.Y ? l.Y : r.Y);

		/// <summary>
		/// Component-wise minimum of the two input points.
		/// </summary>
		/// <param name="l">The first point.</param>
		/// <param name="r">The second point.</param>
		/// <param name="p">The output value for the maximized point.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Max(in Point l, in Point r, out Point p) => (p.X, p.Y) = 
			(l.X > r.X ? l.X : r.X, l.Y > r.Y ? l.Y : r.Y);

		/// <summary>
		/// Component-wise clamp between of the two limiting points.
		/// </summary>
		/// <param name="val">The point to clamp.</param>
		/// <param name="min">The minimum bounding point.</param>
		/// <param name="max">The maximum bounding point.</param>
		/// <param name="p">The output clamped point.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Clamp(in Point val, in Point min, in Point max, out Point p) => (p.X, p.Y) =
			(Math.Clamp(val.X, min.X, max.X), Math.Clamp(val.Y, min.Y, max.Y));
		#endregion // Standard Math
	}
}
