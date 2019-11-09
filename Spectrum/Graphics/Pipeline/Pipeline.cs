/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
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
		internal Vk.PipelineColorBlendStateCreateInfo VkColorBlendState;
		internal Vk.PipelineDepthStencilStateCreateInfo VkDepthStencilState;
		internal Vk.PipelineInputAssemblyStateCreateInfo VkPrimitiveInput;
		internal Vk.PipelineRasterizationStateCreateInfo VkRasterizerState;
		internal Vk.PipelineVertexInputStateCreateInfo VkVertexDescription;
		#endregion // Backing Fields

		#region State Objects
		/// <summary>
		/// The color blending to use in the pass.
		/// </summary>
		public ColorBlendState? ColorBlendState
		{
			get => _colorBlendState;
			set => (_colorBlendState, VkColorBlendState) = (value, value.GetValueOrDefault().ToVulkanType());
		}

		/// <summary>
		/// The depth/stencil operations to use in the pass.
		/// </summary>
		public DepthStencilState? DepthStencilState
		{
			get => _depthStencilState;
			set => (_depthStencilState, VkDepthStencilState) = (value, value.GetValueOrDefault().ToVulkanType());
		}

		/// <summary>
		/// The primitive topology to use in the pass.
		/// </summary>
		public PrimitiveInput? PrimitiveInput
		{
			get => _primitiveInput;
			set => (_primitiveInput, VkPrimitiveInput) = (value, value.GetValueOrDefault().ToVulkanType());
		}

		/// <summary>
		/// The rasterizer state to use in the pass.
		/// </summary>
		public RasterizerState? RasterizerState
		{
			get => _rasterizerState;
			set => (_rasterizerState, VkRasterizerState) = (value, value.GetValueOrDefault().ToVulkanType());
		}

		/// <summary>
		/// The vertex layout to use in the pass.
		/// </summary>
		public VertexDescription? VertexDescription
		{
			get => _vertexDescription;
			set => (_vertexDescription, VkVertexDescription) = (value, value.GetValueOrDefault().ToVulkanType());
		}
		#endregion // State Objects

		#region Attachment Info
		/// <summary>
		/// If this pipeline requires use of the depth/stencil attachment in the <see cref="Renderer"/>.
		/// </summary>
		public bool UseDepthStencil = false;

		/// <summary>
		/// The indices of the <see cref="Renderer"/> attachments to use as the color buffers for this pass.
		/// </summary>
		public uint[] ColorAttachments = null;

		/// <summary>
		/// The indices of the <see cref="Renderer"/> attachments to use as subpass input attachments for this pass.
		/// </summary>
		public uint[] InputAttachments = null;
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
			ColorBlendState.HasValue && DepthStencilState.HasValue && PrimitiveInput.HasValue && RasterizerState.HasValue &&
			VertexDescription.HasValue;
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
		/// Creates a copy of the current pipeline settings with a new name.
		/// </summary>
		/// <param name="name">The optional new debug name for the copy.</param>
		/// <returns>A new pipeline with copied settings.</returns>
		public Pipeline Copy(string name = null)
		{
			var copy = new Pipeline(name);

			// Settings
			if (_colorBlendState.HasValue)
			{
				copy._colorBlendState = _colorBlendState;
				copy.VkColorBlendState = VkColorBlendState;
			}
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
			copy.UseDepthStencil = UseDepthStencil;
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
		/// <returns>If all pipeline settings are valid.</returns>
		public bool Validate(out string error)
		{
			if (!IsComplete)
			{
				error = "Incomplete Pipeline";
				return false;
			}

			error = null;
			return true;
		}
	}
}
