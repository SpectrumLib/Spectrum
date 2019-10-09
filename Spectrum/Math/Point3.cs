/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Spectrum
{
	/// <summary>
	/// Describes a location in 3D cartesian integer space.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size = 3*sizeof(int))]
	public struct Point3 : IEquatable<Point3>
	{
		/// <summary>
		/// Represents the origin of the representable space, at coordinates (0, 0, 0).
		/// </summary>
		public static readonly Point3 Zero = new Point3(0);

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
		/// <summary>
		/// The z-coordinate of the point.
		/// </summary>
		[FieldOffset(2*sizeof(int))]
		public int Z;
		#endregion // Fields

		#region Ctor
		/// <summary>
		/// Constructs a point with all components equal to the value.
		/// </summary>
		/// <param name="i">The coordinate value.</param>
		public Point3(int i) => X = Y = Z = i;

		/// <summary>
		/// Constructs a point with the given component values.
		/// </summary>
		/// <param name="x">The x-coordinate value.</param>
		/// <param name="y">The y-coordinate value.</param>
		/// <param name="z">The z-coordinate value.</param>
		public Point3(int x, int y, int z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		/// <summary>
		/// Contructs a point from a 2D point and a z-coordinate value.
		/// </summary>
		/// <param name="p">The point giving the x and y components.</param>
		/// <param name="z">The z-coordinate value.</param>
		public Point3(in Point p, int z)
		{
			X = p.X;
			Y = p.Y;
			Z = z;
		}
		#endregion // Ctor

		#region Overrides
		public readonly override bool Equals(object obj) => (obj is Point3) && ((Point3)obj == this);

		public readonly override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + X;
				hash = (hash * 23) + Y;
				hash = (hash * 23) + Z;
				return hash;
			}
		}

		public readonly override string ToString() => $"{{{X} {Y} {Z}}}";

		readonly bool IEquatable<Point3>.Equals(Point3 other) => 
			(X == other.X) && (Y == other.Y) && (Z == other.Z);
		#endregion // Overrides

		#region Distance
		/// <summary>
		/// Gets the distance between this point and another.
		/// </summary>
		/// <param name="p">The point to get the distance to.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly float Distance(in Point3 p)
		{
			int dx = X - p.X, dy = Y - p.Y, dz = Z - p.Z;
			return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
		}

		/// <summary>
		/// Gets the distance between two points.
		/// </summary>
		/// <param name="l">The first point.</param>
		/// <param name="r">The second point.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Distance(in Point3 l, in Point3 r)
		{
			int dx = l.X - r.X, dy = l.Y - r.Y, dz = l.Z - r.Z;
			return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
		}

		/// <summary>
		/// Gets the distance squared between this point and another.
		/// </summary>
		/// <param name="p">The point to get the distance to.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly float DistanceSquared(in Point3 p)
		{
			int dx = X - p.X, dy = Y - p.Y, dz = Z - p.Z;
			return dx * dx + dy * dy + dz * dz;
		}

		/// <summary>
		/// Gets the distance squared between two points.
		/// </summary>
		/// <param name="l">The first point.</param>
		/// <param name="r">The second point.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float DistanceSquared(in Point3 l, in Point3 r)
		{
			int dx = l.X - r.X, dy = l.Y - r.Y, dz = l.Z - r.Z;
			return dx * dx + dy * dy + dz * dz;
		}
		#endregion // Distance

		#region Operators
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Point3 l, in Point3 r) => (l.X == r.X) && (l.Y == r.Y) && (l.Z == r.Z);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Point3 l, in Point3 r) => (l.X != r.X) || (l.Y != r.Y) || (l.Z != r.Z);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point3 operator + (in Point3 l, in Point3 r) => new Point3(l.X + r.X, l.Y + r.Y, l.Z + r.Z);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point3 operator - (in Point3 l, in Point3 r) => new Point3(l.X - r.X, l.Y - r.Y, l.Z - r.Z);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point3 operator * (in Point3 l, int r) => new Point3(l.X * r, l.Y * r, l.Z * r);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point3 operator * (int l, in Point3 r) => new Point3(l * r.X, l * r.Y, l * r.Z);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point3 operator / (in Point3 l, int r) => new Point3(l.X / r, l.Y / r, l.Z / r);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Point3 (in Point p) => new Point3(p.X, p.Y, 0);

		// TODO: Casting with other types
		#endregion // Operators

		#region Standard Math
		/// <summary>
		/// Component-wise maximum of the two input points.
		/// </summary>
		/// <param name="l">The first point.</param>
		/// <param name="r">The second point.</param>
		/// <returns>The output value for the minimized point.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point3 Min(in Point3 l, in Point3 r) =>
			new Point3(l.X < r.X ? l.X : r.X, l.Y < r.Y ? l.Y : r.Y, l.Z < r.Z ? l.Z : r.Z);

		/// <summary>
		/// Component-wise maximum of the two input points.
		/// </summary>
		/// <param name="l">The first point.</param>
		/// <param name="r">The second point.</param>
		/// <param name="p">The output value for the minimized point.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Min(in Point3 l, in Point3 r, out Point3 p) => (p.X, p.Y, p.Z) = 
			(l.X < r.X ? l.X : r.X, l.Y < r.Y ? l.Y : r.Y, l.Z < r.Z ? l.Z : r.Z);

		/// <summary>
		/// Component-wise minimum of the two input points.
		/// </summary>
		/// <param name="l">The first point.</param>
		/// <param name="r">The second point.</param>
		/// <returns>The output value for the maximized point.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point3 Max(in Point3 l, in Point3 r) =>
			new Point3(l.X > r.X ? l.X : r.X, l.Y > r.Y ? l.Y : r.Y, l.Z > r.Z ? l.Z : r.Z);

		/// <summary>
		/// Component-wise minimum of the two input points.
		/// </summary>
		/// <param name="l">The first point.</param>
		/// <param name="r">The second point.</param>
		/// <param name="p">The output value for the maximized point.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Max(in Point3 l, in Point3 r, out Point3 p) => (p.X, p.Y, p.Z) =
			(l.X > r.X ? l.X : r.X, l.Y > r.Y ? l.Y : r.Y, l.Z > r.Z ? l.Z : r.Z);

		/// <summary>
		/// Component-wise clamp between of the two limiting points.
		/// </summary>
		/// <param name="val">The point to clamp.</param>
		/// <param name="min">The minimum bounding point.</param>
		/// <param name="max">The maximum bounding point.</param>
		/// <returns>The output clamped point.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point3 Clamp(in Point3 val, in Point3 min, in Point3 max) =>
			new Point3(Math.Clamp(val.X, min.X, max.X), Math.Clamp(val.Y, min.Y, max.Y), Math.Clamp(val.Z, min.Z, max.Z));

		/// <summary>
		/// Component-wise clamp between of the two limiting points.
		/// </summary>
		/// <param name="val">The point to clamp.</param>
		/// <param name="min">The minimum bounding point.</param>
		/// <param name="max">The maximum bounding point.</param>
		/// <param name="p">The output clamped point.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Clamp(in Point3 val, in Point3 min, in Point3 max, out Point3 p) => (p.X, p.Y, p.Z) 
			= (Math.Clamp(val.X, min.X, max.X), Math.Clamp(val.Y, min.Y, max.Y), Math.Clamp(val.Z, min.Z, max.Z));
		#endregion // Standard Math

		#region Tuples
		public readonly void Deconstruct(out int x, out int y, out int z)
		{
			x = X;
			y = Y;
			z = Z;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Point3 (in (int x, int y, int z) tup) =>
			new Point3(tup.x, tup.y, tup.z);
		#endregion // Tuples
	}
}
