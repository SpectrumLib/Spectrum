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
		#endregion // Settings

		/// <summary>
		/// Gets if all of the required pipeline state objects have been specified.
		/// </summary>
		public bool IsComplete => ColorBlendState.HasValue && DepthStencilState.HasValue && PrimitiveInput.HasValue &&
			RasterizerState.HasValue && VertexDescription.HasValue;
		#endregion // Fields

		private PipelineBuilder()
		{

		}

		public static PipelineBuilder New()
		{
			return new PipelineBuilder();
		}

		#region Settings
		/// <summary>
		/// Sets the color blending state that pipelines from this builder will use.
		/// </summary>
		/// <param name="state">The state to use.</param>
		/// <returns>The same builder, to facilitate method chaining.</returns>
		public PipelineBuilder SetColorBlendState(in ColorBlendState state) => SetColorBlendState(state, out bool changed);
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
		public PipelineBuilder SetDepthStencilState(in DepthStencilState state) => SetDepthStencilState(state, out bool changed);
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
		public PipelineBuilder SetPrimitiveInput(in PrimitiveInput state) => SetPrimitiveInput(state, out bool changed);
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
		public PipelineBuilder SetRasterizerState(in RasterizerState state) => SetRasterizerState(state, out bool changed);
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
		public PipelineBuilder SetVertexDescription(in VertexDescription state) => SetVertexDescription(state, out bool changed);
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
		#endregion // Settings
	}
}
