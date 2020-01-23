/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	/// <summary>
	/// The set of values that controls pipeline color blending.
	/// </summary>
	public struct ColorBlendState
	{
		#region Predefined States
		/// <summary>
		/// No blending is performed - alpha is ignored and the new color fully replaces the existing color.
		/// </summary>
		public static readonly ColorBlendState None = new ColorBlendState {
			Enabled = false,
			SrcColorFactor = BlendFactor.One, DstColorFactor = BlendFactor.Zero, ColorOp = BlendOp.Add,
			SrcAlphaFactor = BlendFactor.One, DstAlphaFactor = BlendFactor.Zero, AlphaOp = BlendOp.Add,
			WriteMask = null, BlendConstants = Color.TransparentBlack
		};
		/// <summary>
		/// The new color is added directly on top of the old color, taking into account the source alpha.
		/// </summary>
		public static readonly ColorBlendState Additive = new ColorBlendState {
			Enabled = true,
			SrcColorFactor = BlendFactor.SrcAlpha, DstColorFactor = BlendFactor.One, ColorOp = BlendOp.Add,
			SrcAlphaFactor = BlendFactor.SrcAlpha, DstAlphaFactor = BlendFactor.One, AlphaOp = BlendOp.Add,
			WriteMask = null, BlendConstants = Color.TransparentBlack
		};
		/// <summary>
		/// Fully blends the source and destination colors using the alpha values for both. This is the traditional
		/// "nothing special" transparency blending.
		/// </summary>
		public static readonly ColorBlendState Alpha = new ColorBlendState {
			Enabled = true,
			SrcColorFactor = BlendFactor.SrcAlpha, DstColorFactor = BlendFactor.OneMinusSrcAlpha, ColorOp = BlendOp.Add,
			SrcAlphaFactor = BlendFactor.One, DstAlphaFactor = BlendFactor.Zero, AlphaOp = BlendOp.Add,
			WriteMask = null, BlendConstants = Color.TransparentBlack
		};
		#endregion // Predefined States

		#region Fields
		/// <summary>
		/// If color blending is enabled.
		/// </summary>
		public bool Enabled;
		/// <summary>
		/// The blending factor to apply to the source color channels.
		/// </summary>
		public BlendFactor SrcColorFactor;
		/// <summary>
		/// The blending factor to apply to the destination color channels.
		/// </summary>
		public BlendFactor DstColorFactor;
		/// <summary>
		/// The blending operation to apply to the color channels.
		/// </summary>
		public BlendOp ColorOp;
		/// <summary>
		/// The blending factor to apply to the source alpha channel.
		/// </summary>
		public BlendFactor SrcAlphaFactor;
		/// <summary>
		/// The blending factor to apply to the destination alpha channel.
		/// </summary>
		public BlendFactor DstAlphaFactor;
		/// <summary>
		/// The blending operation to apply to the alpha channel.
		/// </summary>
		public BlendOp AlphaOp;
		/// <summary>
		/// A mask of the color channels that are written to by the pipeline. Null defaults to all channels.
		/// </summary>
		public ColorComponents? WriteMask;
		/// <summary>
		/// The values to use as the blending constants if the selected blending operations need them.
		/// </summary>
		public Color BlendConstants;
		#endregion // Fields

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Vk.PipelineColorBlendAttachmentState ToVulkanType() => new Vk.PipelineColorBlendAttachmentState {
			BlendEnable = Enabled,
			SourceColorBlendFactor = (Vk.BlendFactor)SrcColorFactor,
			DestinationColorBlendFactor = (Vk.BlendFactor)DstColorFactor,
			ColorBlendOp = (Vk.BlendOp)ColorOp,
			SourceAlphaBlendFactor = (Vk.BlendFactor)SrcAlphaFactor,
			DestinationAlphaBlendFactor = (Vk.BlendFactor)DstAlphaFactor,
			AlphaBlendOp = (Vk.BlendOp)AlphaOp,
			ColorWriteMask = (Vk.ColorComponentFlags)(WriteMask.HasValue ? WriteMask.Value : ColorComponents.All)
		};
	}

	/// <summary>
	/// Contains a bit mask for the four color channels.
	/// </summary>
	[Flags]
	public enum ColorComponents
	{
		/// <summary>
		/// Red channel.
		/// </summary>
		R = Vk.ColorComponentFlags.R,
		/// <summary>
		/// Green channel.
		/// </summary>
		G = Vk.ColorComponentFlags.G,
		/// <summary>
		/// Blue channel.
		/// </summary>
		B = Vk.ColorComponentFlags.B,
		/// <summary>
		/// Alpha channel.
		/// </summary>
		A = Vk.ColorComponentFlags.A,
		/// <summary>
		/// All channels.
		/// </summary>
		All = (R | G | B | A)
	}

	/// <summary>
	/// List of blending equation factors that can be applied to color and alpha channels.
	/// </summary>
	public enum BlendFactor
	{
		/// <summary>
		/// A constant 0.
		/// </summary>
		Zero = Vk.BlendFactor.Zero,
		/// <summary>
		/// A constant 1.
		/// </summary>
		One = Vk.BlendFactor.One,
		/// <summary>
		/// The source color channels `src.rgb`.
		/// </summary>
		SrcColor = Vk.BlendFactor.SourceColor,
		/// <summary>
		/// One minus the source color channels `(1,1,1) - src.rgb`.
		/// </summary>
		OneMinusSrcColor = Vk.BlendFactor.OneMinusSourceColor,
		/// <summary>
		/// The destination color channels `dst.rgb`.
		/// </summary>
		DstColor = Vk.BlendFactor.DestinationColor,
		/// <summary>
		/// One minus the destination color channels `(1,1,1) - dst.rgb`.
		/// </summary>
		OneMinusDstColor = Vk.BlendFactor.OneMinusDestinationColor,
		/// <summary>
		/// The source alpha channel `src.a`.
		/// </summary>
		SrcAlpha = Vk.BlendFactor.SourceAlpha,
		/// <summary>
		/// One minus the source alpha channel `1 - src.a`.
		/// </summary>
		OneMinusSrcAlpha = Vk.BlendFactor.OneMinusSourceAlpha,
		/// <summary>
		/// The destination alpha channel `dst.a`.
		/// </summary>
		DstAlpha = Vk.BlendFactor.DestinationAlpha,
		/// <summary>
		/// One minus the destination alpha channel `1 - dst.a`.
		/// </summary>
		OneMinusDstAlpha = Vk.BlendFactor.OneMinusDestinationAlpha,
		/// <summary>
		/// A constant color value (three channels).
		/// </summary>
		ConstColor = Vk.BlendFactor.ConstantColor,
		/// <summary>
		/// One minus a constant color value (three channels).
		/// </summary>
		OneMinusConstColor = Vk.BlendFactor.OneMinusConstantColor,
		/// <summary>
		/// A constant alpha value (one channel).
		/// </summary>
		ConstAlpha = Vk.BlendFactor.ConstantAlpha,
		/// <summary>
		/// One minus a constant alpha value (one channel).
		/// </summary>
		OneMinusConstAlpha = Vk.BlendFactor.OneMinusConstantAlpha,
		/// <summary>
		/// Minimum of source and one minus destination alphas to the color channels: `(f, f, f, 1) where f = min(src.a, 1 - dst.a)`.
		/// </summary>
		SrcAlphaSaturate = Vk.BlendFactor.SourceAlphaSaturate
	}

	/// <summary>
	/// List of operations to apply to color and alpha blending.
	/// </summary>
	/// <remarks>Implementation of the *many* extension ops is planned.</remarks>
	public enum BlendOp
	{
		/// <summary>
		/// The source channels and destination channels are added.
		/// </summary>
		Add = Vk.BlendOp.Add,
		/// <summary>
		/// The destination channels are subtracted from the source channels.
		/// </summary>
		Subtract = Vk.BlendOp.Subtract,
		/// <summary>
		/// The source channels are subtracted from the destination channels.
		/// </summary>
		ReverseSubtract = Vk.BlendOp.ReverseSubtract,
		/// <summary>
		/// The minimum of the channels are selected.
		/// </summary>
		Min = Vk.BlendOp.Min,
		/// <summary>
		/// The maximum of the channels are selected.
		/// </summary>
		Max = Vk.BlendOp.Max
	}
}
