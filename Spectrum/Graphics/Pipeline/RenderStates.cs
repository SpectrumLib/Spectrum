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
	/// Collection of values that fully describe how a <see cref="Pipeline"/> processes rendering commands.
	/// </summary>
	public sealed class RenderStates
	{
		#region Fields
		// Cached Vulkan pipeline objects
		internal Vk.PipelineColorBlendAttachmentState VkColorBlendState;
		internal (float R, float G, float B, float A) VkColorBlendConstants = (0, 0, 0, 0);
		internal Vk.PipelineDepthStencilStateCreateInfo VkDepthStencilState;
		internal Vk.PipelineInputAssemblyStateCreateInfo VkPrimitiveInput;
		internal Vk.PipelineRasterizationStateCreateInfo VkRasterizerState;
		internal Vk.PipelineVertexInputStateCreateInfo VkVertexDescription;

		// Cached validation objects
		private bool? _isValid = null;
		private string _validError = null;

		#region States
		/// <summary>
		/// The color blending to use in the pipeline.
		/// </summary>
		public ColorBlendState? ColorBlendState
		{
			get => _colorBlendState;
			set => (_colorBlendState, VkColorBlendState, _isValid) = (value, value?.ToVulkanType() ?? default, null);
		}
		private ColorBlendState? _colorBlendState = null;

		/// <summary>
		/// The color blending constant values, if any of the color attachments use constant blending operations.
		/// </summary>
		public Color ColorBlendConstants
		{
			get => VkColorBlendConstants;
			set => (VkColorBlendConstants, _isValid) = ((value.RFloat, value.GFloat, value.BFloat, value.AFloat), null);
		}

		/// <summary>
		/// The depth/stencil operations to use in the pipeline.
		/// </summary>
		public DepthStencilState? DepthStencilState
		{
			get => _depthStencilState;
			set => (_depthStencilState, VkDepthStencilState, _isValid) = (value, value?.ToVulkanType() ?? default, null);
		}
		private DepthStencilState? _depthStencilState = null;

		/// <summary>
		/// The primitive topology to use in the pipeline.
		/// </summary>
		public PrimitiveInput? PrimitiveInput
		{
			get => _primitiveInput;
			set => (_primitiveInput, VkPrimitiveInput, _isValid) = (value, value?.ToVulkanType() ?? default, null);
		}
		private PrimitiveInput? _primitiveInput = null;

		/// <summary>
		/// The rasterizer state to use in the pipeline.
		/// </summary>
		public RasterizerState? RasterizerState
		{
			get => _rasterizerState;
			set => (_rasterizerState, VkRasterizerState, _isValid) = (value, value?.ToVulkanType() ?? default, null);
		}
		private RasterizerState? _rasterizerState = null;

		/// <summary>
		/// The vertex layout to use in the pipeline.
		/// </summary>
		public VertexDescription? VertexDescription
		{
			get => _vertexDescription;
			set => (_vertexDescription, VkVertexDescription, _isValid) = (value, value?.ToVulkanType() ?? default, null);
		}
		private VertexDescription? _vertexDescription = null;

		/// <summary>
		/// The viewport to use for the pipeline, or <c>null</c> to use the default framebuffer size.
		/// </summary>
		public Viewport? Viewport = null;

		/// <summary>
		/// The scissor to use foe the pipeline, or <c>null</c> to use the default framebuffer size.
		/// </summary>
		public Scissor? Scissor = null;
		#endregion // States

		/// <summary>
		/// Gets if the current <see cref="DepthStencilState"/> performs depth buffer operations.
		/// </summary>
		public bool UsesDepthBuffer => _depthStencilState?.HasDepthOperations ?? false;

		/// <summary>
		/// Gets if the current <see cref="DepthStencilState"/> performs stencil buffer operations.
		/// </summary>
		public bool UsesStencilBuffer => _depthStencilState?.HasStencilOperations ?? false;

		/// <summary>
		/// Gets if the pipeline description is complete, with all pipeline values fully defined.
		/// </summary>
		public bool IsComplete =>
			ColorBlendState.HasValue && DepthStencilState.HasValue && PrimitiveInput.HasValue &&
			RasterizerState.HasValue && VertexDescription.HasValue;
		#endregion // Fields

		#region Ctor
		/// <summary>
		/// Creates a new set of incomplete render states.
		/// </summary>
		public RenderStates() { }

		/// <summary>
		/// Creates a new set of render states from the passed states.
		/// </summary>
		/// <param name="colorBlend">The color blending state.</param>
		/// <param name="depthStencil">The depth stencil operations state.</param>
		/// <param name="primitives">The primitive topology.</param>
		/// <param name="rasterizer">The rasterizer state.</param>
		/// <param name="vertexDescription">The layout of the vertex data.</param>
		/// <param name="viewport">The optional viewport, or <c>null</c> for the framebuffer default.</param>
		/// <param name="scissor">The optional scissor, or <c>null</c> for the framebuffer default.</param>
		/// <param name="blendConstants">The color blending constants, or <c>null</c> for (0, 0, 0, 0).</param>
		public RenderStates(
			ColorBlendState? colorBlend,
			DepthStencilState? depthStencil,
			PrimitiveInput? primitives,
			RasterizerState? rasterizer,
			VertexDescription? vertexDescription,
			Viewport? viewport = null,
			Scissor? scissor = null,
			Color? blendConstants = null
		)
		{
			ColorBlendState = colorBlend;
			DepthStencilState = depthStencil;
			PrimitiveInput = primitives;
			RasterizerState = rasterizer;
			VertexDescription = vertexDescription;
			Viewport = viewport;
			Scissor = scissor;
			ColorBlendConstants = blendConstants.GetValueOrDefault(Color.TransparentBlack);
		}
		#endregion Ctor

		/// <summary>
		/// Creates a copy of all current render states into a new independent set of render states.
		/// </summary>
		/// <returns>A copy of the current states in a new set.</returns>
		public RenderStates Copy()
		{
			var copy = new RenderStates();

			if (_colorBlendState.HasValue)
			{
				copy._colorBlendState = _colorBlendState.Value;
				copy.VkColorBlendState = VkColorBlendState;
			}
			copy.VkColorBlendConstants = VkColorBlendConstants;
			if (_depthStencilState.HasValue)
			{
				copy._depthStencilState = _depthStencilState.Value;
				copy.VkDepthStencilState = VkDepthStencilState;
			}
			if (_primitiveInput.HasValue)
			{
				copy._primitiveInput = _primitiveInput.Value;
				copy.VkPrimitiveInput = VkPrimitiveInput;
			}
			if (_rasterizerState.HasValue)
			{
				copy._rasterizerState = _rasterizerState.Value;
				copy.VkRasterizerState = VkRasterizerState;
			}
			if (_vertexDescription.HasValue)
			{
				copy._vertexDescription = _vertexDescription.Value;
				copy.VkVertexDescription = VkVertexDescription;
			}

			copy.Viewport = Viewport;
			copy.Scissor = Scissor;

			return copy;
		}

		// Returns an error string describing an invalid state
		internal string Validate(Renderer.PassInfo pass, Attachment[] atts)
		{
			// Get cached valid info
			if (!_isValid.HasValue)
				_isValid = (_validError = validateStates()) == null;
			if (!_isValid.Value)
				return _validError;

			// Check depth/stencil operations
			if ((UsesStencilBuffer || UsesDepthBuffer) && !pass.DepthStencil.HasValue)
				return "depth/stencil operations not supported in render pass";
			if (UsesStencilBuffer && !atts[pass.DepthStencil.Value].Target.HasStencilData)
				return "stencil operations not supported in render pass";

			// Check viewport/scissor settings
			if (Viewport.HasValue && (Viewport.Value.Width > atts[0].Target.Width || Viewport.Value.Height > atts[0].Target.Height))
				return "viewport is too large for the render pass attachments";
			if (Scissor.HasValue && (Scissor.Value.Width > atts[0].Target.Width || Scissor.Value.Height > atts[0].Target.Height))
				return "scissor is too large for render pass attachments";

			return null;
		}

		private string validateStates()
		{
			var dev = Core.Instance.GraphicsDevice;

			// Check if complete
			if (!IsComplete)
				return "incomplete render states";

			// Check state features
			if (_depthStencilState.Value.DepthBoundsEnable && !dev.Features.DepthBoundsTesting.Enabled)
				return "depth bounds testing not enabled on device";
			if (_primitiveInput.Value.IsListType && _primitiveInput.Value.Restart)
				return "cannot use primitive restart on list topologies";
			if (_rasterizerState.Value.DepthClampEnable && !dev.Features.DepthClamp.Enabled)
				return "depth clamping is not enabled on device";
			if (_rasterizerState.Value.LineWidth.GetValueOrDefault(1.0f) != 1.0f && !dev.Features.WideLines.Enabled)
				return "wide lines not enabled on device";
			if (_rasterizerState.Value.FillMode != FillMode.Solid && !dev.Features.FillModeNonSolid.Enabled)
				return "non-solid fill modes not enabled on device";

			// Check state limits
			var lw = _rasterizerState.Value.LineWidth.GetValueOrDefault(1.0f);
			if (lw < dev.Limits.LineWidth.Min || lw > dev.Limits.LineWidth.Max)
				return $"line width ({lw}) is out of valid range";

			return null;
		}
	}
}
