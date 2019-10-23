/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	/// <summary>
	/// The different types of textures available (described dimensionality of textures).
	/// </summary>
	public enum TextureType
	{
		/// <summary>
		/// The texture has data in one dimension.
		/// </summary>
		Texture1D = Vk.ImageType.Image1d,
		/// <summary>
		/// The texture has data in two dimensions.
		/// </summary>
		Texture2D = Vk.ImageType.Image2d,
		/// <summary>
		/// The texture has data in three dimensions.
		/// </summary>
		Texture3D = Vk.ImageType.Image3d
	}
}
