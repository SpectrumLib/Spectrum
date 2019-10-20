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

		public readonly override string ToString() => $"{{{X}x{Y}x{Width}x{Height}}}";

		public readonly override int GetHashCode()
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
		public readonly override bool Equals(object obj) => (obj is Scissor) && (((Scissor)obj) == this);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		readonly bool IEquatable<Scissor>.Equals(Scissor other) =>
			(other.X == X) && (other.Y == Y) && (other.Width == Width) && (other.Height == Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Scissor l, in Scissor r) =>
			(l.X == r.X) && (l.Y == r.Y) && (l.Width == r.Width) && (l.Height == r.Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Scissor l, in Scissor r) =>
			(l.X != r.X) || (l.Y != r.Y) || (l.Width != r.Width) || (l.Height != r.Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Vk.Rect2D (in Scissor s) =>
			new Vk.Rect2D { Offset = { X = (int)s.X, Y = (int)s.Y }, Extent = { Width = s.Width, Height = s.Height } };
	}
}
