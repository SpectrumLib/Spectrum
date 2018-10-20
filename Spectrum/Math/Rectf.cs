using System;

namespace Spectrum
{
	/// <summary>
	/// Describes a rectangle on a 2D cartesian grid.
	/// </summary>
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
	}
}
