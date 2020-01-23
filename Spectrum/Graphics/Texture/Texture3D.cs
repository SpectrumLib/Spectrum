/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	/// <summary>
	/// A three-dimensional texture type, storing a volume of texels.
	/// </summary>
	public sealed class Texture3D : Texture
	{
		#region Fields
		/// <summary>
		/// The width of the texture.
		/// </summary>
		public uint Width => Dimensions.Width;

		/// <summary>
		/// The height of the texture.
		/// </summary>
		public uint Height => Dimensions.Height;

		/// <summary>
		/// The depth of the texture.
		/// </summary>
		public uint Depth => Dimensions.Depth;
		#endregion // Fields

		/// <summary>
		/// Creates a texture of uninitialized texel data.
		/// </summary>
		/// <param name="width">The width of the texture, in texels.</param>
		/// <param name="height">The height of the texture, in texels.</param>
		/// <param name="depth">The depth of the texture, in texels.</param>
		public Texture3D(uint width, uint height, uint depth) :
			base(TextureType.Tex3D, (width, height, depth, 1))
		{ }

		/// <summary>
		/// Uploads texel data to the texture.
		/// </summary>
		/// <param name="data">The data to upload to the texture.</param>
		/// <param name="start">The starting coordinates to set data for.</param>
		/// <param name="size">The size of the region to set data for.</param>
		public void SetData(ReadOnlySpan<byte> data, Point3 start, Extent3 size)
		{
			if (start.X < 0 || start.Y < 0 || start.Z < 0)
				throw new ArgumentOutOfRangeException("SetData(): negative start coordinates.");
			if ((start.X + size.Width) > Width || (start.Y + size.Height) > Height || (start.Z + size.Depth) > Depth)
				throw new ArgumentOutOfRangeException("SetData(): (start + size) > texture size.");

			SetDataInternal(data, ((uint)start.X, (uint)start.Y, (uint)start.Z, size.Width, size.Height, size.Depth), 0);
		}

		/// <summary>
		/// Uploads texel data to the texture.
		/// </summary>
		/// <param name="data">The data to upload to the texture.</param>
		/// <param name="start">The starting coordinates to set data for.</param>
		/// <param name="size">The size of the region to set data for.</param>
		public void SetData<T>(ReadOnlySpan<T> data, Point3 start, Extent3 size)
			where T : struct =>
			SetData(MemoryMarshal.AsBytes(data), start, size);

		/// <summary>
		/// Uploads texel data to the texture asynchronously. The memory in <paramref name="data"/> must not be
		/// modified before the task returned by this function completes.
		/// </summary>
		/// <param name="data">The data to upload to the texture.</param>
		/// <param name="start">The starting coordinates to set data for.</param>
		/// <param name="size">The size of the region to set data for.</param>
		/// <returns>The task representing the data upload.</returns>
		public Task SetDataAsync(ReadOnlyMemory<byte> data, Point3 start, Extent3 size)
		{
			if (start.X < 0 || start.Y < 0 || start.Z < 0)
				throw new ArgumentOutOfRangeException("SetData(): negative start coordinates.");
			if ((start.X + size.Width) > Width || (start.Y + size.Height) > Height || (start.Z + size.Depth) > Depth)
				throw new ArgumentOutOfRangeException("SetData(): (start + size) > texture size.");

			return SetDataAsyncInternal(data, ((uint)start.X, (uint)start.Y, (uint)start.Z, size.Width, size.Height, size.Depth), 0);
		}

		/// <summary>
		/// Uploads texel data to the texture asynchronously. The memory in <paramref name="data"/> must not be
		/// modified before the task returned by this function completes.
		/// </summary>
		/// <param name="data">The data to upload to the texture.</param>
		/// <param name="start">The starting coordinates to set data for.</param>
		/// <param name="size">The size of the region to set data for.</param>
		/// <returns>The task representing the data upload.</returns>
		public Task SetDataAsync<T>(ReadOnlyMemory<T> data, Point3 start, Extent3 size)
			where T : struct
		{
			if (start.X < 0 || start.Y < 0 || start.Z < 0)
				throw new ArgumentOutOfRangeException("SetData(): negative start coordinates.");
			if ((start.X + size.Width) > Width || (start.Y + size.Height) > Height || (start.Z + size.Depth) > Depth)
				throw new ArgumentOutOfRangeException("SetData(): (start + size) > texture size.");

			return SetDataAsyncInternal(data, ((uint)start.X, (uint)start.Y, (uint)start.Z, size.Width, size.Height, size.Depth), 0);
		}
	}
}
