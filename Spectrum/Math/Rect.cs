using System;

namespace Spectrum
{
	/// <summary>
	/// Describes a rectangle on a 2D cartesian integer grid.
	/// </summary>
	public struct Rect : IEquatable<Rect>
	{
		/// <summary>
		/// Represents an empty rectangle with zero dimensions.
		/// </summary>
		public static readonly Rect Empty = new Rect(0, 0, 0, 0);

		#region Fields
		/// <summary>
		/// The x-coordinate of the top left corner.
		/// </summary>
		public int X;
		/// <summary>
		/// The y-coordinate of the top-left corner.
		/// </summary>
		public int Y;
		/// <summary>
		/// The width of the rectangle.
		/// </summary>
		public int Width;
		/// <summary>
		/// The height of the rectangle.
		/// </summary>
		public int Height;

		/// <summary>
		/// Gets/sets the top-left corner of the rectangle.
		/// </summary>
		public Point Position
		{
			get => new Point(X, Y);
			set { X = value.X; Y = value.Y; }
		}
		/// <summary>
		/// Gets/sets the size of the rectangle.
		/// </summary>
		public Point Size
		{
			get => new Point(Width, Height);
			set { Width = value.X; Height = value.Y; }
		}

		/// <summary>
		/// Gets the coordinate of the top-left corner.
		/// </summary>
		public Point TopLeft => new Point(X, Y);
		/// <summary>
		/// Gets the coordinate of the top-right corner.
		/// </summary>
		public Point TopRight => new Point(X + Width, Y);
		/// <summary>
		/// Gets the coordinate of the bottom-left corner.
		/// </summary>
		public Point BottomLeft => new Point(X, Y - Height);
		/// <summary>
		/// Gets the coordinate of the bottom-right corner.
		/// </summary>
		public Point BottomRight => new Point(X + Width, Y - Height);

		/// <summary>
		/// The x-coorindate of the left side.
		/// </summary>
		public int Left => X;
		/// <summary>
		/// The x-coorindate of the right side.
		/// </summary>
		public int Right => X + Width;
		/// <summary>
		/// The x-coorindate of the top side.
		/// </summary>
		public int Top => Y;
		/// <summary>
		/// The x-coorindate of the bottom side.
		/// </summary>
		public int Bottom => Y - Height;

		/// <summary>
		/// Gets the center point of the rectangle, rounded towards the top-left center if needed.
		/// </summary>
		public Point Center => new Point(X + (Width / 2), Y - (Height / 2));
		/// <summary>
		/// Gets the center point of the rectangle, using floating point instead of rounding.
		/// </summary>
		public Vec2 CenterF => new Vec2(X + (Width / 2f), Y - (Height / 2f));
		#endregion // Fields

		/// <summary>
		/// Creates a new rectangle from the passed coordinates and dimensions.
		/// </summary>
		/// <param name="x">The x-coordinate of the top-left corner.</param>
		/// <param name="y">The y-coordinate of the top-left corner.</param>
		/// <param name="w">The width.</param>
		/// <param name="h">The height.</param>
		public Rect(int x, int y, int w, int h)
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
		public Rect(in Point pos, in Point size)
		{
			X = pos.X;
			Y = pos.Y;
			Width = size.X;
			Height = size.Y;
		}

		#region Overrides
		bool IEquatable<Rect>.Equals(Rect other)
		{
			return (X == other.X) && (Y == other.Y) && (Width == other.Width) && (Height == other.Height);
		}

		public override bool Equals(object obj)
		{
			return (obj as Rect?)?.Equals(this) ?? false;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + X;
				hash = (hash * 23) + Y;
				hash = (hash * 23) + Width;
				hash = (hash * 23) + Height;
				return hash;
			}
		}

		public override string ToString()
		{
			return $"{{{X} {Y} {Width} {Height}}}";
		}
		#endregion // Overrides
	}
}
