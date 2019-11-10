/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Linq;
using System.Threading;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Collection of objects that fully describe how to process rendering commands within a specific 
	/// <see cref="Renderer"/> pass.
	/// </summary>
	public sealed class Pipeline
	{
		private static int _PipelineIndex = 0;

		#region Fields
		#region Backing Fields
		// Public api values
		private ColorBlendState? _colorBlendState = null;
		private DepthStencilState? _depthStencilState = null;
		private PrimitiveInput? _primitiveInput = null;
		private RasterizerState? _rasterizerState = null;
		private VertexDescription? _vertexDescription = null;

		// Cached Vulkan state objects
		internal Vk.PipelineColorBlendAttachmentState VkColorBlendState;
		internal (float R, float G, float B, float A) VkColorBlendConstants;
		internal Vk.PipelineDepthStencilStateCreateInfo VkDepthStencilState;
		internal Vk.PipelineInputAssemblyStateCreateInfo VkPrimitiveInput;
		internal Vk.PipelineRasterizationStateCreateInfo VkRasterizerState;
		internal Vk.PipelineVertexInputStateCreateInfo VkVertexDescription;

		// Cached validation objects, for all non-framebuffer related validation
		private bool? _isValid = null;
		private string _validError = null;
		#endregion // Backing Fields

		#region State Objects
		/// <summary>
		/// The color blending to use in the pass.
		/// </summary>
		public ColorBlendState? ColorBlendState
		{
			get => _colorBlendState;
			set => (_colorBlendState, VkColorBlendState, _isValid) = (value, value?.ToVulkanType() ?? default, null);
		}

		/// <summary>
		/// The color blending constant values, if any of the color attachments use constant blending operations.
		/// </summary>
		public Color ColorBlendConstants
		{
			get => VkColorBlendConstants;
			set => (VkColorBlendConstants, _isValid) = ((value.RFloat, value.GFloat, value.BFloat, value.AFloat), null);
		}

		/// <summary>
		/// The depth/stencil operations to use in the pass.
		/// </summary>
		public DepthStencilState? DepthStencilState
		{
			get => _depthStencilState;
			set => (_depthStencilState, VkDepthStencilState, _isValid) = (value, value?.ToVulkanType() ?? default, null);
		}

		/// <summary>
		/// The primitive topology to use in the pass.
		/// </summary>
		public PrimitiveInput? PrimitiveInput
		{
			get => _primitiveInput;
			set => (_primitiveInput, VkPrimitiveInput, _isValid) = (value, value?.ToVulkanType() ?? default, null);
		}

		/// <summary>
		/// The rasterizer state to use in the pass.
		/// </summary>
		public RasterizerState? RasterizerState
		{
			get => _rasterizerState;
			set => (_rasterizerState, VkRasterizerState, _isValid) = (value, value?.ToVulkanType() ?? default, null);
		}

		/// <summary>
		/// The vertex layout to use in the pass.
		/// </summary>
		public VertexDescription? VertexDescription
		{
			get => _vertexDescription;
			set => (_vertexDescription, VkVertexDescription, _isValid) = (value, value?.ToVulkanType() ?? default, null);
		}
		#endregion // State Objects

		#region Attachment Info
		/// <summary>
		/// Gets if this pipeline performs operations on a depth buffer.
		/// </summary>
		public bool UsesDepthBuffer => IsComplete && _depthStencilState.Value.HasDepthOperations;

		/// <summary>
		/// Gets if this pipeline performs operations on a stencil buffer.
		/// </summary>
		public bool UsesStencilBuffer => IsComplete && _depthStencilState.Value.HasStencilOperations;

		/// <summary>
		/// The indices of the <see cref="Renderer"/> attachments to use as the color buffers for this pass.
		/// </summary>
		public uint[] ColorAttachments
		{
			get => _colorAttachments;
			set => (_colorAttachments, _isValid) = (value, null);
		}
		private uint[] _colorAttachments = null;

		/// <summary>
		/// The indices of the <see cref="Renderer"/> attachments to use as subpass input attachments for this pass.
		/// </summary>
		public uint[] InputAttachments
		{
			get => _inputAttachments;
			set => (_inputAttachments, _isValid) = (value, null);
		}
		private uint[] _inputAttachments = null;
		#endregion // Attachment Info

		/// <summary>
		/// A debug name for this pipeline. If not specified, it will take the form "Pipeline_X", where X is a
		/// monotonically increasing integer.
		/// </summary>
		public readonly string Name;

		/// <summary>
		/// Gets if the pipeline description is complete, with all pipeline values fully defined.
		/// </summary>
		public bool IsComplete =>
			ColorBlendState.HasValue && DepthStencilState.HasValue && PrimitiveInput.HasValue && 
			RasterizerState.HasValue && VertexDescription.HasValue;
		#endregion // Fields

		/// <summary>
		/// Creates a new pipeline with no specified values, and an optional debug name.
		/// </summary>
		/// <param name="name">The optional debug name for this pipeline.</param>
		public Pipeline(string name = null)
		{
			Name = name ?? $"Pipeline_{Interlocked.Increment(ref _PipelineIndex)}";
			if (String.IsNullOrWhiteSpace(Name))
				throw new ArgumentException("Pipeline name cannot be empty or whitespace.");
		}

		/// <summary>
		/// Create a new pipeline from the given states.
		/// </summary>
		/// <param name="colorblend">The color blending state.</param>
		/// <param name="depthStencil">The depth/stencil state.</param>
		/// <param name="primitiveInput">The primitive input.</param>
		/// <param name="rasterState">The rasterizer control state.</param>
		/// <param name="vertexDescription">The vertex layout description.</param>
		/// <param name="colorAtts">The color attachments.</param>
		/// <param name="inputAtts">The input attachments.</param>
		/// <param name="name">An optional debug name for the pipeline.</param>
		public Pipeline(
			ColorBlendState colorblend,
			DepthStencilState depthStencil,
			PrimitiveInput primitiveInput,
			RasterizerState rasterState,
			VertexDescription vertexDescription,
			uint[] colorAtts,
			uint[] inputAtts,
			string name = null
		)
		{
			ColorBlendState = colorblend;
			DepthStencilState = depthStencil;
			PrimitiveInput = primitiveInput;
			RasterizerState = rasterState;
			VertexDescription = vertexDescription;
			ColorAttachments = colorAtts ?? throw new ArgumentNullException(nameof(colorAtts));
			InputAttachments = inputAtts ?? throw new ArgumentNullException(nameof(inputAtts));
			Name = name ?? $"Pipeline_{Interlocked.Increment(ref _PipelineIndex)}";
			if (String.IsNullOrWhiteSpace(Name))
				throw new ArgumentException("Pipeline name cannot be empty or whitespace.");
		}

		/// <summary>
		/// Creates a copy of the current pipeline settings with a new name.
		/// </summary>
		/// <param name="name">The optional new debug name for the copy.</param>
		/// <returns>A new pipeline with copied settings.</returns>
		public Pipeline Copy(string name = null)
		{
			var copy = new Pipeline(name);

			// Settings
			if (_colorBlendState != null)
			{
				copy._colorBlendState = _colorBlendState;
				copy.VkColorBlendState = VkColorBlendState;
			}
			copy.VkColorBlendConstants = VkColorBlendConstants;
			if (_depthStencilState.HasValue)
			{
				copy._depthStencilState = _depthStencilState;
				copy.VkDepthStencilState = VkDepthStencilState;
			}
			if (_primitiveInput.HasValue)
			{
				copy._primitiveInput = _primitiveInput;
				copy.VkPrimitiveInput = VkPrimitiveInput;
			}
			if (_rasterizerState.HasValue)
			{
				copy._rasterizerState = _rasterizerState;
				copy.VkRasterizerState = VkRasterizerState;
			}
			if (_vertexDescription.HasValue)
			{
				copy._vertexDescription = _vertexDescription;
				copy.VkVertexDescription = VkVertexDescription;
			}

			// Attachments
			if (ColorAttachments != null)
				Array.Copy(ColorAttachments, copy.ColorAttachments = new uint[ColorAttachments.Length], ColorAttachments.Length);
			if (InputAttachments != null)
				Array.Copy(InputAttachments, copy.InputAttachments = new uint[InputAttachments.Length], InputAttachments.Length);

			return copy;
		}

		/// <summary>
		/// Checks that the current pipeline settings are valid with the current device, giving a human-readable
		/// description of invalid settings.
		/// </summary>
		/// <param name="error">Gets set to a human readable error message about the invalid state.</param>
		/// <param name="fbuffer">The framebuffer to check the pipeline against for attachment validity.</param>
		/// <returns>If all pipeline settings are valid.</returns>
		public bool Validate(out string error, Framebuffer fbuffer)
		{
			// Validate state w/ cache
			if (!_isValid.HasValue)
				_isValid = (_validError = validateState()) == null;
			if (!_isValid.Value)
			{
				error = _validError;
				return false;
			}

			// Validate against the framebuffer
			//error = validateFramebuffer(fbuffer);
			return ((error = null) == null);
		}

		private string validateState()
		{
			var dev = Core.Instance.GraphicsDevice;

			// Check if complete
			if (!IsComplete)
				return "incomplete pipeline";

			// Check state features
			if (_depthStencilState.Value.DepthBoundsEnable && !dev.Features.DepthBoundsTesting.Enabled)
				return "depth bounds testing not enabled on device";
			if (_primitiveInput.Value.IsListType && _primitiveInput.Value.Restart)
				return "cannot use primitive restart on list topologies";
			if (_rasterizerState.Value.DepthClampEnable && !dev.Features.DepthClamp.Enabled)
				return "depth clamping is not enabled on device";
			if (_rasterizerState.Value.LineWidth.HasValue && _rasterizerState.Value.LineWidth.Value != 1.0f && !dev.Features.WideLines.Enabled)
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
