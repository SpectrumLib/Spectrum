using System;
using System.Runtime.CompilerServices;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Describes the output region of rendering commands to a render target. All values are normalized to [0, 1].
	/// </summary>
	/// <remarks>
	/// Unlike the <see cref="Scissor"/> type, this type actually scales the entire output to a region, instead of
	/// defining the region that output is allowed to be written to.
	/// </remarks>
	public struct Viewport : IEquatable<Viewport>
	{
		#region Constants
		/// <summary>
		/// The entire target.
		/// </summary>
		public static readonly Viewport Full = new Viewport(0, 0, 1, 1);
		/// <summary>
		/// The left half of the target.
		/// </summary>
		public static readonly Viewport Left = new Viewport(0, 0, 0.5f, 1);
		/// <summary>
		/// The right half of the target.
		/// </summary>
		public static readonly Viewport Right = new Viewport(0.5f, 0, 0.5f, 1);
		/// <summary>
		/// The top half of the target.
		/// </summary>
		public static readonly Viewport Top = new Viewport(0, 0, 1, 0.5f);
		/// <summary>
		/// The bottom half of the target.
		/// </summary>
		public static readonly Viewport Bottom = new Viewport(0, 0.5f, 1, 0.5f);
		/// <summary>
		/// The top-left quarter of the target.
		/// </summary>
		public static readonly Viewport TopLeft = new Viewport(0, 0, 0.5f, 0.5f);
		/// <summary>
		/// The top-right quarter of the target.
		/// </summary>
		public static readonly Viewport TopRight = new Viewport(0.5f, 0, 0.5f, 0.5f);
		/// <summary>
		/// The bottom-left quarter of the target.
		/// </summary>
		public static readonly Viewport BottomLeft = new Viewport(0, 0.5f, 0.5f, 0.5f);
		/// <summary>
		/// The bottom-right quarter of the target.
		/// </summary>
		public static readonly Viewport BottomRight = new Viewport(0.5f, 0.5f, 0.5f, 0.5f);
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
		/// The minimum depth value, should be left at zero except for special cases.
		/// </summary>
		public float MinDepth;
		/// <summary>
		/// The maximum depth value, should be left at one except for special cases.
		/// </summary>
		public float MaxDepth;

		/// <summary>
		/// The bounds of the viewport as a rectangle.
		/// </summary>
		public Rectf Bounds => new Rectf(X, Y, Width, Height);

		/// <summary>
		/// The aspect ratio of the viewport.
		/// </summary>
		public float Aspect => MathUtils.NearlyEqual(Height, 0f) ? 0f : (Width / Height);
		#endregion // Fields

		/// <summary>
		/// Creates a new viewport from normlized target coordinates.
		/// </summary>
		/// <param name="x">The left side of the viewport.</param>
		/// <param name="y">The right side of the viewport.</param>
		/// <param name="w">The width of the viewport.</param>
		/// <param name="h">The height of the viewport.</param>
		public Viewport(float x, float y, float w, float h)
		{
			X = x;
			Y = y;
			Width = w;
			Height = h;
			MinDepth = 0;
			MaxDepth = 1;
		}

		/// <summary>
		/// Creates a new viewport from normlized target coordinates.
		/// </summary>
		/// <param name="x">The left side of the viewport.</param>
		/// <param name="y">The right side of the viewport.</param>
		/// <param name="w">The width of the viewport.</param>
		/// <param name="h">The height of the viewport.</param>
		/// <param name="min">The minimum value of the viewport depth.</param>
		/// <param name="max">The maximum value of the viewport depth.</param>
		public Viewport(float x, float y, float w, float h, float min, float max)
		{
			X = x;
			Y = y;
			Width = w;
			Height = h;
			MinDepth = min;
			MaxDepth = max;
		}

		public override string ToString() => $"{{{X}x{Y}x{Width}x{Height} [{MinDepth},{MaxDepth}]}}";

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
		public override bool Equals(object obj) => (obj is Viewport) && (((Viewport)obj) == this);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool IEquatable<Viewport>.Equals(Viewport other) =>
			(other.X == X) && (other.Y == Y) && (other.Width == Width) && (other.Height == Height) && (other.MinDepth == MinDepth) && (other.MaxDepth == MaxDepth);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Viewport l, in Viewport r) =>
			(l.X == r.X) && (l.Y == r.Y) && (l.Width == r.Width) && (l.Height == r.Height) && (l.MinDepth == r.MinDepth) && (l.MaxDepth == r.MaxDepth);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Viewport l, in Viewport r) =>
			(l.X != r.X) || (l.Y != r.Y) || (l.Width != r.Width) || (l.Height != r.Height) || (l.MinDepth != r.MinDepth) || (l.MaxDepth != r.MaxDepth);

		internal Vk.Viewport ToVulkanNative(int w, int h) =>
			new Vk.Viewport(X * w, Y * h, Width * w, Height * h, MinDepth, MaxDepth);
	}
}
