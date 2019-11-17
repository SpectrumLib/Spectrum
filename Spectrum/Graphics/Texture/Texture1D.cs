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
	/// A one-dimensional texture type, storing a strip of texels.
	/// </summary>
	public sealed class Texture1D : Texture
	{
		#region Fields
		/// <summary>
		/// The width of the texture.
		/// </summary>
		public uint Width => Dimensions.Width;
		#endregion // Fields

		/// <summary>
		/// Creates a texture of uninitialized texel data.
		/// </summary>
		/// <param name="width">The width of the texture, in texels.</param>
		public Texture1D(uint width) :
			base(TextureType.Tex1D, (width, 1, 1, 1))
		{ }

		/// <summary>
		/// Uploads texel data to the texture.
		/// </summary>
		/// <param name="data">The data to upload to the texture.</param>
		/// <param name="start">The starting texel to set data for.</param>
		/// <param name="size">The number of texels to set data for.</param>
		public void SetData(ReadOnlySpan<byte> data, uint start, uint size)
		{
			if ((start + size) > Width)
				throw new ArgumentOutOfRangeException("SetData(): (start + size) > texture width.");
			if (size == 0)
				return;

			SetDataInternal(data, (start, 0, 0, size, 1, 1), 0);
		}

		/// <summary>
		/// Uploads texel data to the texture.
		/// </summary>
		/// <param name="data">The data to upload to the texture.</param>
		/// <param name="start">The starting texel to set data for.</param>
		/// <param name="size">The number of texels to set data for.</param>
		public void SetData<T>(ReadOnlySpan<T> data, uint start, uint size)
			where T : struct =>
			SetData(MemoryMarshal.AsBytes(data), start, size);
	}
}
