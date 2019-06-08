using System;
using System.Linq;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Contains the set of values that fully describe a <see cref="Pipeline"/>.
	/// </summary>
	public class PipelineDescription
	{
		#region Fields
		#region Backing Fields
		private ColorBlendState? _colorBlendState = null;
		private DepthStencilState? _depthStencilState = null;
		private PrimitiveInput? _primitiveInput = null;
		private RasterizerState? _rasterizerState = null;
		private VertexDescription? _vertexDescription = null;
		private Shader _shader = null;
		private RenderTarget _depthRT = null;
		private RenderTarget[] _colorRTs = null;

		// Vulkan pipeline create infos
		internal Vk.PipelineColorBlendStateCreateInfo? _cbsCI = null;
		internal Vk.PipelineDepthStencilStateCreateInfo? _dssCI = null;
		internal Vk.PipelineInputAssemblyStateCreateInfo? _piCI = null;
		internal Vk.PipelineRasterizationStateCreateInfo? _rsCI = null;
		internal Vk.PipelineVertexInputStateCreateInfo? _vdCI = null;
		internal Vk.PipelineShaderStageCreateInfo[] _shaderCIs = null;
		internal Vk.AttachmentDescription? _depthAI = null;
		internal Vk.AttachmentDescription[] _colorAIs = null;
		#endregion // Backing Fields

		#region Public Settings
		/// <summary>
		/// The specified color blend state, if any.
		/// </summary>
		public ColorBlendState? ColorBlendState
		{
			get => _colorBlendState;
			set
			{
				_colorBlendState = value;
				_cbsCI = value.HasValue ? value.Value.ToCreateInfo() : (Vk.PipelineColorBlendStateCreateInfo?)null;
			}
		}
		/// <summary>
		/// The specified depth stencil state, if any.
		/// </summary>
		public DepthStencilState? DepthStencilState
		{
			get => _depthStencilState;
			set
			{
				_depthStencilState = value;
				_dssCI = value.HasValue ? value.Value.ToCreateInfo() : (Vk.PipelineDepthStencilStateCreateInfo?)null;
			}
		}
		/// <summary>
		/// The specified primitive input style, if any.
		/// </summary>
		public PrimitiveInput? PrimitiveInput
		{
			get => _primitiveInput;
			set
			{
				_primitiveInput = value;
				_piCI = value.HasValue ? value.Value.ToCreateInfo() : (Vk.PipelineInputAssemblyStateCreateInfo?)null;
			}
		}
		/// <summary>
		/// The specified rasterizer state, if any.
		/// </summary>
		public RasterizerState? RasterizerState
		{
			get => _rasterizerState;
			set
			{
				_rasterizerState = value;
				_rsCI = value.HasValue ? value.Value.ToCreateInfo() : (Vk.PipelineRasterizationStateCreateInfo?)null;
			}
		}
		/// <summary>
		/// The specified vertex input description, if any.
		/// </summary>
		public VertexDescription? VertexDescription
		{
			get => _vertexDescription;
			set
			{
				_vertexDescription = value;
				_vdCI = value.HasValue ? value.Value.ToCreateInfo() : (Vk.PipelineVertexInputStateCreateInfo?)null;
			}
		}
		/// <summary>
		/// The specified shader.
		/// </summary>
		public Shader Shader
		{
			get => _shader;
			set
			{
				_shader = value ?? throw new InvalidOperationException("Cannot use a null shader in a pipeline description");
				_shaderCIs = value.CreateInfo;
			}
		}
		/// <summary>
		/// The specified depth/stencil render target.
		/// </summary>
		public RenderTarget DepthTarget
		{
			get => _depthRT;
			set
			{
				_depthRT = value;
				_depthAI = value?.GetDescription();
			}
		}
		/// <summary>
		/// The specified color render targets. The order must match the desired order of fragment shader outputs.
		/// </summary>
		public RenderTarget[] ColorTargets
		{
			get => _colorRTs;
			set
			{
				_colorRTs = value;
				_colorAIs = value?.Select(rt => rt.GetDescription()).ToArray();
			}
		}
		#endregion // Public Settings

		#region Checking
		/// <summary>
		/// Gets if the descrption has a specified color blend state.
		/// </summary>
		public bool HasColorBlendState => _colorBlendState.HasValue;
		/// <summary>
		/// Gets if the descrption has a specified depth stencil state.
		/// </summary>
		public bool HasDepthStencilState => _depthStencilState.HasValue;
		/// <summary>
		/// Gets if the descrption has a specified primitive input.
		/// </summary>
		public bool HasPrimitiveInput => _primitiveInput.HasValue;
		/// <summary>
		/// Gets if the descrption has a specified rasterizer state.
		/// </summary>
		public bool HasRasterizerState => _rasterizerState.HasValue;
		/// <summary>
		/// Gets if the descrption has a specified vertex description.
		/// </summary>
		public bool HasVertexDescription => _vertexDescription.HasValue;
		/// <summary>
		/// Gets if the descrption has a specified shader.
		/// </summary>
		public bool HasShader => (_shader != null);
		/// <summary>
		/// Gets if the description has a specified depth render target.
		/// </summary>
		public bool HasDepthTarget => (_depthRT != null);
		/// <summary>
		/// Gets if the description has one or more specified color render targets.
		/// </summary>
		public bool HasColorTargets => (_colorRTs != null) && (_colorRTs.Length > 0);

		/// <summary>
		/// Gets if this can fully describe a pipeline. If this is <c>false</c>, then attempting to create a
		/// <see cref="Pipeline"/> instance with this description will fail.
		/// </summary>
		public bool IsComplete =>
			_colorBlendState.HasValue && _depthStencilState.HasValue && _primitiveInput.HasValue &&
			_rasterizerState.HasValue && _vertexDescription.HasValue && (_shader != null) && HasTargets;
		/// <summary>
		/// Gets if the pipeline description has at least one render target of any type, which is a
		/// requirement for the description to be considered complete.
		/// </summary>
		public bool HasTargets => HasDepthTarget || HasColorTargets;

		/// <summary>
		/// Gets the size of the render targets for this description. Does not check render target validity before
		/// getting the size. Will return <see cref="Point.Zero"/> if there are no render targets.
		/// </summary>
		public Point TargetSize => HasColorTargets ? _colorRTs[0].Size : (_depthRT?.Size ?? Point.Zero);
		/// <summary>
		/// Gets the number of render targets specified in this description.
		/// </summary>
		public uint TargetCount => (HasColorTargets ? (uint)_colorRTs.Length : 0u) + ((_depthRT != null) ? 1u : 0u);

		/// <summary>
		/// Gets the default viewport for pipelines created with this description. This function does not check for
		/// render target validity before creating the viewport.
		/// </summary>
		public Viewport? DefaultViewport =>
			HasColorTargets ? _colorRTs[0].DefaultViewport : _depthRT?.DefaultViewport;

		/// <summary>
		/// Gets the default scissor for pipelines created with this description. This function does not check for
		/// render target validity before creating the scissor.
		/// </summary>
		public Scissor? DefaultScissor =>
			HasColorTargets ? _colorRTs[0].DefaultScissor : _depthRT?.DefaultScissor;
		#endregion // Checking
		#endregion // Fields

		/// <summary>
		/// Gets if all of the render targets have the same size (a requirement for a valid pipeline description).
		/// </summary>
		/// <returns>If all of the render targets are the same size.</returns>
		public bool CheckRenderTargetSizes()
		{
			Point sz = TargetSize;
			if (sz == Point.Zero)
				return true; // No render targets
			if (_depthRT != null && _depthRT.Size != sz)
				return false;
			return _colorRTs?.All(rt => rt.Size == sz) ?? true;
		}
	}
}
