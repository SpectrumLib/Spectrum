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
	/// The floating point version of the <see cref="Rect"/> type, for continuous cartesian space. Note that this type
	/// does not check for negative size.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size=4*sizeof(float))]
	public struct Rectf : IEquatable<Rectf>
	{
		/// <summary>
		/// Represents an empty rectangle with zero dimensions.
		/// </summary>
		public static readonly Rectf Empty = new Rectf(0f, 0f, 0f, 0f);

		#region Fields
		/// <summary>
		/// The x-coordinate of the left side of the rectangle.
		/// </summary>
		[FieldOffset(0)]
		public float X;
		/// <summary>
		/// The y-coordinate of the bottom side of the rectangle.
		/// </summary>
		[FieldOffset(sizeof(float))]
		public float Y;
		/// <summary>
		/// The width of the rectangle (along x-axis).
		/// </summary>
		[FieldOffset(2*sizeof(float))]
		public float Width;
		/// <summary>
		/// The height of the rectangle (along y-axis).
		/// </summary>
		[FieldOffset(3*sizeof(float))]
		public float Height;

		/// <summary>
		/// The bottom left corner of the rectangle.
		/// </summary>
		public Vec2 Position
		{
			readonly get => new Vec2(X, Y);
			set { X = value.X; Y = value.Y; }
		}
		/// <summary>
		/// The dimensions of the rectangle.
		/// </summary>
		public Extentf Size
		{
			readonly get => new Extentf(Width, Height);
			set { Width = value.Width; Height = value.Height; }
		}

		/// <summary>
		/// The top-left corner, assuming an up/right coordinate system.
		/// </summary>
		public readonly Vec2 TopLeft => new Vec2(X, Y + Height);
		/// <summary>
		/// The top-right corner, assuming an up/right coordinate system.
		/// </summary>
		public readonly Vec2 TopRight => new Vec2(X + Width, Y + Height);
		/// <summary>
		/// The bottom-left corner, assuming an up/right coordinate system.
		/// </summary>
		public readonly Vec2 BottomLeft => new Vec2(X, Y);
		/// <summary>
		/// The bottom-right corner, assuming an up/right coordinate system.
		/// </summary>
		public readonly Vec2 BottomRight => new Vec2(X + Width, Y);

		/// <summary>
		/// The top-left corner, assuming an down/right coordinate system.
		/// </summary>
		public readonly Vec2 TopLeftInv => new Vec2(X, Y);
		/// <summary>
		/// The top-right corner, assuming an down/right coordinate system.
		/// </summary>
		public readonly Vec2 TopRightInv => new Vec2(X + Width, Y);
		/// <summary>
		/// The bottom-left corner, assuming an down/right coordinate system.
		/// </summary>
		public readonly Vec2 BottomLeftInv => new Vec2(X, Y + Height);
		/// <summary>
		/// The bottom-right corner, assuming an down/right coordinate system.
		/// </summary>
		public readonly Vec2 BottomRightInv => new Vec2(X + Width, Y + Height);

		/// <summary>
		/// The x-coordinate of the left edge.
		/// </summary>
		public readonly float Left => X;
		/// <summary>
		/// The x-coordinate of the right edge.
		/// </summary>
		public readonly float Right => X + Width;
		/// <summary>
		/// The y-coordinate of the top edge, assuming an up/right coordinate system.
		/// </summary>
		public readonly float Top => Y + Height;
		/// <summary>
		/// The y-coordinate of the bottom edge, assuming an up/right coordinate system.
		/// </summary>
		public readonly float Bottom => Y;
		/// <summary>
		/// The y-coordinate of the top edge, assuming an down/right coordinate system.
		/// </summary>
		public readonly float TopInv => Y;
		/// <summary>
		/// The y-coordinate of the bottom edge, assuming an down/right coordinate system.
		/// </summary>
		public readonly float BottomInv => Y + Height;

		/// <summary>
		/// The area of the rectangle interior.
		/// </summary>
		public readonly float Area => Width * Height;

		/// <summary>
		/// The center of the rectangle.
		/// </summary>
		public readonly Vec2 Center => new Vec2(X + (Width / 2), Y + (Height / 2));

		/// <summary>
		/// Gets if the dimensions of the rectangle are positive.
		/// </summary>
		public readonly bool IsReal => (Width >= 0) && (Height >= 0);

		/// <summary>
		/// The range of values covered by this rect on the x-axis.
		/// </summary>
		public readonly ValueRange<float> RangeX => new ValueRange<float>(X, X + Width);

		/// <summary>
		/// The range of values covered by this rect on the y-axis.
		/// </summary>
		public readonly ValueRange<float> RangeY => new ValueRange<float>(Y, Y + Height);
		#endregion // Fields

		#region Ctor
		/// <summary>
		/// Constructs a new rectangle from the given coordinates and dimensions
		/// </summary>
		/// <param name="x">The x-coordinate of the left side.</param>
		/// <param name="y">The y-coordinate of the bottom side.</param>
		/// <param name="w">The width.</param>
		/// <param name="h">The height.</param>
		public Rectf(float x, float y, float w, float h)
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
		public Rectf(in Vec2 pos, in Extentf ex)
		{
			X = pos.X;
			Y = pos.Y;
			Width = ex.Width;
			Height = ex.Height;
		}
		#endregion // Ctor

		#region Overrides
		readonly bool IEquatable<Rectf>.Equals(Rectf other) => other == this;

		public readonly override bool Equals(object obj) => (obj is Rectf) && ((Rectf)obj == this);

		public readonly override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + X.GetHashCode();
				hash = (hash * 23) + Y.GetHashCode();
				hash = (hash * 23) + Width.GetHashCode();
				hash = (hash * 23) + Height.GetHashCode();
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
		public static Rectf Union(in Rectf r1, in Rectf r2)
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
		public static void Union(in Rectf r1, in Rectf r2, out Rectf o)
		{
			o.X = Math.Min(r1.X, r2.X);
			o.Y = Math.Min(r1.Y, r2.Y);
			o.Width = Math.Max(r1.X + r1.Width, r2.X + r2.Width) - o.X;
			o.Height = Math.Max(r1.Y + r1.Height, r2.Y + r2.Height) - o.Y;
		}

		/// <summary>
		/// Calculates the overlap between the two rectangular areas, if any.
		/// </summary>
		/// <param name="r1">The first rectangle.</param>
		/// <param name="r2">The second rectangle.</param>
		/// <returns>The overlap area, set to <see cref="Empty"/> if there is no overlap.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Rectf Intersect(in Rectf r1, in Rectf r2)
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
		public static void Intersect(in Rectf r1, in Rectf r2, out Rectf o)
		{
			if (r1.Intersects(r2))
			{
				o.X = Math.Max(r1.X, r2.X);
				o.Y = Math.Max(r1.Y, r2.Y);
				o.Width = Math.Min(r1.X + r1.Width, r2.X + r2.Width) - o.X;
				o.Height = Math.Min(r1.Y + r1.Height, r2.Y + r2.Height) - o.Y;
			}
			else
				o = Rectf.Empty;
		}

		/// <summary>
		/// Constructs the minimal rectangular area that encompasses both passed points as corners.
		/// </summary>
		/// <param name="p1">The first point to contain.</param>
		/// <param name="p2">The second point to contain.</param>
		/// <returns>The output area.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Rectf FromCorners(in Vec2 p1, in Vec2 p2)
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
		public static void FromCorners(in Vec2 p1, in Vec2 p2, out Rectf o)
		{
			o.X = Math.Min(p1.X, p2.X);
			o.Y = Math.Min(p1.Y, p2.Y);
			o.Width = Math.Max(p1.X, p2.X) - o.X;
			o.Height = Math.Max(p1.Y, p2.Y) - o.Y;
		}

		/// <summary>
		/// Constructs the minimal rectangular area that encompasses all of the passed points.
		/// </summary>
		/// <param name="pts">The collection of points to encompass.</param>
		/// <returns>The output area.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Rectf FromPoints(params Vec2[] pts)
		{
			FromPoints(out var o, pts);
			return o;
		}

		/// <summary>
		/// Constructs the minimal rectangular area that encompasses all of the passed points.
		/// </summary>
		/// <param name="o">The output area.</param>
		/// <param name="pts">The collection of points to encompass.</param>
		public static void FromPoints(out Rectf o, params Vec2[] pts)
		{
			float minx = Single.MaxValue, miny = Single.MaxValue;
			float maxx = Single.MinValue, maxy = Single.MinValue;
			foreach (var p in pts)
			{
				minx = (p.X < minx) ? p.X : minx;
				miny = (p.Y < miny) ? p.Y : miny;
				maxx = (p.X > maxx) ? p.X : maxx;
				maxy = (p.Y > maxy) ? p.Y : maxy;
			}
			o = new Rectf(minx, miny, maxx - minx, maxy - miny);
		}
		#endregion // Creation

		#region Operators
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Rectf l, in Rectf r) =>
			(l.X == r.X) && (l.Y == r.Y) && (l.Width == r.Width) && (l.Height == r.Height);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Rectf l, in Rectf r) =>
			(l.X != r.X) || (l.Y != r.Y) || (l.Width != r.Width) || (l.Height != r.Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Rectf (in Rect r) => new Rectf(r.X, r.Y, r.Width, r.Height);
		#endregion // Operators

		#region Tuples
		public readonly void Deconstruct(out float x, out float y, out float w, out float h)
		{
			x = X;
			y = Y;
			w = Width;
			h = Height;
		}

		public readonly void Deconstruct(out Vec2 pos, out Extentf ext)
		{
			pos.X = X;
			pos.Y = Y;
			ext.Width = Width;
			ext.Height = Height;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Rectf (in (float x, float y, float w, float h) tup) =>
			new Rectf(tup.x, tup.y, tup.w, tup.h);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Rectf (in (Vec2 pos, Extentf ext) tup) =>
			new Rectf(tup.pos, tup.ext);
		#endregion // Tuples
	}
}
