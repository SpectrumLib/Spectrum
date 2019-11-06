/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.CompilerServices;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	/// <summary>
	/// The set of values that controls the pipeline rasterizer engine.
	/// </summary>
	public struct RasterizerState
	{
		#region Predefined States
		/// <summary>
		/// Default, counter-clockwise no-culling rasterizer state.
		/// </summary>
		public static readonly RasterizerState CullNone = new RasterizerState {
			DepthClampEnable = false,
			FillMode = FillMode.Solid,
			LineWidth = null,
			CullMode = CullMode.None,
			FrontFace = FrontFace.CounterClockwise
		};
		/// <summary>
		/// Counter-clockwise front face with culling of back faces.
		/// </summary>
		public static readonly RasterizerState CullBack = new RasterizerState {
			DepthClampEnable = false,
			FillMode = FillMode.Solid,
			LineWidth = null,
			CullMode = CullMode.Back,
			FrontFace = FrontFace.CounterClockwise
		};
		/// <summary>
		/// Wireframe mode with no culling. Note that this requires FillModeNonSolid to be enabled on the graphics device.
		/// </summary>
		public static readonly RasterizerState Wireframe = new RasterizerState {
			DepthClampEnable = false,
			FillMode = FillMode.Line,
			LineWidth = null,
			CullMode = CullMode.None,
			FrontFace = FrontFace.CounterClockwise
		};
		#endregion // Predefined States

		#region Fields
		/// <summary>
		/// Enable/Disable depth clamping. Requires explicitly enabling the feature in the graphics device.
		/// </summary>
		public bool DepthClampEnable;
		/// <summary>
		/// Polygon fill mode. Anything other than <see cref="FillMode.Solid"/> requires explicitly enabling the feature
		/// in the graphics device.
		/// </summary>
		public FillMode FillMode;
		/// <summary>
		/// Width of drawn lines. Any value other than 1.0 requires explicitly enabling the feature in the graphics
		/// device. Defaults to null, which is the default width of 1.
		/// </summary>
		public float? LineWidth;
		/// <summary>
		/// The polygon culling mode. Defaults to no culling.
		/// </summary>
		public CullMode CullMode;
		/// <summary>
		/// The winding considered a front face. Defaults to counter-clockwise winding.
		/// </summary>
		public FrontFace FrontFace;
		#endregion // Fields

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Vk.PipelineRasterizationStateCreateInfo ToVulkanType() => new Vk.PipelineRasterizationStateCreateInfo {
			DepthClampEnable = DepthClampEnable,
			PolygonMode = (Vk.PolygonMode)FillMode,
			CullMode = (Vk.CullModeFlags)CullMode,
			FrontFace = (Vk.FrontFace)FrontFace,
			LineWidth = LineWidth.HasValue ? LineWidth.Value : 1.0f,
			DepthBiasEnable = false,
			RasterizerDiscardEnable = false
		};
	}

	/// <summary>
	/// The fill mode for the polygons. Note that <see cref="GraphicsDevice.Features"/> must have
	/// FillModeNonSolid as true to use anything except for solid fill mode.
	/// </summary>
	public enum FillMode
	{
		/// <summary>
		/// Polygon primitives are completely filled.
		/// </summary>
		Solid = Vk.PolygonMode.Fill,
		/// <summary>
		/// Only vertices, and lines between vertices are rendered. Also known as "wireframe" mode.
		/// </summary>
		Line = Vk.PolygonMode.Line,
		/// <summary>
		/// Only vertices are rendered as points, and are not connected.
		/// </summary>
		Point = Vk.PolygonMode.Point
	}

	/// <summary>
	/// The direction of culled polygons.
	/// </summary>
	public enum CullMode
	{
		/// <summary>
		/// Do not cull any polygons.
		/// </summary>
		None = Vk.CullModeFlags.None,
		/// <summary>
		/// Cull polygons facing towards the camera.
		/// </summary>
		Front = Vk.CullModeFlags.Front,
		/// <summary>
		/// Cull polygons facing away from the camera.
		/// </summary>
		Back = Vk.CullModeFlags.Back,
		/// <summary>
		/// Cull all polygons.
		/// </summary>
		Both = Vk.CullModeFlags.FrontAndBack
	}

	/// <summary>
	/// The winding direction for front-facing polygons.
	/// </summary>
	public enum FrontFace
	{
		/// <summary>
		/// Counter-clockwise winding is considered the front face.
		/// </summary>
		CounterClockwise = Vk.FrontFace.CounterClockwise,
		/// <summary>
		/// Clockwise winding is considered the front face.
		/// </summary>
		Clockwise = Vk.FrontFace.Clockwise
	}
}
