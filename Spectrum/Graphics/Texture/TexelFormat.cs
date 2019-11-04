/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Contains the different formats for texture texels.
	/// </summary>
	public enum TexelFormat
	{
		// ================================== STANDARD COLOR INTEGER FORMATS ==========================================
		/// <summary>
		/// Each texel is an unsigned 8-bit integer representing a value between 0 and 1. Channels: R.
		/// </summary>
		UNorm = Vk.Format.R8UNorm,
		/// <summary>
		/// Each texel is two unsigned 8-bit integers representing values between 0 and 1. Channels: RG.
		/// </summary>
		UNorm2 = Vk.Format.R8G8UNorm,
		/// <summary>
		/// Each texel is four unsigned 8-bit integers representing values between 0 and 1. Channels: RGBA. This is the
		/// standard format used for color textures.
		/// </summary>
		UNorm4 = Vk.Format.R8G8B8A8UNorm,
		/// <summary>
		/// Each texel is a single unsigned 8-bit integer. Channels: R.
		/// </summary>
		UByte = Vk.Format.R8UInt,
		/// <summary>
		/// Each texel is two unsigned 8-bit integers. Channels: RG.
		/// </summary>
		UByte2 = Vk.Format.R8G8UInt,
		/// <summary>
		/// Each texel is four unsigned 8-bit integers. Channels: RGBA.
		/// </summary>
		UByte4 = Vk.Format.R8G8B8A8UInt,
		/// <summary>
		/// Each texel is a single signed 8-bit integer. Channels: R.
		/// </summary>
		Byte = Vk.Format.R8SInt,
		/// <summary>
		/// Each texel is two signed 8-bit integers. Channels: RG.
		/// </summary>
		Byte2 = Vk.Format.R8G8SInt,
		/// <summary>
		/// Each texel is four signed 8-bit integers. Channels: RGBA.
		/// </summary>
		Byte4 = Vk.Format.R8G8B8A8SInt,
		/// <summary>
		/// Each texel is an unsigned 16-bit integer. Channels: R.
		/// </summary>
		UShort = Vk.Format.R16UInt,
		/// <summary>
		/// Each texel is two unsigned 16-bit integers. Channels: RG.
		/// </summary>
		UShort2 = Vk.Format.R16G16UInt,
		/// <summary>
		/// Each texel is four unsigned 16-bit integers. Channels: RGBA.
		/// </summary>
		UShort4 = Vk.Format.R16G16B16A16UInt,
		/// <summary>
		/// Each texel is a signed 16-bit integer. Channels: R.
		/// </summary>
		Short = Vk.Format.R16SInt,
		/// <summary>
		/// Each texel is two signed 16-bit integers. Channels: RG.
		/// </summary>
		Short2 = Vk.Format.R16G16SInt,
		/// <summary>
		/// Each texel is four signed 16-bit integers. Channels: RGBA.
		/// </summary>
		Short4 = Vk.Format.R16G16B16A16SInt,
		/// <summary>
		/// Each texel is an unsigned 32-bit integer. Channels: R.
		/// </summary>
		UInt = Vk.Format.R32UInt,
		/// <summary>
		/// Each texel is two unsigned 32-bit integers. Channels: RG.
		/// </summary>
		UInt2 = Vk.Format.R32G32UInt,
		/// <summary>
		/// Each texel is four unsigned 32-bit integers. Channels: RGBA.
		/// </summary>
		UInt4 = Vk.Format.R32G32B32A32UInt,
		/// <summary>
		/// Each texel is a signed 32-bit integer. Channels: R.
		/// </summary>
		Int = Vk.Format.R32SInt,
		/// <summary>
		/// Each texel is two signed 32-bit integers. Channels: RG.
		/// </summary>
		Int2 = Vk.Format.R32G32SInt,
		/// <summary>
		/// Each texel is four signed 32-bit integers. Channels: RGBA.
		/// </summary>
		Int4 = Vk.Format.R32G32B32A32SInt,
		/// <summary>
		/// The format used by <see cref="Texture"/> types, same as <see cref="UNorm4"/>. 8-bit unsigned normalized
		/// integers. Channels: RGBA.
		/// </summary>
		Color = Vk.Format.R8G8B8A8UNorm,

		// =============================== STANDARD COLOR FLOATING POINT FORMATS ======================================
		/// <summary>
		/// Each texel is a single-precision 32-bit floating point number. Channels: R.
		/// </summary>
		Float = Vk.Format.R32SFloat,
		/// <summary>
		/// Each texel is two single-precision 32-bit floating point numbers. Channels: RG.
		/// </summary>
		Float2 = Vk.Format.R32G32SFloat,
		/// <summary>
		/// Each texel is four single-precision 32-bit floating point numbers. Channels: RGBA.
		/// </summary>
		Float4 = Vk.Format.R32G32B32A32SFloat,

		// ====================================== DEPTH/STENCIL FORMATS ===============================================
		/// <summary>
		/// Each texel is a single 16-bit unsigned integer representing a depth value between 0 and 1.
		/// </summary>
		Depth16 = Vk.Format.D16UNorm,
		/// <summary>
		/// Each texel is a single-precision 32-bit floating point number representing a depth value between 0 and 1.
		/// </summary>
		Depth32 = Vk.Format.D32SFloat,
		/// <summary>
		/// Each texel is a packed 24-bit unsigned integer representing a depth value between 0 and 1, and an 8-bit
		/// unsigned integer representing stencil data.
		/// </summary>
		Depth24Stencil8 = Vk.Format.D24UNormS8UInt
	}

	/// <summary>
	/// Contains utility functionality for working with <see cref="TexelFormat"/> values.
	/// </summary>
	public static class TexelFormatExtensions
	{
		private static readonly Dictionary<TexelFormat, (uint s, uint c)> FORMAT_DATA = new Dictionary<TexelFormat, (uint, uint)> {
			{ TexelFormat.UNorm, (1, 1) },   { TexelFormat.UNorm2, (2, 2) },  { TexelFormat.UNorm4, (4, 4) },
			{ TexelFormat.UByte, (1, 1) },   { TexelFormat.UByte2, (2, 2) },  { TexelFormat.UByte4, (4, 4) },
			{ TexelFormat.Byte, (1, 1) },    { TexelFormat.Byte2, (2, 2) },   { TexelFormat.Byte4, (4, 4) },
			{ TexelFormat.UShort, (2, 1) },  { TexelFormat.UShort2, (4, 2) }, { TexelFormat.UShort4, (8, 4) },
			{ TexelFormat.Short, (2, 1) },   { TexelFormat.Short2, (4, 2) },  { TexelFormat.Short4, (8, 4) },
			{ TexelFormat.UInt, (4, 1) },    { TexelFormat.UInt2, (8, 2) },   { TexelFormat.UInt4, (16, 4) },
			{ TexelFormat.Int, (4, 1) },     { TexelFormat.Int2, (8, 2) },    { TexelFormat.Int4, (16, 4) },
			{ TexelFormat.Float, (4, 1) },   { TexelFormat.Float2, (8, 2) },  { TexelFormat.Float4, (16, 4) },
			{ TexelFormat.Depth16, (2, 1) }, { TexelFormat.Depth32, (4, 1) }, { TexelFormat.Depth24Stencil8, (4, 2) }
		};

		/// <summary>
		/// Gets if the format is a valid format for textures used as input attachments.
		/// </summary>
		/// <param name="fmt">The format to check.</param>
		/// <returns>If the format is valid for shader input attachments.</returns>
		public static bool IsValidInputFormat(this TexelFormat fmt)
		{
			string fStr = fmt.ToString();
			if (fStr[fStr.Length - 1] == '3')
				return false; // No 3-component texel format is supported
			return true;
		}

		/// <summary>
		/// Gets the size of a single texel of the given format, in bytes.
		/// </summary>
		/// <param name="fmt">The format to get the size for.</param>
		/// <returns>The format texel size.</returns>
		public static uint GetSize(this TexelFormat fmt) => FORMAT_DATA[fmt].s;

		/// <summary>
		/// Gets the number of components in a texel of the format.
		/// </summary>
		/// <param name="fmt">The format to get the component count for.</param>
		/// <returns>The format texel component count.</returns>
		public static uint GetComponentCount(this TexelFormat fmt) => FORMAT_DATA[fmt].c;

		/// <summary>
		/// Gets if the format is a depth or depth/stencil format.
		/// </summary>
		/// <param name="fmt">The format to check.</param>
		/// <returns>If the format holds depth or depth/stencil data.</returns>
		public static bool IsDepthFormat(this TexelFormat fmt) =>
			(fmt == TexelFormat.Depth16) || (fmt == TexelFormat.Depth24Stencil8) || (fmt == TexelFormat.Depth32);

		/// <summary>
		/// Gets if the format is a a color format (anything that isn't a depth or depth/stencil format).
		/// </summary>
		/// <param name="fmt">The format to check.</param>
		/// <returns>If the format holds color data.</returns>
		public static bool IsColorFormat(this TexelFormat fmt) => !IsDepthFormat(fmt);

		/// <summary>
		/// Gets if the format has a stencil component.
		/// </summary>
		/// <param name="fmt">The format to check.</param>
		/// <returns>If the texel format contains a stencil component.</returns>
		public static bool HasStencilComponent(this TexelFormat fmt) => fmt == TexelFormat.Depth24Stencil8;
	}
}
