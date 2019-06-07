using System;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Used to create graphics pipelines, either from scratch, or derived from existing pipelines. Also assists in
	/// building multiple nearly identical pipelines quickly.
	/// </summary>
	public sealed class PipelineBuilder
	{
		// Default dynamic states for each pipeline (used to implement global state like viewports and scissors)
		private static readonly Vk.PipelineDynamicStateCreateInfo DEFAULT_DYNAMIC_STATE = new Vk.PipelineDynamicStateCreateInfo(
			Vk.DynamicState.Viewport, Vk.DynamicState.Scissor	
		);

		#region Fields
		#region Settings
		/// <summary>
		/// The currently specified color blending settings.
		/// </summary>
		public ColorBlendState? ColorBlendState { get; private set; } = null;
		private Vk.PipelineColorBlendStateCreateInfo? _colorBlendCI = null;
		/// <summary>
		/// If the builder has specified color blending settings.
		/// </summary>
		public bool HasColorBlendState => ColorBlendState.HasValue;

		/// <summary>
		/// The currently selected depth/stencil settings.
		/// </summary>
		public DepthStencilState? DepthStencilState { get; private set; } = null;
		private Vk.PipelineDepthStencilStateCreateInfo? _depthStencilCI = null;
		/// <summary>
		/// If the builder has specified depth/stencil settings.
		/// </summary>
		public bool HasDepthStencilState => DepthStencilState.HasValue;

		/// <summary>
		/// The currently active primitive input settings.
		/// </summary>
		public PrimitiveInput? PrimitiveInput { get; private set; } = null;
		private Vk.PipelineInputAssemblyStateCreateInfo? _inputAssemblyCI = null;
		/// <summary>
		/// If the builder has specified primitive input settings.
		/// </summary>
		public bool HasPrimitiveInput => PrimitiveInput.HasValue;

		/// <summary>
		/// The currently active rasterizer state settings. 
		/// </summary>
		public RasterizerState? RasterizerState { get; private set; } = null;
		private Vk.PipelineRasterizationStateCreateInfo? _rasterizationCI = null;
		/// <summary>
		/// If the builder has specified rasterizer settings.
		/// </summary>
		public bool HasRasterizerState => RasterizerState.HasValue;

		/// <summary>
		/// The currently active vertex description.
		/// </summary>
		public VertexDescription? VertexDescription { get; private set; } = null;
		private Vk.PipelineVertexInputStateCreateInfo? _vertexInputStateCI = null;
		/// <summary>
		/// If the builder has a specified vertex description.
		/// </summary>
		public bool HasVertexDescription => VertexDescription.HasValue;

		/// <summary>
		/// The currently active shader program.
		/// </summary>
		public Shader Shader { get; private set; } = null;
		private Vk.PipelineShaderStageCreateInfo[] _shaderStageCIs = null;
		/// <summary>
		/// If the builder has a specified shader.
		/// </summary>
		public bool HasShader => (Shader != null);
		#endregion // Settings

		// Quick reference to the graphics device
		internal GraphicsDevice Device => SpectrumApp.Instance.GraphicsDevice;

		/// <summary>
		/// Gets if all of the required pipeline state objects have been specified.
		/// </summary>
		public bool IsComplete => ColorBlendState.HasValue && DepthStencilState.HasValue && PrimitiveInput.HasValue &&
			RasterizerState.HasValue && VertexDescription.HasValue && (Shader != null);
		#endregion // Fields

		private PipelineBuilder()
		{

		}

		public static PipelineBuilder New()
		{
			return new PipelineBuilder();
		}

		/// <summary>
		/// Using the specified settings, create a new named pipeline object. <see cref="IsComplete"/> must be true
		/// when this function is called, or an exception will be thrown.
		/// </summary>
		/// <param name="name">The name of the new pipeline.</param>
		/// <param name="renderPass">The render pass containing the subpass that the built pipeline will be compatible with.</param>
		/// <param name="subpass">The name of the subpass that the build pipeline will be compatible with.</param>
		/// <returns>The new pipeline object.</returns>
		public Pipeline Build(string name, RenderPass renderPass, string subpass)
		{
			Build(name, renderPass, subpass, out Pipeline p);
			return p;
		}

		/// <summary>
		/// Using the specified settings, create a new named pipeline object. <see cref="IsComplete"/> must be true
		/// when this function is called, or an exception will be thrown. This version of the function is used to
		/// facilitate chaining. This function also checks the pipeline settings against the enabled features on the
		/// graphics device.
		/// </summary>
		/// <param name="name">The name of the new pipeline.</param>
		/// <param name="renderPass">The render pass containing the subpass that the built pipeline will be compatible with.</param>
		/// <param name="subpass">The name of the subpass that the build pipeline will be compatible with.</param>
		/// <param name="pipeline">The new pipeline object.</param>
		/// <returns>The pipeline builder, to facilitate method chaining.</returns>
		public PipelineBuilder Build(string name, RenderPass renderPass, string subpass, out Pipeline pipeline)
		{
			if (String.IsNullOrWhiteSpace(name))
				throw new ArgumentException("The pipeline name cannot be null or whitespace", nameof(name));
			if (!IsComplete)
				throw new InvalidOperationException("Cannot build a pipeline that is not fully specified."); // TODO: report the missing states
			int spIdx = renderPass.SubpassNames.IndexOf(subpass);
			if (spIdx == -1)
				throw new ArgumentException($"The render pass specified to create the pipeline with does not contain a subpass with the name '{subpass}'", nameof(subpass));

			// Feature checking
			if ((RasterizerState.Value.FillMode != FillMode.Solid) && !Device.Features.FillModeNonSolid)
				throw new InvalidOperationException("Cannot create pipeline, non-solid fill modes are not enabled on the graphics device.");
			if (RasterizerState.Value.LineWidth.HasValue && (RasterizerState.Value.LineWidth.Value != 1) && !Device.Features.WideLines)
				throw new InvalidOperationException("Cannot create pipeline, wide lines are not enabled on the graphics device.");
			if (RasterizerState.Value.DepthClampEnable && !Device.Features.DepthClamp)
				throw new InvalidOperationException("Cannot create pipeline, depth clamping is not enabled on the graphics device.");

			// Non-user-specified create infos
			var vsci = new Vk.PipelineViewportStateCreateInfo(
				renderPass.DefaultViewport.ToVulkanNative(),
				renderPass.DefaultScissor.ToVulkanNative()
			);
			var mssci = new Vk.PipelineMultisampleStateCreateInfo(Vk.SampleCounts.Count1); // TODO: derive this from the framebuffer or renderpass

			// Layout
			var lci = new Vk.PipelineLayoutCreateInfo(null, null);
			var layout = Device.VkDevice.CreatePipelineLayout(lci);

			// Create the pipeline object
			var gpci = new Vk.GraphicsPipelineCreateInfo(
				layout,
				renderPass.VkRenderPass,
				spIdx,
				_shaderStageCIs,
				_inputAssemblyCI.Value,
				_vertexInputStateCI.Value,
				_rasterizationCI.Value,
				tessellationState: null, // TODO: control this once we support tessellation shaders
				viewportState: vsci,
				multisampleState: mssci,
				depthStencilState: _depthStencilCI.Value,
				colorBlendState: _colorBlendCI.Value,
				dynamicState: DEFAULT_DYNAMIC_STATE,
				flags: Vk.PipelineCreateFlags.None, // TODO: look into derivative pipelines
				basePipelineHandle: null, // TODO: look into derivative pipelines
				basePipelineIndex: -1  // TODO: look into derivative pipelines
			);
			var vkpipeline = Device.VkDevice.CreateGraphicsPipeline(gpci); // TODO: look into pipeline caches and how/if to use them

			pipeline = new Pipeline(name, vkpipeline, layout);
			return this;
		}

		#region Settings
		/// <summary>
		/// Sets the color blending state that pipelines from this builder will use.
		/// </summary>
		/// <param name="state">The state to use.</param>
		/// <returns>The same builder, to facilitate method chaining.</returns>
		public PipelineBuilder SetColorBlendState(in ColorBlendState state) => SetColorBlendState(state, out bool _);
		/// <summary>
		/// Sets the color blending state that pipelines from this builder will use.
		/// </summary>
		/// <param name="state">The state to use.</param>
		/// <param name="changed">If there was an existing color blending state that was changed by this call.</param>
		/// <returns>The same builder, to facilitate method chaining.</returns>
		public PipelineBuilder SetColorBlendState(in ColorBlendState state, out bool changed)
		{
			changed = ColorBlendState.HasValue;
			ColorBlendState = state;
			_colorBlendCI = state.ToCreateInfo();
			return this;
		}

		/// <summary>
		/// Sets the depth/stencil state that pipelines from this builder will use.
		/// </summary>
		/// <param name="state">The state to use.</param>
		/// <returns>The same builder, to facilitate method chaining.</returns>
		public PipelineBuilder SetDepthStencilState(in DepthStencilState state) => SetDepthStencilState(state, out bool _);
		/// <summary>
		/// Sets the depth/stencil state that pipelines from this builder will use.
		/// </summary>
		/// <param name="state">The state to use.</param>
		/// <param name="changed">If there was an existing depth/stencil state that was changed by this call.</param>
		/// <returns>The same builder, to facilitate method chaining.</returns>
		public PipelineBuilder SetDepthStencilState(in DepthStencilState state, out bool changed)
		{
			changed = DepthStencilState.HasValue;
			DepthStencilState = state;
			_depthStencilCI = state.ToCreateInfo();
			return this;
		}

		/// <summary>
		/// Sets the primitive input layout that pipelines from this builder will use.
		/// </summary>
		/// <param name="state">The primitive input layout to use.</param>
		/// <returns>The same builder, to facilitate method chaining.</returns>
		public PipelineBuilder SetPrimitiveInput(in PrimitiveInput state) => SetPrimitiveInput(state, out bool _);
		/// <summary>
		/// Sets the primitive input layout that pipelines from this builder will use.
		/// </summary>
		/// <param name="state">The primitive input layout to use.</param>
		/// <param name="changed">If there was an existing primitive input layout that was changed by this call.</param>
		/// <returns>The same builder, to facilitate method chaining.</returns>
		public PipelineBuilder SetPrimitiveInput(in PrimitiveInput state, out bool changed)
		{
			changed = PrimitiveInput.HasValue;
			PrimitiveInput = state;
			_inputAssemblyCI = state.ToCreateInfo();
			return this;
		}

		/// <summary>
		/// Sets the rasterizer state that pipelines from this builder will use.
		/// </summary>
		/// <param name="state">The state to use.</param>
		/// <returns>The same builder, to facilitate method chaining.</returns>
		public PipelineBuilder SetRasterizerState(in RasterizerState state) => SetRasterizerState(state, out bool _);
		/// <summary>
		/// Sets the rasterizer state that pipelines from this builder will use.
		/// </summary>
		/// <param name="state">The state to use.</param>
		/// <param name="changed">If there was an existing rasterizer state that was changed by this call.</param>
		/// <returns>The same builder, to facilitate method chaining.</returns>
		public PipelineBuilder SetRasterizerState(in RasterizerState state, out bool changed)
		{
			changed = RasterizerState.HasValue;
			RasterizerState = state;
			_rasterizationCI = state.ToCreateInfo();
			return this;
		}

		/// <summary>
		/// Sets the color blending state that pipelines from this builder will use.
		/// </summary>
		/// <param name="state">The state to use.</param>
		/// <returns>The same builder, to facilitate method chaining.</returns>
		public PipelineBuilder SetVertexDescription(in VertexDescription state) => SetVertexDescription(state, out bool _);
		/// <summary>
		/// Sets the color blending state that pipelines from this builder will use.
		/// </summary>
		/// <param name="state">The state to use.</param>
		/// <param name="changed">If there was an existing color blending state that was changed by this call.</param>
		/// <returns>The same builder, to facilitate method chaining.</returns>
		public PipelineBuilder SetVertexDescription(in VertexDescription state, out bool changed)
		{
			changed = VertexDescription.HasValue;
			VertexDescription = state;
			_vertexInputStateCI = state.ToCreateInfo();
			return this;
		}

		/// <summary>
		/// Sets the shader program that pipelines from this builder will use.
		/// </summary>
		/// <param name="shader">The shader to use.</param>
		/// <returns>The same builder, to facilitate method chaining.</returns>
		public PipelineBuilder SetShader(Shader shader) => SetShader(shader, out bool _);
		/// <summary>
		/// Sets the shader program that pipelines from this builder will use.
		/// </summary>
		/// <param name="shader">The shader to use.</param>
		/// <param name="changed">If there was an existing shader that was changed by this call.</param>
		/// <returns>The same builder, to facilitate method chaining.</returns>
		public PipelineBuilder SetShader(Shader shader, out bool changed)
		{
			changed = (Shader != null);
			Shader = shader ?? throw new ArgumentNullException(nameof(shader));
			_shaderStageCIs = Shader.CreateInfo;
			return this;
		}
		#endregion // Settings
	}
}
