using System;
using System.Runtime.CompilerServices;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Contains settings that control the execution of the rasterizer stage.
	/// </summary>
	public struct RasterizerState
	{
		#region Constants
		/// <summary>
		/// Default, counter-clockwise no-culling rasterizer state.
		/// </summary>
		public static readonly RasterizerState CullNone = new RasterizerState {
			DepthClampEnable = false, FillMode = FillMode.Solid, LineWidth = null, CullMode = CullMode.None, FrontFace = CullFace.CounterClockwise
		};
		/// <summary>
		/// Counter-clockwise front face with culling of back faces.
		/// </summary>
		public static readonly RasterizerState CullBack = new RasterizerState {
			DepthClampEnable = false, FillMode = FillMode.Solid, LineWidth = null, CullMode = CullMode.Back, FrontFace = CullFace.CounterClockwise
		};
		/// <summary>
		/// Wireframe mode with no culling. Note that this requires FillModeNonSolid to be enabled on the graphics device.
		/// </summary>
		public static readonly RasterizerState Wireframe = new RasterizerState {
			DepthClampEnable = false, FillMode = FillMode.Line, LineWidth = null, CullMode = CullMode.None, FrontFace = CullFace.CounterClockwise
		};
		#endregion // Constants

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
		public CullFace FrontFace;
		#endregion // Fields

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Vk.PipelineRasterizationStateCreateInfo ToCreateInfo()
		{
			return new Vk.PipelineRasterizationStateCreateInfo(
				depthClampEnable: DepthClampEnable,
				polygonMode: (Vk.PolygonMode)FillMode,
				cullMode: (Vk.CullModes)CullMode,
				frontFace: (Vk.FrontFace)FrontFace,
				lineWidth: LineWidth.HasValue ? LineWidth.Value : 1.0f
			);
		}
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
		/// Only vertices, and lines between vertices are rendered.
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
		None = Vk.CullModes.None,
		/// <summary>
		/// Cull polygons facing towards the camera.
		/// </summary>
		Front = Vk.CullModes.Front,
		/// <summary>
		/// Cull polygons facing away from the camera.
		/// </summary>
		Back = Vk.CullModes.Back,
		/// <summary>
		/// Cull all polygons.
		/// </summary>
		Both = Vk.CullModes.FrontAndBack
	}

	/// <summary>
	/// The winding direction for front-facing polygons.
	/// </summary>
	public enum CullFace
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
