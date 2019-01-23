using System;
using System.Runtime.CompilerServices;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Defines the region of a rendertarget that can have output written to it. All values are normalized to [0, 1].
	/// </summary>
	/// <remarks></remarks>
	public struct Scissor : IEquatable<Scissor>
	{
		#region Constants
		/// <summary>
		/// The entire target.
		/// </summary>
		public static readonly Scissor Full = new Scissor(0, 0, 1, 1);
		/// <summary>
		/// The left half of the target.
		/// </summary>
		public static readonly Scissor Left = new Scissor(0, 0, 0.5f, 1);
		/// <summary>
		/// The right half of the target.
		/// </summary>
		public static readonly Scissor Right = new Scissor(0.5f, 0, 0.5f, 1);
		/// <summary>
		/// The top half of the target.
		/// </summary>
		public static readonly Scissor Top = new Scissor(0, 0, 1, 0.5f);
		/// <summary>
		/// The bottom half of the target.
		/// </summary>
		public static readonly Scissor Bottom = new Scissor(0, 0.5f, 1, 0.5f);
		/// <summary>
		/// The top-left quarter of the target.
		/// </summary>
		public static readonly Scissor TopLeft = new Scissor(0, 0, 0.5f, 0.5f);
		/// <summary>
		/// The top-right quarter of the target.
		/// </summary>
		public static readonly Scissor TopRight = new Scissor(0.5f, 0, 0.5f, 0.5f);
		/// <summary>
		/// The bottom-left quarter of the target.
		/// </summary>
		public static readonly Scissor BottomLeft = new Scissor(0, 0.5f, 0.5f, 0.5f);
		/// <summary>
		/// The bottom-right quarter of the target.
		/// </summary>
		public static readonly Scissor BottomRight = new Scissor(0.5f, 0.5f, 0.5f, 0.5f);
		#endregion // Constants

		#region Fields
		/// <summary>
		/// The normalized left side of the viewport.
		/// </summary>
		public float X;
		/// <summary>
		/// The normalized right side of the viewport.
		/// </summary>
		public float Y;
		/// <summary>
		/// The normalized width of the viewport.
		/// </summary>
		public float Width;
		/// <summary>
		/// The normalized height of the viewport.
		/// </summary>
		public float Height;

		/// <summary>
		/// The bounds of the viewport as a rectangle.
		/// </summary>
		public Rectf Bounds => new Rectf(X, Y, Width, Height);
		#endregion // Fields

		/// <summary>
		/// Creates a new scissor from normlized target coordinates.
		/// </summary>
		/// <param name="x">The left side of the scissor.</param>
		/// <param name="y">The right side of the scissor.</param>
		/// <param name="w">The width of the scissor.</param>
		/// <param name="h">The height of the scissor.</param>
		public Scissor(float x, float y, float w, float h)
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

		internal Vk.Rect2D ToVulkanNative(int w, int h) => 
			new Vk.Rect2D((int)(X * w), (int)(Y * h), (int)(Width * w), (int)(Height * h));
	}
}
