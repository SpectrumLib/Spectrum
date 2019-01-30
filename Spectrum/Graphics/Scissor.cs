using System;
using System.Runtime.CompilerServices;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Defines the region of a render target that can have output written to it.
	/// </summary>
	/// <remarks>
	/// Unlike the <see cref="Viewport"/> type, this type controls the portion of the render target that is allowed to
	/// receive output, instead of scaling the entire output to a specific area of the render target.
	/// </remarks>
	public struct Scissor : IEquatable<Scissor>
	{
		#region Fields
		/// <summary>
		/// The left side of the scissor region.
		/// </summary>
		public uint X;
		/// <summary>
		/// The top of the scissor region.
		/// </summary>
		public uint Y;
		/// <summary>
		/// The width of the scissor region.
		/// </summary>
		public uint Width;
		/// <summary>
		/// The height of the scissor region.
		/// </summary>
		public uint Height;

		/// <summary>
		/// The bounds of the scissor region as a rectangle.
		/// </summary>
		public Rect Bounds => new Rect((int)X, (int)Y, (int)Width, (int)Height);

		/// <summary>
		/// Gets a scissor describing the left half of this scissor.
		/// </summary>
		public Scissor Left => new Scissor(X, Y, Width / 2, Height);
		/// <summary>
		/// Gets a scissor describing the right half of this scissor.
		/// </summary>
		public Scissor Right => new Scissor(X + (Width / 2), Y, Width / 2, Height);
		/// <summary>
		/// Gets a scissor describing the top half of this scissor.
		/// </summary>
		public Scissor Top => new Scissor(X, Y, Width, Height / 2);
		/// <summary>
		/// Gets a scissor describing the bottom half of this scissor.
		/// </summary>
		public Scissor Bottom => new Scissor(X, Y + (Height / 2), Width, Height / 2);
		/// <summary>
		/// Gets a scissor describing the top-left quarter of this scissor.
		/// </summary>
		public Scissor TopLeft => new Scissor(X, Y, Width / 2, Height / 2);
		/// <summary>
		/// Gets a scissor describing the top-right quarter of this scissor.
		/// </summary>
		public Scissor TopRight => new Scissor(X + (Width / 2), Y, Width / 2, Height / 2);
		/// <summary>
		/// Gets a scissor describing the bottom-left quarter of this scissor.
		/// </summary>
		public Scissor BottomLeft => new Scissor(X, Y + (Height / 2), Width / 2, Height / 2);
		/// <summary>
		/// Gets a scissor describing the bottom-right quarter of this scissor.
		/// </summary>
		public Scissor BottomRight => new Scissor(X + (Width / 2), Y + (Height / 2), Width / 2, Height / 2);
		#endregion // Fields

		/// <summary>
		/// Creates a new scissor from normlized target coordinates.
		/// </summary>
		/// <param name="x">The left side of the scissor region.</param>
		/// <param name="y">The top of the scissor region.</param>
		/// <param name="w">The width of the scissor region.</param>
		/// <param name="h">The height of the scissor region.</param>
		public Scissor(uint x, uint y, uint w, uint h)
		{
			X = x;
			Y = y;
			Width = w;
			Height = h;
		}

		public override string ToString() => $"{{{X}x{Y}x{Width}x{Height}}}";

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 14461 * (5051 + X.GetHashCode());
				hash *= (5051 + Y.GetHashCode());
				hash *= (5051 + Width.GetHashCode());
				return hash * (5051 + Height.GetHashCode());
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object obj) => (obj is Scissor) && (((Scissor)obj) == this);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool IEquatable<Scissor>.Equals(Scissor other) =>
			(other.X == X) && (other.Y == Y) && (other.Width == Width) && (other.Height == Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Scissor l, in Scissor r) =>
			(l.X == r.X) && (l.Y == r.Y) && (l.Width == r.Width) && (l.Height == r.Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Scissor l, in Scissor r) =>
			(l.X != r.X) || (l.Y != r.Y) || (l.Width != r.Width) || (l.Height != r.Height);

		internal Vk.Rect2D ToVulkanNative() => 
			new Vk.Rect2D((int)X, (int)Y, (int)Width, (int)Height);
	}
}
