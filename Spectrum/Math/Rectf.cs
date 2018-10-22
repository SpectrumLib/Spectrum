using System;

namespace Spectrum
{
	/// <summary>
	/// Describes a rectangle on a 2D cartesian grid.
	/// </summary>
	/// <remarks>
	/// Note that while the dimensions of the rectangle can be negative, this is not accounted for by the struct and
	/// will give incorrect results. However, the struct will never produce rectangles with negative dimensions from
	/// non-negative inputs, so it is up to the programmer to ensure that they do not create rectangles with
	/// negative dimensions.
	/// </remarks>
	public struct Rectf : IEquatable<Rectf>
	{
		/// <summary>
		/// Represents an empty rectangle with zero dimensions.
		/// </summary>
		public static readonly Rectf Empty = new Rectf(0, 0, 0, 0);

		#region Fields
		/// <summary>
		/// The x-coordinate of the top left corner.
		/// </summary>
		public float X;
		/// <summary>
		/// The y-coordinate of the top-left corner.
		/// </summary>
		public float Y;
		/// <summary>
		/// The width of the rectangle.
		/// </summary>
		public float Width;
		/// <summary>
		/// The height of the rectangle.
		/// </summary>
		public float Height;

		/// <summary>
		/// Gets/sets the top-left corner of the rectangle.
		/// </summary>
		public Vec2 Position
		{
			get => new Vec2(X, Y);
			set { X = value.X; Y = value.Y; }
		}
		/// <summary>
		/// Gets/sets the size of the rectangle.
		/// </summary>
		public Vec2 Size
		{
			get => new Vec2(Width, Height);
			set { Width = value.X; Height = value.Y; }
		}

		/// <summary>
		/// Gets the coordinate of the top-left corner.
		/// </summary>
		public Vec2 TopLeft => new Vec2(X, Y);
		/// <summary>
		/// Gets the coordinate of the top-right corner.
		/// </summary>
		public Vec2 TopRight => new Vec2(X + Width, Y);
		/// <summary>
		/// Gets the coordinate of the bottom-left corner.
		/// </summary>
		public Vec2 BottomLeft => new Vec2(X, Y - Height);
		/// <summary>
		/// Gets the coordinate of the bottom-right corner.
		/// </summary>
		public Vec2 BottomRight => new Vec2(X + Width, Y - Height);

		/// <summary>
		/// The x-coorindate of the left side.
		/// </summary>
		public float Left => X;
		/// <summary>
		/// The x-coorindate of the right side.
		/// </summary>
		public float Right => X + Width;
		/// <summary>
		/// The x-coorindate of the top side.
		/// </summary>
		public float Top => Y;
		/// <summary>
		/// The x-coorindate of the bottom side.
		/// </summary>
		public float Bottom => Y - Height;

		/// <summary>
		/// Gets the center point of the rectangle.
		/// </summary>
		public Vec2 Center => new Vec2(X + (Width / 2), Y - (Height / 2));

		/// <summary>
		/// Gets if the rectangle has zero dimensions and is at the origin.
		/// </summary>
		public bool IsEmpty => (X == 0) && (Y == 0) && (Width == 0) && (Height == 0);
		#endregion // Fields

		/// <summary>
		/// Creates a new rectangle from the passed coordinates and dimensions.
		/// </summary>
		/// <param name="x">The x-coordinate of the top-left corner.</param>
		/// <param name="y">The y-coordinate of the top-left corner.</param>
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
		/// Creates a new rectangle from the passed coordinates and dimensions.
		/// </summary>
		/// <param name="pos">The location of the top-left corner.</param>
		/// <param name="size">The dimensions of the rectangle.</param>
		public Rectf(in Vec2 pos, in Vec2 size)
		{
			X = pos.X;
			Y = pos.Y;
			Width = size.X;
			Height = size.Y;
		}

		public static explicit operator Rect (in Rectf r)
		{
			return new Rect((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
		}

		#region Overrides
		bool IEquatable<Rectf>.Equals(Rectf other)
		{
			return (X == other.X) && (Y == other.Y) && (Width == other.Width) && (Height == other.Height);
		}

		public override bool Equals(object obj)
		{
			return (obj as Rectf?)?.Equals(this) ?? false;
		}

		public override int GetHashCode()
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

		public override string ToString()
		{
			return $"{{{X} {Y} {Width} {Height}}}";
		}
		#endregion // Overrides

		#region Combining
		/// <summary>
		/// Returns a rectangle covering the overlap between the two passed rectangles.
		/// </summary>
		/// <param name="r1">The first rectangle.</param>
		/// <param name="r2">The second rectangle.</param>
		/// <returns>The overlap, or <see cref="Rectf.Empty"/> if there is no overlap.</returns>
		public static Rectf Intersect(in Rectf r1, in Rectf r2)
		{
			Intersect(r1, r2, out Rectf o);
			return o;
		}

		/// <summary>
		/// Returns a rectangle covering the overlap between the two passed rectangles.
		/// </summary>
		/// <param name="r1">The first rectangle.</param>
		/// <param name="r2">The second rectangle.</param>
		/// <param name="o">The overlap, or <see cref="Rectf.Empty"/> if there is no overlap.</param>
		public static void Intersect(in Rectf r1, in Rectf r2, out Rectf o)
		{
			if (r2.Left < r1.Right && r1.Left < r2.Right && r2.Top > r1.Bottom && r1.Top > r2.Bottom)
			{
				float r = Math.Min(r1.X + r1.Width, r2.X + r2.Width);
				float l = Math.Max(r1.X, r2.X);
				float t = Math.Min(r1.Y, r2.Y);
				float b = Math.Max(r1.Y - r1.Height, r2.Y - r2.Height);
				o.X = l;
				o.Y = t;
				o.Width = r - l;
				o.Height = t - b;
			}
			else
				o = Rect.Empty;
		}

		/// <summary>
		/// Returns a rectangle that contains both passed rectangles within it.
		/// </summary>
		/// <param name="r1">The first rectangle.</param>
		/// <param name="r2">The second rectangle.</param>
		public static Rectf Union(in Rectf r1, in Rectf r2)
		{
			Union(r1, r2, out Rectf o);
			return o;
		}

		/// <summary>
		/// Returns a rectangle that contains both passed rectangles within it.
		/// </summary>
		/// <param name="r1">The first rectangle.</param>
		/// <param name="r2">The second rectangle.</param>
		/// <param name="o">The output rectangle.</param>
		public static void Union(in Rectf r1, in Rectf r2, out Rectf o)
		{
			o.X = Math.Min(r1.X, r2.X);
			o.Y = Math.Max(r1.Y, r2.Y);
			o.Width = Math.Max(r1.X + r1.Width, r2.X + r2.Width) - o.X;
			o.Height = o.Y - Math.Min(r1.Y - r1.Height, r2.Y - r2.Height);
		}
		#endregion // Combining
	}
}
