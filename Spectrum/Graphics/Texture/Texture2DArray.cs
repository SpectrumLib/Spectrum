/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
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
	/// An array of two-dimensional textures, each storing a plane of texels.
	/// </summary>
	public sealed class Texture2DArray : Texture
	{
		#region Fields
		/// <summary>
		/// The width of the textures.
		/// </summary>
		public uint Width => Dimensions.Width;
		
		/// <summary>
		/// The height of the textures.
		/// </summary>
		public uint Height => Dimensions.Height;

		/// <summary>
		/// The number of textures in the texture array.
		/// </summary>
		public uint Layers => Dimensions.Layers;
		#endregion // Fields

		/// <summary>
		/// Creates an array of textures of uninitialized texel data.
		/// </summary>
		/// <param name="width">The width of the textures, in texels.</param>
		/// <param name="height">The height of the textures, in texels.</param>
		/// <param name="layers">The number of textures in the texture array.</param>
		public Texture2DArray(uint width, uint height, uint layers) :
			base(TextureType.Tex2DArray, (width, height, 1, layers))
		{ }

		/// <summary>
		/// Uploads texel data to the texture.
		/// </summary>
		/// <param name="data">The data to upload to the texture.</param>
		/// <param name="start">The starting coordinates to set data for.</param>
		/// <param name="size">The size of the region to set data for.</param>
		/// <param name="layer">The index of the texture in the array to set data for.</param>
		public void SetData(ReadOnlySpan<byte> data, Point start, Extent size, uint layer)
		{
			if (start.X < 0 || start.Y < 0)
				throw new ArgumentOutOfRangeException("SetData(): negative start coordinates.");
			if ((start.X + size.Width) > Width || (start.Y + size.Height) > Height)
				throw new ArgumentOutOfRangeException("SetData(): (start + size) > texture size.");
			if (layer >= Layers)
				throw new ArgumentOutOfRangeException("SetData(): layer > texture array count.");
			if (size.Width == 0 || size.Height == 0)
				return;

			SetDataInternal(data, ((uint)start.X, (uint)start.Y, 0, size.Width, size.Height, 1), layer);
		}

		/// <summary>
		/// Uploads texel data to the texture.
		/// </summary>
		/// <param name="data">The data to upload to the texture.</param>
		/// <param name="start">The starting coordinates to set data for.</param>
		/// <param name="size">The size of the region to set data for.</param>
		/// <param name="layer">The index of the texture in the array to set data for.</param>
		public void SetData<T>(ReadOnlySpan<T> data, Point start, Extent size, uint layer)
			where T : struct =>
			SetData(MemoryMarshal.AsBytes(data), start, size, layer);

		/// <summary>
		/// Uploads texel data to the texture asynchronously. The memory in <paramref name="data"/> must not be
		/// modified before the task returned by this function completes.
		/// </summary>
		/// <param name="data">The data to upload to the texture.</param>
		/// <param name="start">The starting coordinates to set data for.</param>
		/// <param name="size">The size of the region to set data for.</param>
		/// <param name="layer">The index of the texture in the array to set data for.</param>
		/// <returns>The task representing the data upload.</returns>
		public Task SetDataAsync(ReadOnlyMemory<byte> data, Point start, Extent size, uint layer)
		{
			if (start.X < 0 || start.Y < 0)
				throw new ArgumentOutOfRangeException("SetData(): negative start coordinates.");
			if ((start.X + size.Width) > Width || (start.Y + size.Height) > Height)
				throw new ArgumentOutOfRangeException("SetData(): (start + size) > texture size.");

			return SetDataAsyncInternal(data, ((uint)start.X, (uint)start.Y, 0, size.Width, size.Height, 1), layer);
		}

		/// <summary>
		/// Uploads texel data to the texture asynchronously. The memory in <paramref name="data"/> must not be
		/// modified before the task returned by this function completes.
		/// </summary>
		/// <param name="data">The data to upload to the texture.</param>
		/// <param name="start">The starting coordinates to set data for.</param>
		/// <param name="size">The size of the region to set data for.</param>
		/// <param name="layer">The index of the texture in the array to set data for.</param>
		/// <returns>The task representing the data upload.</returns>
		public Task SetDataAsync<T>(ReadOnlyMemory<T> data, Point start, Extent size, uint layer)
			where T : struct
		{
			if (start.X < 0 || start.Y < 0)
				throw new ArgumentOutOfRangeException("SetData(): negative start coordinates.");
			if ((start.X + size.Width) > Width || (start.Y + size.Height) > Height)
				throw new ArgumentOutOfRangeException("SetData(): (start + size) > texture size.");

			return SetDataAsyncInternal(data, ((uint)start.X, (uint)start.Y, 0, size.Width, size.Height, 1), layer);
		}
	}
}
