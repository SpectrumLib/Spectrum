/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.CompilerServices;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Describes the output region of rendering commands to a render target. Note that this type describes a region
	/// mask to output pixels to unscaled. For scaled rendering areas, use <see cref="Viewport"/>.
	/// </summary>
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
		public readonly Rect Bounds => new Rect((int)X, (int)Y, Width, Height);

		/// <summary>
		/// Gets a scissor describing the left half of this scissor.
		/// </summary>
		public readonly Scissor Left => new Scissor(X, Y, Width / 2, Height);
		/// <summary>
		/// Gets a scissor describing the right half of this scissor.
		/// </summary>
		public readonly Scissor Right => new Scissor(X + (Width / 2), Y, Width / 2, Height);
		/// <summary>
		/// Gets a scissor describing the top half of this scissor.
		/// </summary>
		public readonly Scissor Top => new Scissor(X, Y, Width, Height / 2);
		/// <summary>
		/// Gets a scissor describing the bottom half of this scissor.
		/// </summary>
		public readonly Scissor Bottom => new Scissor(X, Y + (Height / 2), Width, Height / 2);
		/// <summary>
		/// Gets a scissor describing the top-left quarter of this scissor.
		/// </summary>
		public readonly Scissor TopLeft => new Scissor(X, Y, Width / 2, Height / 2);
		/// <summary>
		/// Gets a scissor describing the top-right quarter of this scissor.
		/// </summary>
		public readonly Scissor TopRight => new Scissor(X + (Width / 2), Y, Width / 2, Height / 2);
		/// <summary>
		/// Gets a scissor describing the bottom-left quarter of this scissor.
		/// </summary>
		public readonly Scissor BottomLeft => new Scissor(X, Y + (Height / 2), Width / 2, Height / 2);
		/// <summary>
		/// Gets a scissor describing the bottom-right quarter of this scissor.
		/// </summary>
		public readonly Scissor BottomRight => new Scissor(X + (Width / 2), Y + (Height / 2), Width / 2, Height / 2);
		#endregion // Fields

		#region Ctor
		/// <summary>
		/// Creates a new scissor.
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
		#endregion Ctor

		#region Overrides
		public readonly override string ToString() => $"{{{X}x{Y}x{Width}x{Height}}}";

		public readonly override int GetHashCode()
		{
			unchecked
			{
				int hash = 17 * (23 + X.GetHashCode());
				hash *= (23 + Y.GetHashCode());
				hash *= (23 + Width.GetHashCode());
				return hash * (23 + Height.GetHashCode());
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly override bool Equals(object obj) => (obj is Scissor) && (((Scissor)obj) == this);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		readonly bool IEquatable<Scissor>.Equals(Scissor other) =>
			(other.X == X) && (other.Y == Y) && (other.Width == Width) && (other.Height == Height);
		#endregion // Overrides

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Scissor l, in Scissor r) =>
			(l.X == r.X) && (l.Y == r.Y) && (l.Width == r.Width) && (l.Height == r.Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Scissor l, in Scissor r) =>
			(l.X != r.X) || (l.Y != r.Y) || (l.Width != r.Width) || (l.Height != r.Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Vk.Rect2D ToVulkanType() => new Vk.Rect2D(new Vk.Offset2D((int)X, (int)Y), new Vk.Extent2D(Width, Height));
	}
}
