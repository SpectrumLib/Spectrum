﻿using System;
using System.Runtime.CompilerServices;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Describes the output region of rendering commands to a render target.
	/// </summary>
	/// <remarks>
	/// Unlike the <see cref="Scissor"/> type, this type actually scales the entire output to a region, instead of
	/// defining the region that output can be written to.
	/// </remarks>
	public struct Viewport : IEquatable<Viewport>
	{
		#region Fields
		/// <summary>
		/// The left side of the viewport.
		/// </summary>
		public uint X;
		/// <summary>
		/// The top of the viewport.
		/// </summary>
		public uint Y;
		/// <summary>
		/// The width of the viewport.
		/// </summary>
		public uint Width;
		/// <summary>
		/// The height of the viewport.
		/// </summary>
		public uint Height;
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
		public Rect Bounds => new Rect((int)X, (int)Y, (int)Width, (int)Height);
		/// <summary>
		/// The aspect ratio of the viewport.
		/// </summary>
		public float Aspect => (Height == 0) ? 0f : ((float)Width / Height);

		/// <summary>
		/// Gets a viewport describing the left half of this viewport.
		/// </summary>
		public Viewport Left => new Viewport(X, Y, Width / 2, Height);
		/// <summary>
		/// Gets a viewport describing the right half of this viewport.
		/// </summary>
		public Viewport Right => new Viewport(X + (Width / 2), Y, Width / 2, Height);
		/// <summary>
		/// Gets a viewport describing the top half of this viewport.
		/// </summary>
		public Viewport Top => new Viewport(X, Y, Width, Height / 2);
		/// <summary>
		/// Gets a viewport describing the bottom half of this viewport.
		/// </summary>
		public Viewport Bottom => new Viewport(X, Y + (Height / 2), Width, Height / 2);
		/// <summary>
		/// Gets a viewport describing the top-left quarter of this viewport.
		/// </summary>
		public Viewport TopLeft => new Viewport(X, Y, Width / 2, Height / 2);
		/// <summary>
		/// Gets a viewport describing the top-right quarter of this viewport.
		/// </summary>
		public Viewport TopRight => new Viewport(X + (Width / 2), Y, Width / 2, Height / 2);
		/// <summary>
		/// Gets a viewport describing the bottom-left quarter of this viewport.
		/// </summary>
		public Viewport BottomLeft => new Viewport(X, Y + (Height / 2), Width / 2, Height / 2);
		/// <summary>
		/// Gets a viewport describing the bottom-right quarter of this viewport.
		/// </summary>
		public Viewport BottomRight => new Viewport(X + (Width / 2), Y + (Height / 2), Width / 2, Height / 2);
		#endregion // Fields

		/// <summary>
		/// Creates a new viewport.
		/// </summary>
		/// <param name="x">The left side of the viewport.</param>
		/// <param name="y">The top of the viewport.</param>
		/// <param name="w">The width of the viewport.</param>
		/// <param name="h">The height of the viewport.</param>
		public Viewport(uint x, uint y, uint w, uint h)
		{
			X = x;
			Y = y;
			Width = w;
			Height = h;
			MinDepth = 0;
			MaxDepth = 1;
		}

		/// <summary>
		/// Creates a new viewport.
		/// </summary>
		/// <param name="x">The left side of the viewport.</param>
		/// <param name="y">The top of the viewport.</param>
		/// <param name="w">The width of the viewport.</param>
		/// <param name="h">The height of the viewport.</param>
		/// <param name="min">The minimum value of the viewport depth.</param>
		/// <param name="max">The maximum value of the viewport depth.</param>
		public Viewport(uint x, uint y, uint w, uint h, float min, float max)
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

		internal Vk.Viewport ToVulkanNative() => new Vk.Viewport(X, Y, Width, Height, MinDepth, MaxDepth);
	}
}
