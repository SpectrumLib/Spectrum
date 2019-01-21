using System;
using System.Runtime.CompilerServices;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Controls the color blending functionality of a pipeline.
	/// </summary>
	/// <remarks>It is a planned feature to enable logic blending operations, but it is not supported now.</remarks>
	public struct ColorBlendState
	{
		#region Fields
		/// <summary>
		/// If color blending is enabled.
		/// </summary>
		public bool BlendEnabled;
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
		internal Vk.PipelineColorBlendStateCreateInfo ToCreateInfo()
		{
			var cbas = new Vk.PipelineColorBlendAttachmentState(
				blendEnable: BlendEnabled,
				srcColorBlendFactor: (Vk.BlendFactor)SrcColorFactor,
				dstColorBlendFactor: (Vk.BlendFactor)DstColorFactor,
				colorBlendOp: (Vk.BlendOp)ColorOp,
				srcAlphaBlendFactor: (Vk.BlendFactor)SrcAlphaFactor,
				dstAlphaBlendFactor: (Vk.BlendFactor)DstAlphaFactor,
				alphaBlendOp: (Vk.BlendOp)AlphaOp,
				colorWriteMask: WriteMask.HasValue ? (Vk.ColorComponents)WriteMask.Value : Vk.ColorComponents.All
			);
			return new Vk.PipelineColorBlendStateCreateInfo(
				new[] { cbas },
				logicOpEnable: false,
				blendConstants: new Vk.ColorF4(BlendConstants.RFloat, BlendConstants.GFloat, BlendConstants.BFloat, BlendConstants.AFloat)
			);
		}
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
		R = Vk.ColorComponents.R,
		/// <summary>
		/// Green channel.
		/// </summary>
		G = Vk.ColorComponents.G,
		/// <summary>
		/// Blue channel.
		/// </summary>
		B = Vk.ColorComponents.B,
		/// <summary>
		/// Alpha channel.
		/// </summary>
		A = Vk.ColorComponents.A,
		/// <summary>
		/// All channels.
		/// </summary>
		All = Vk.ColorComponents.All
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
		SrcColor = Vk.BlendFactor.SrcColor,
		/// <summary>
		/// One minus the source color channels `(1,1,1) - src.rgb`.
		/// </summary>
		OneMinusSrcColor = Vk.BlendFactor.OneMinusSrcColor,
		/// <summary>
		/// The destination color channels `dst.rgb`.
		/// </summary>
		DstColor = Vk.BlendFactor.DstColor,
		/// <summary>
		/// One minus the destination color channels `(1,1,1) - dst.rgb`.
		/// </summary>
		OneMinusDstColor = Vk.BlendFactor.OneMinusDstColor,
		/// <summary>
		/// The source alpha channel `src.a`.
		/// </summary>
		SrcAlpha = Vk.BlendFactor.SrcAlpha,
		/// <summary>
		/// One minus the source alpha channel `1 - src.a`.
		/// </summary>
		OneMinusSrcAlpha = Vk.BlendFactor.OneMinusSrcAlpha,
		/// <summary>
		/// The destination alpha channel `dst.a`.
		/// </summary>
		DstAlpha = Vk.BlendFactor.DstAlpha,
		/// <summary>
		/// One minus the destination alpha channel `1 - dst.a`.
		/// </summary>
		OneMinusDstAlpha = Vk.BlendFactor.OneMinusDstAlpha,
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
		SrcAlphaSaturate = Vk.BlendFactor.SrcAlphaSaturate
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
