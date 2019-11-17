/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.InteropServices;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	/// <summary>
	/// An array of one-dimensional textures, each storing a strip of texels.
	/// </summary>
	public sealed class Texture1DArray : Texture
	{
		#region Fields
		/// <summary>
		/// The width of the textures.
		/// </summary>
		public uint Width => Dimensions.Width;

		/// <summary>
		/// The number of textures in the texture array.
		/// </summary>
		public uint Layers => Dimensions.Layers;
		#endregion // Fields

		/// <summary>
		/// Creates an array of textures of uninitialized texel data.
		/// </summary>
		/// <param name="width">The width of the textures, in texels.</param>
		/// <param name="layers">The number of textures in the texture array.</param>
		public Texture1DArray(uint width, uint layers) :
			base(TextureType.Tex1DArray, (width, 1, 1, layers))
		{ }

		/// <summary>
		/// Uploads texel data to the texture.
		/// </summary>
		/// <param name="data">The data to upload to the texture.</param>
		/// <param name="start">The starting texel to set data for.</param>
		/// <param name="size">The number of texels to set data for.</param>
		/// <param name="layer">The index of the texture in the array to set data for.</param>
		public void SetData(ReadOnlySpan<byte> data, uint start, uint size, uint layer)
		{
			if ((start + size) > Width)
				throw new ArgumentOutOfRangeException("SetData(): (start + size) > texture width.");
			if (layer >= Layers)
				throw new ArgumentOutOfRangeException("SetData(): layer > texture array count.");
			if (size == 0)
				return;

			SetDataInternal(data, (start, 0, 0, size, 1, 1), layer);
		}

		/// <summary>
		/// Uploads texel data to the texture.
		/// </summary>
		/// <param name="data">The data to upload to the texture.</param>
		/// <param name="start">The starting texel to set data for.</param>
		/// <param name="size">The number of texels to set data for.</param>
		/// <param name="layer">The index of the texture in the array to set data for.</param>
		public void SetData<T>(ReadOnlySpan<T> data, uint start, uint size, uint layer)
			where T : struct =>
			SetData(MemoryMarshal.AsBytes(data), start, size, layer);
	}
}
