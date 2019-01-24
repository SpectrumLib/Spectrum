using System;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Describes a region of texels within an texture, using the standard of (0,0,0) being the top-left-near corner of
	/// the texture data, as seen face-on. This type can also be quickly built from uint tuples of length 2, 4, or 6.
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
		/// The size of the region in the x-axis.
		/// </summary>
		public uint Width;
		/// <summary>
		/// The size of the region in the y-axis.
		/// </summary>
		public uint Height;
		/// <summary>
		/// The size of the region in the z-axis.
		/// </summary>
		public uint Depth;

		/// <summary>
		/// The maximum x-coordinate of the region.
		/// </summary>
		public uint XMax => X + Width;
		/// <summary>
		/// The maximum y-coordinate of the region.
		/// </summary>
		public uint YMax => Y + Height;
		/// <summary>
		/// The maximum z-coordinate of the region.
		/// </summary>
		public uint ZMax => Z + Depth;

		// Quick casting to the Vulkan objects that this region describes
		internal Vk.Offset3D Offset => new Vk.Offset3D((int)X, (int)Y, (int)Z);
		internal Vk.Extent3D Extent => new Vk.Extent3D((int)Width, (int)Height, (int)Depth);
		#endregion // Fields

		/// <summary>
		/// Creates a region that describes an offset and size in a 1D texture.
		/// </summary>
		/// <param name="x">The x-coordinate of the region start.</param>
		/// <param name="w">The size of the region in the x-axis.</param>
		public TextureRegion(uint x, uint w)
		{
			X = x;
			Y = 1;
			Z = 1;
			Width = w;
			Height = 1;
			Depth = 1;
		}

		/// <summary>
		/// Creates a region that describes an offset and size in a 2D texture.
		/// </summary>
		/// <param name="x">The x-coordinate of the region start.</param>
		/// <param name="y">The y-coordinate of the region start.</param>
		/// <param name="w">The size of the region in the x-axis.</param>
		/// <param name="h">The size of the region in the y-axis.</param>
		public TextureRegion(uint x, uint y, uint w, uint h)
		{
			X = x;
			Y = y;
			Z = 1;
			Width = w;
			Height = h;
			Depth = 1;
		}

		/// <summary>
		/// Creates a region that describes an offset and size in a 3D texture.
		/// </summary>
		/// <param name="x">The x-coordinate of the region start.</param>
		/// <param name="y">The y-coordinate of the region start.</param>
		/// <param name="z">The z-coordinate of the region start.</param>
		/// <param name="w">The size of the region in the x-axis.</param>
		/// <param name="h">The size of the region in the y-axis.</param>
		/// <param name="d">The size of the region in the z-axis.</param>
		public TextureRegion(uint x, uint y, uint z, uint w, uint h, uint d)
		{
			X = x;
			Y = y;
			Z = z;
			Width = w;
			Height = h;
			Depth = d;
		}

		/// <summary>
		/// Checks if the region is a valid descriptor for the given texture type.
		/// </summary>
		/// <param name="type">The texture type to validate for.</param>
		/// <returns>If the region is valid.</returns>
		public bool ValidFor(TextureType type)
		{
			return (Width >= 1) && (
				(type == TextureType.Texture1D) ? (Height == 1 && Depth == 1) :
				(type == TextureType.Texture2D) ? (Height >= 1 && Depth == 1) :
				(Height >= 1 && Depth >= 1)
			);
		}

		public override string ToString() => $"{{{{{X}:{XMax}}}x{{{Y}:{YMax}}}x{{{Z}:{ZMax}}}}}";

		public override int GetHashCode()
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

		public override bool Equals(object obj) => (obj is TextureRegion) && (((TextureRegion)obj) == this);

		bool IEquatable<TextureRegion>.Equals(TextureRegion other) =>
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
