/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Describes a region of texels within a texture, using the standard (0, 0, 0) as the top-left-front.
	/// </summary>
	public struct TextureRegion : IEquatable<TextureRegion>
	{
		#region Fields
		/// <summary>
		/// The x-coordinate of the region start.
		/// </summary>
		public uint X;
		/// <summary>
		/// The y-coordinate of the region start.
		/// </summary>
		public uint Y;
		/// <summary>
		/// The z-coordinate of the region start.
		/// </summary>
		public uint Z;
		/// <summary>
		/// The size of the region in the x-axis. Must be at least 1 for all texture types.
		/// </summary>
		public uint Width;
		/// <summary>
		/// The size of the region in the y-axis. Must be at least 1 for all texture types.
		/// </summary>
		public uint Height;
		/// <summary>
		/// The size of the region in the z-axis. Must be at least 1 for all texture types.
		/// </summary>
		public uint Depth;

		/// <summary>
		/// The maximum x-coordinate of the region.
		/// </summary>
		public readonly uint XMax => X + Width;
		/// <summary>
		/// The maximum y-coordinate of the region.
		/// </summary>
		public readonly uint YMax => Y + Height;
		/// <summary>
		/// The maximum z-coordinate of the region.
		/// </summary>
		public readonly uint ZMax => Z + Depth;

		/// <summary>
		/// The number of texels that are contained inside of this region.
		/// </summary>
		public readonly uint TexelCount => Width * Height * Depth;

		// Quick casting to the Vulkan objects that this region describes
		internal readonly Vk.Offset3D Offset => new Vk.Offset3D((int)X, (int)Y, (int)Z);
		internal readonly Vk.Extent3D Extent => new Vk.Extent3D(Width, Height, Depth);
		#endregion // Fields

		#region Ctor
		/// <summary>
		/// Creates a region that describes an offset and size in a 1D texture.
		/// </summary>
		/// <param name="x">The x-coordinate of the region start.</param>
		/// <param name="w">The size of the region in the x-axis.</param>
		public TextureRegion(uint x, uint w) =>
			(X, Y, Z, Width, Height, Depth) = (x, 0, 0, w, 1, 1);

		/// <summary>
		/// Creates a region that describes an offset and size in a 2D texture.
		/// </summary>
		/// <param name="x">The x-coordinate of the region start.</param>
		/// <param name="y">The y-coordinate of the region start.</param>
		/// <param name="w">The size of the region in the x-axis.</param>
		/// <param name="h">The size of the region in the y-axis.</param>
		public TextureRegion(uint x, uint y, uint w, uint h) =>
			(X, Y, Z, Width, Height, Depth) = (x, y, 0, w, h, 1);

		/// <summary>
		/// Creates a region that describes an offset and size in a 3D texture.
		/// </summary>
		/// <param name="x">The x-coordinate of the region start.</param>
		/// <param name="y">The y-coordinate of the region start.</param>
		/// <param name="z">The z-coordinate of the region start.</param>
		/// <param name="w">The size of the region in the x-axis.</param>
		/// <param name="h">The size of the region in the y-axis.</param>
		/// <param name="d">The size of the region in the z-axis.</param>
		public TextureRegion(uint x, uint y, uint z, uint w, uint h, uint d) =>
			(X, Y, Z, Width, Height, Depth) = (x, y, z, w, h, d);
		#endregion // Ctor

		public readonly override string ToString() => $"{{{{{X}:{XMax}}}x{{{Y}:{YMax}}}x{{{Z}:{ZMax}}}}}";

		public readonly override int GetHashCode()
		{
			unchecked
			{
				uint hash = 14461 * (5051 + X);
				hash *= (5051 + Y);
				hash *= (5051 + Z);
				hash *= (5051 + Width);
				hash *= (5051 + Height);
				return (int)(hash * (5051 + Depth));
			}
		}

		public readonly override bool Equals(object obj) => (obj is TextureRegion) && (((TextureRegion)obj) == this);

		readonly bool IEquatable<TextureRegion>.Equals(TextureRegion other) =>
			(other.X == X) && (other.Y == Y) && (other.Z == Z) && (other.Width == Width) && (other.Height == Height) &&
			(other.Depth == Depth);

		public static bool operator == (in TextureRegion l, in TextureRegion r) =>
			(l.X == r.X) && (l.Y == r.Y) && (l.Z == r.Z) && (l.Width == r.Width) && (l.Height == r.Height) &&
			(l.Depth == r.Depth);

		public static bool operator != (in TextureRegion l, in TextureRegion r) =>
			(l.X != r.X) || (l.Y != r.Y) || (l.Z != r.Z) || (l.Width != r.Width) || (l.Height != r.Height) ||
			(l.Depth != r.Depth);

		public static implicit operator TextureRegion (in (uint x, uint w) tup) =>
			new TextureRegion(tup.x, tup.w);

		public static implicit operator TextureRegion (in (uint x, uint y, uint w, uint h) tup) =>
			new TextureRegion(tup.x, tup.y, tup.w, tup.h);

		public static implicit operator TextureRegion (in (uint x, uint y, uint z, uint w, uint h, uint d) tup) =>
			new TextureRegion(tup.x, tup.y, tup.z, tup.w, tup.h, tup.d);
	}
}
