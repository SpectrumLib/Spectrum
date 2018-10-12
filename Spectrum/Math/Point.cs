using System;
using System.Runtime.CompilerServices;

namespace Spectrum
{
	/// <summary>
	/// Defines a location on a 2D cartesian integer grid.
	/// </summary>
	public struct Point : IEquatable<Point>
	{
		/// <summary>
		/// Represents the origin of the grid, with both components as zero.
		/// </summary>
		public static readonly Point Zero = new Point(0, 0);

		#region Fields
		/// <summary>
		/// The x-coordinate of the point.
		/// </summary>
		public int X;
		/// <summary>
		/// The y-coordinate of the point.
		/// </summary>
		public int Y;
		#endregion // Fields

		/// <summary>
		/// Creates a point with both components equal to the passed value.
		/// </summary>
		/// <param name="i">The value of both components.</param>
		public Point(int i)
		{
			X = Y = i;
		}

		/// <summary>
		/// Creates a point with components equal to the passed values.
		/// </summary>
		/// <param name="x">The x-component.</param>
		/// <param name="y">The y-component.</param>
		public Point(int x, int y)
		{
			X = x;
			Y = y;
		}

		public override bool Equals(object obj)
		{
			return (obj as Point?)?.Equals(this) ?? false;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + X;
				hash = (hash * 23) + Y;
				return hash;
			}
		}

		public override string ToString()
		{
			return $"{{{X} {Y}}}";
		}

		#region Distance
		/// <summary>
		/// Returns the distance from the point to the origin.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float Distance() => (float)Math.Sqrt(X * X + Y * Y);

		/// <summary>
		/// Returns the distance from the point to the origin.
		/// </summary>
		/// <param name="p">The point.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Distance(in Point p) => (float)Math.Sqrt(p.X * p.X + p.Y * p.Y);

		/// <summary>
		/// Returns the distance squared from the point to the origin.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int DistanceSquared() => X * X + Y * Y;

		/// <summary>
		/// Returns the distance squared from the point to the origin.
		/// </summary>
		/// <param name="p">The point.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int DistanceSquared(in Point p) => p.X * p.X + p.Y * p.Y;

		/// <summary>
		/// Gets the distance between this point and another.
		/// </summary>
		/// <param name="p">The point to get the distance to.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float DistanceTo(in Point p)
		{
			int dx = X - p.X, dy = Y - p.Y;
			return (float)Math.Sqrt(dx * dx + dy * dy);
		}

		/// <summary>
		/// Gets the distance between two points.
		/// </summary>
		/// <param name="l">The first point.</param>
		/// <param name="r">The second point.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float DistanceTo(in Point l, in Point r)
		{
			int dx = l.X - r.X, dy = l.Y - r.Y;
			return (float)Math.Sqrt(dx * dx + dy * dy);
		}

		/// <summary>
		/// Gets the distance squared between this point and another.
		/// </summary>
		/// <param name="p">The point to get the distance to.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float DistanceToSquared(in Point p)
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
		public static float DistanceToSquared(in Point l, in Point r)
		{
			int dx = l.X - r.X, dy = l.Y - r.Y;
			return dx * dx + dy * dy;
		}
		#endregion // Distance

		#region IEquatable
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool IEquatable<Point>.Equals(Point other)
		{
			return (X == other.X) && (Y == other.Y);
		}
		#endregion // IEquatable

		#region Operators
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (Point l, Point r)
		{
			return (l.X == r.X) && (l.Y == r.Y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (Point l, Point r)
		{
			return (l.X != r.X) || (l.Y != r.Y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point operator + (Point l, Point r)
		{
			return new Point(l.X + r.X, l.Y + r.Y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point operator - (Point l, Point r)
		{
			return new Point(l.X - r.X, l.Y - r.Y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point operator * (Point l, int r)
		{
			return new Point(l.X * r, l.Y * r);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point operator * (int l, Point r)
		{
			return new Point(l * r.X, l * r.Y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Point operator / (Point l, int r)
		{
			return new Point(l.X / r, l.Y / r);
		}

		// TODO
		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//public static implicit operator Vec2 (Point p)
		//{
		//	return new Vec2(p.X, p.Y);
		//}
		#endregion // Operators
	}
}
