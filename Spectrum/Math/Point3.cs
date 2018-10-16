using System;
using System.Runtime.CompilerServices;

namespace Spectrum
{
	/// <summary>
	/// Defines a location on a 3D cartesian integer grid.
	/// </summary>
	public struct Point3 : IEquatable<Point3>
	{
		/// <summary>
		/// Represents the origin of the grid, with all components as zero.
		/// </summary>
		public static readonly Point3 Zero = new Point3(0, 0, 0);

		#region Fields
		/// <summary>
		/// The x-coordinate of the point.
		/// </summary>
		public int X;
		/// <summary>
		/// The y-coordinate of the point.
		/// </summary>
		public int Y;
		/// <summary>
		/// The z-coordinate of the point.
		/// </summary>
		public int Z;
		#endregion // Fields

		/// <summary>
		/// Creates a point with all components equal to the passed value.
		/// </summary>
		/// <param name="i">The value of all components.</param>
		public Point3(int i)
		{
			X = Y = Z = i;
		}

		/// <summary>
		/// Creates a point with components equal to the passed values.
		/// </summary>
		/// <param name="x">The x-component.</param>
		/// <param name="y">The y-component.</param>
		/// <param name="z">The z-component.</param>
		public Point3(int x, int y, int z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		/// <summary>
		/// Creates a point from a 2D point appended with a z-axis coordinate.
		/// </summary>
		/// <param name="p">The 2D point for the X and Y coordinates.</param>
		/// <param name="z">The z coordinate.</param>
		public Point3(in Point p, int z)
		{
			X = p.X;
			Y = p.Y;
			Z = z;
		}

		#region Overrides
		bool IEquatable<Point3>.Equals(Point3 other)
		{
			return (X == other.X) && (Y == other.Y) && (Z == other.Z);
		}

		public override bool Equals(object obj)
		{
			return (obj as Point3?)?.Equals(this) ?? false;
		}

		public override int GetHashCode()
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

		public override string ToString()
		{
			return $"{{{X} {Y} {Z}}}";
		}
		#endregion // Overrides

		#region Distance
		/// <summary>
		/// Returns the distance from the point to the origin.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float Distance() => Mathf.Sqrt(X * X + Y * Y + Z * Z);

		/// <summary>
		/// Returns the distance from the point to the origin.
		/// </summary>
		/// <param name="p">The point.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Distance(in Point3 p) => Mathf.Sqrt(p.X * p.X + p.Y * p.Y + p.Z * p.Z);

		/// <summary>
		/// Returns the distance squared from the point to the origin.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int DistanceSquared() => X * X + Y * Y + Z * Z;

		/// <summary>
		/// Returns the distance squared from the point to the origin.
		/// </summary>
		/// <param name="p">The point.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int DistanceSquared(in Point3 p) => p.X * p.X + p.Y * p.Y + p.Z * p.Z;

		/// <summary>
		/// Gets the distance between this point and another.
		/// </summary>
		/// <param name="p">The point to get the distance to.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float DistanceTo(in Point3 p)
		{
			int dx = X - p.X, dy = Y - p.Y, dz = Z - p.Z;
			return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
		}

		/// <summary>
		/// Gets the distance between two points.
		/// </summary>
		/// <param name="l">The first point.</param>
		/// <param name="r">The second point.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float DistanceTo(in Point3 l, in Point3 r)
		{
			int dx = l.X - r.X, dy = l.Y - r.Y, dz = l.Z - r.Z;
			return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
		}

		/// <summary>
		/// Gets the distance squared between this point and another.
		/// </summary>
		/// <param name="p">The point to get the distance to.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float DistanceToSquared(in Point3 p)
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
		public static float DistanceToSquared(in Point3 l, in Point3 r)
		{
			int dx = l.X - r.X, dy = l.Y - r.Y, dz = l.Z - r.Z;
			return dx * dx + dy * dy + dz * dz;
		}
		#endregion // Distance

		#region Operators
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Point3 l, in Point3 r)
		{
			return (l.X == r.X) && (l.Y == r.Y) && (l.Z == r.Z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Point3 l, in Point3 r)
		{
			return (l.X != r.X) || (l.Y != r.Y) || (l.Z != r.Z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point3 operator + (in Point3 l, in Point3 r)
		{
			return new Point3(l.X + r.X, l.Y + r.Y, l.Z + r.Z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point3 operator - (in Point3 l, in Point3 r)
		{
			return new Point3(l.X - r.X, l.Y - r.Y, l.Z - r.Z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point3 operator * (in Point3 l, int r)
		{
			return new Point3(l.X * r, l.Y * r, l.Z * r);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point3 operator * (int l, in Point3 r)
		{
			return new Point3(l * r.X, l * r.Y, l * r.Z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point3 operator / (in Point3 l, int r)
		{
			return new Point3(l.X / r, l.Y / r, l.Z / r);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Point (in Point3 p)
		{
			return new Point(p.X, p.Y);
		}

		// TODO
		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//public static implicit operator Vec3 (in Point3 p)
		//{
		//	return new Vec2(p.X, p.Y);
		//}
		#endregion // Operators

		#region Min/Max
		/// <summary>
		/// Creates a new point with the minimum components of the two input points.
		/// </summary>
		/// <param name="l">The first point.</param>
		/// <param name="r">The second point.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point3 Min(in Point3 l, in Point3 r)
		{
			return new Point3(l.X < r.X ? l.X : r.X, l.Y < r.Y ? l.Y : r.Y, l.Z < r.Z ? l.Z : r.Z);
		}
		/// <summary>
		/// Creates a new point with the minimum components of the two input points.
		/// </summary>
		/// <param name="l">The first point.</param>
		/// <param name="r">The second point.</param>
		/// <param name="p">The output value for the minimized point.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Min(in Point3 l, in Point3 r, out Point3 p)
		{
			p = new Point3(l.X < r.X ? l.X : r.X, l.Y < r.Y ? l.Y : r.Y, l.Z < r.Z ? l.Z : r.Z);
		}

		/// <summary>
		/// Creates a new point with the maximum components of the two input points.
		/// </summary>
		/// <param name="l">The first point.</param>
		/// <param name="r">The second point.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point3 Max(in Point3 l, in Point3 r)
		{
			return new Point3(l.X > r.X ? l.X : r.X, l.Y > r.Y ? l.Y : r.Y, l.Z > r.Z ? l.Z : r.Z);
		}
		/// <summary>
		/// Creates a new point with the maximum components of the two input points.
		/// </summary>
		/// <param name="l">The first point.</param>
		/// <param name="r">The second point.</param>
		/// <param name="p">The output value for the maximized point.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Max(in Point3 l, in Point3 r, out Point3 p)
		{
			p = new Point3(l.X > r.X ? l.X : r.X, l.Y > r.Y ? l.Y : r.Y, l.Z > r.Z ? l.Z : r.Z);
		}
		#endregion // Min/Max
	}
}
