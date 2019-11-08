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
	/// Collection of objects that fully describe how to process rendering commands within a specific 
	/// <see cref="Renderer"/> pass.
	/// </summary>
	public sealed class Pipeline
	{
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
		/// The index of the <see cref="Renderer"/> attachment to use as the depth/stencil buffer for this pass. If
		/// <c>null</c>, then this pass will not use a depth/stencil attachment.
		/// </summary>
		public uint? DepthStencilAttachment = null;

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
		/// Gets if the pipeline description is complete, with all pipeline values fully defined.
		/// </summary>
		public bool IsComplete =>
			ColorBlendState.HasValue && DepthStencilState.HasValue && PrimitiveInput.HasValue && RasterizerState.HasValue &&
			VertexDescription.HasValue;
		#endregion // Fields
	}
}
