using System;
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

		// Vulkan pipeline create infos
		internal Vk.PipelineColorBlendStateCreateInfo? _cbsCI = null;
		internal Vk.PipelineDepthStencilStateCreateInfo? _dssCI = null;
		internal Vk.PipelineInputAssemblyStateCreateInfo? _piCI = null;
		internal Vk.PipelineRasterizationStateCreateInfo? _rsCI = null;
		internal Vk.PipelineVertexInputStateCreateInfo? _vdCI = null;
		internal Vk.PipelineShaderStageCreateInfo[] _shaderCIs = null;
		#endregion // Backing Fields

		#region Public Settings

		public ColorBlendState? ColorBlendState
		{
			get { return _colorBlendState; }
			set
			{
				_colorBlendState = value;
				_cbsCI = value.HasValue ? value.Value.ToCreateInfo() : (Vk.PipelineColorBlendStateCreateInfo?)null;
			}
		}

		public DepthStencilState? DepthStencilState
		{
			get { return _depthStencilState; }
			set
			{
				_depthStencilState = value;
				_dssCI = value.HasValue ? value.Value.ToCreateInfo() : (Vk.PipelineDepthStencilStateCreateInfo?)null;
			}
		}

		public PrimitiveInput? PrimitiveInput
		{
			get { return _primitiveInput; }
			set
			{
				_primitiveInput = value;
				_piCI = value.HasValue ? value.Value.ToCreateInfo() : (Vk.PipelineInputAssemblyStateCreateInfo?)null;
			}
		}

		public RasterizerState? RasterizerState
		{
			get { return _rasterizerState; }
			set
			{
				_rasterizerState = value;
				_rsCI = value.HasValue ? value.Value.ToCreateInfo() : (Vk.PipelineRasterizationStateCreateInfo?)null;
			}
		}

		public VertexDescription? VertexDescription
		{
			get { return _vertexDescription; }
			set
			{
				_vertexDescription = value;
				_vdCI = value.HasValue ? value.Value.ToCreateInfo() : (Vk.PipelineVertexInputStateCreateInfo?)null;
			}
		}

		public Shader Shader
		{
			get { return _shader; }
			set
			{
				_shader = value;
				_shaderCIs = value.CreateInfo;
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
		/// Gets if this can fully describe a pipeline. If this is <c>false</c>, then attempting to create a
		/// <see cref="Pipeline"/> instance with this description will fail.
		/// </summary>
		public bool IsComplete =>
			_colorBlendState.HasValue && _depthStencilState.HasValue && _primitiveInput.HasValue && 
			_rasterizerState.HasValue && _vertexDescription.HasValue && (_shader != null);
		#endregion // Checking
		#endregion // Fields
	}
}
