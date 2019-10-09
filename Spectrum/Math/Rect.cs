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
	/// Describes a axis-aligned rectangle on a 2D integer cartesian grid. The origin is the bottom-left, with more
	/// positive coordinates to the right (+x) and up (+y).
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size = 4*sizeof(float))]
	public struct Rect : IEquatable<Rect>
	{
		/// <summary>
		/// Represents an empty rectangle with zero dimensions.
		/// </summary>
		public static readonly Rect Empty = new Rect(0, 0, 0, 0);

		#region Fields
		/// <summary>
		/// The x-coordinate of the left side of the rectangle.
		/// </summary>
		[FieldOffset(0)]
		public int X;
		/// <summary>
		/// The y-coordinate of the bottom side of the rectangle.
		/// </summary>
		[FieldOffset(sizeof(int))]
		public int Y;
		/// <summary>
		/// The width of the rectangle (along x-axis).
		/// </summary>
		[FieldOffset(2*sizeof(int))]
		public uint Width;
		/// <summary>
		/// The height of the rectangle (along y-axis).
		/// </summary>
		[FieldOffset(3*sizeof(int))]
		public uint Height;

		/// <summary>
		/// The bottom left corner of the rectangle.
		/// </summary>
		public Point Position
		{
			readonly get => new Point(X, Y);
			set { X = value.X; Y = value.Y; }
		}
		/// <summary>
		/// The dimensions of the rectangle.
		/// </summary>
		public Extent Size
		{
			readonly get => new Extent(Width, Height);
			set { Width = value.Width; Height = value.Height; }
		}

		/// <summary>
		/// The top-left corner.
		/// </summary>
		public readonly Point TopLeft => new Point(X, Y + (int)Height);
		/// <summary>
		/// The top-right corner.
		/// </summary>
		public readonly Point TopRight => new Point(X + (int)Width, Y + (int)Height);
		/// <summary>
		/// The bottom-left corner.
		/// </summary>
		public readonly Point BottomLeft => new Point(X, Y);
		/// <summary>
		/// The bottom-right corner.
		/// </summary>
		public readonly Point BottomRight => new Point(X + (int)Width, Y);

		/// <summary>
		/// The x-coordinate of the left edge.
		/// </summary>
		public readonly int Left => X;
		/// <summary>
		/// The x-coordinate of the right edge.
		/// </summary>
		public readonly int Right => X + (int)Width;
		/// <summary>
		/// The y-coordinate of the top edge.
		/// </summary>
		public readonly int Top => Y + (int)Height;
		/// <summary>
		/// The y-coordinate of the bottom edge.
		/// </summary>
		public readonly int Bottom => Y;

		/// <summary>
		/// The area of the rectangle interior.
		/// </summary>
		public readonly uint Area => Width * Height;

		/// <summary>
		/// The center of the rect area, with rounding towards the top-left when necessary.
		/// </summary>
		public readonly Point Center => new Point(X + (int)(Width / 2), Y + (int)(Height / 2));

		/// <summary>
		/// The exact center of the rectangle, using floating point coordinates.
		/// </summary>
		public readonly Vec2 CenterF => new Vec2(X + (Width / 2f), Y + (Height / 2f));
		#endregion // Fields

		#region Ctor
		/// <summary>
		/// Constructs a new rectangle from the given coordinates and dimensions
		/// </summary>
		/// <param name="x">The x-coordinate of the left side.</param>
		/// <param name="y">The y-coordinate of the bottom side.</param>
		/// <param name="w">The width.</param>
		/// <param name="h">The height.</param>
		public Rect(int x, int y, uint w, uint h)
		{
			X = x;
			Y = y;
			Width = w;
			Height = h;
		}

		/// <summary>
		/// Constructs a new rectangle from the position and size.
		/// </summary>
		/// <param name="pos">The rectangle position.</param>
		/// <param name="ex">The rectangle size.</param>
		public Rect(in Point pos, in Extent ex)
		{
			X = pos.X;
			Y = pos.Y;
			Width = ex.Width;
			Height = ex.Height;
		}
		#endregion // Ctor

		#region Overrides
		readonly bool IEquatable<Rect>.Equals(Rect other) => other == this;

		public readonly override bool Equals(object obj) => (obj is Rect) && ((Rect)obj == this);

		public readonly override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + X;
				hash = (hash * 23) + Y;
				hash = (hash * 23) + (int)Width;
				hash = (hash * 23) + (int)Height;
				return hash;
			}
		}

		public readonly override string ToString() => $"{{{X} {Y} {Width} {Height}}}";
		#endregion // Overrides

		#region Creation
		/// <summary>
		/// Calculates the minimal rectangular area that completely contains both input rectangles.
		/// </summary>
		/// <param name="r1">The first rectangle.</param>
		/// <param name="r2">The second rectangle.</param>
		/// <returns>The output union rectangle.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Rect Union(in Rect r1, in Rect r2)
		{
			Union(r1, r2, out var o);
			return o;
		}

		/// <summary>
		/// Calculates the minimal rectangular area that completely contains both input rectangles.
		/// </summary>
		/// <param name="r1">The first rectangle.</param>
		/// <param name="r2">The second rectangle.</param>
		/// <param name="o">The output union rectangle.</param>
		public static void Union(in Rect r1, in Rect r2, out Rect o)
		{
			o.X = Math.Min(r1.X, r2.X);
			o.Y = Math.Min(r1.Y, r2.Y);
			o.Width = (uint)(Math.Max(r1.Right, r2.Right) - o.X);
			o.Height = (uint)(Math.Max(r1.Top, r2.Top) - o.Y);
		}

		/// <summary>
		/// Calculates the overlap between the two rectangular areas, if any.
		/// </summary>
		/// <param name="r1">The first rectangle.</param>
		/// <param name="r2">The second rectangle.</param>
		/// <returns>The overlap area, set to <see cref="Empty"/> if there is no overlap.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Rect Intersect(in Rect r1, in Rect r2)
		{
			Intersect(r1, r2, out var o);
			return o;
		}

		/// <summary>
		/// Calculates the overlap between the two rectangular areas, if any.
		/// </summary>
		/// <param name="r1">The first rectangle.</param>
		/// <param name="r2">The second rectangle.</param>
		/// <param name="o">The overlap area, set to <see cref="Empty"/> if there is no overlap.</param>
		public static void Intersect(in Rect r1, in Rect r2, out Rect o)
		{
			if ((r1.Left < r2.Right) && (r1.Right > r2.Left) && (r1.Bottom < r2.Top) && (r1.Top > r2.Bottom))
			{
				o.X = Math.Max(r1.X, r2.X);
				o.Y = Math.Max(r1.Y, r2.Y);
				o.Width = (uint)(Math.Min(r1.Right, r2.Right) - o.X);
				o.Height = (uint)(Math.Min(r1.Top, r2.Top) - o.Y);
			}
			else
				o = Rect.Empty;
		}

		/// <summary>
		/// Constructs the minimal rectangular area that encompasses both passed points as corners.
		/// </summary>
		/// <param name="p1">The first point to contain.</param>
		/// <param name="p2">The second point to contain.</param>
		/// <returns>The output area.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Rect FromCorners(in Point p1, in Point p2)
		{
			FromCorners(p1, p2, out var o);
			return o;
		}

		/// <summary>
		/// Constructs the minimal rectangular area that encompasses both passed points as corners.
		/// </summary>
		/// <param name="p1">The first point to contain.</param>
		/// <param name="p2">The second point to contain.</param>
		/// <param name="o">The output area.</param>
		public static void FromCorners(in Point p1, in Point p2, out Rect o)
		{
			o.X = Math.Min(p1.X, p2.X);
			o.Y = Math.Min(p1.Y, p2.Y);
			o.Width = (uint)(Math.Max(p1.X, p2.X) - o.X);
			o.Height = (uint)(Math.Max(p1.Y, p2.Y) - o.Y);
		}

		/// <summary>
		/// Constructs the minimal rectangular area that encompasses all of the passed points.
		/// </summary>
		/// <param name="pts">The collection of points to encompass.</param>
		/// <returns>The output area.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Rect FromPoints(params Point[] pts)
		{
			FromPoints(out var o, pts);
			return o;
		}

		/// <summary>
		/// Constructs the minimal rectangular area that encompasses all of the passed points.
		/// </summary>
		/// <param name="o">The output area.</param>
		/// <param name="pts">The collection of points to encompass.</param>
		public static void FromPoints(out Rect o, params Point[] pts)
		{
			int minx = Int32.MaxValue, miny = Int32.MaxValue;
			int maxx = Int32.MinValue, maxy = Int32.MinValue;
			foreach (var p in pts)
			{
				minx = (p.X < minx) ? p.X : minx;
				miny = (p.Y < miny) ? p.Y : miny;
				maxx = (p.X > maxx) ? p.X : maxx;
				maxy = (p.Y > maxy) ? p.Y : maxy;
			}
			o = new Rect(minx, miny, (uint)(maxx - minx), (uint)(maxy - miny));
		}
		#endregion // Creation

		#region Operators
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Rect l, in Rect r) =>
			(l.X == r.X) && (l.Y == r.Y) && (l.Width == r.Width) && (l.Height == r.Height);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Rect l, in Rect r) =>
			(l.X != r.X) || (l.Y != r.Y) || (l.Width != r.Width) || (l.Height != r.Height);
		#endregion // Operators

		#region Tuples
		public readonly void Deconstruct(out int x, out int y, out uint w, out uint h)
		{
			x = X;
			y = Y;
			w = Width;
			h = Height;
		}

		public readonly void Deconstruct(out Point pos, out Extent ext)
		{
			pos.X = X;
			pos.Y = Y;
			ext.Width = Width;
			ext.Height = Height;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Rect (in (int x, int y, uint w, uint h) tup) =>
			new Rect(tup.x, tup.y, tup.w, tup.h);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Rect (in (Point pos, Extent ext) tup) =>
			new Rect(tup.pos, tup.ext);
		#endregion // Tuples
	}
}
