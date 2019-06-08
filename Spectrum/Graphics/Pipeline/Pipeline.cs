using System;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Fully describes a rendering state, such as the vertex format, shader program, and fixed function settings.
	/// </summary>
	/// <remarks>Encapsulates multiple Vulkan objects into one type: a Pipeline, a RenderPass, and a Framebuffer.</remarks>
	public class Pipeline
	{
		private static readonly Vk.PipelineDynamicStateCreateInfo DEFAULT_DYNAMIC_STATE = new Vk.PipelineDynamicStateCreateInfo(
			Vk.DynamicState.Viewport, Vk.DynamicState.Scissor
		);

		#region Fields
		/// <summary>
		/// Optional name for the pipeline for identification and debugging purposes.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// The graphics device for the pipeline.
		/// </summary>
		public GraphicsDevice Device => SpectrumApp.Instance.GraphicsDevice;

		// The pipeline objects
		internal readonly Vk.Pipeline VkPipeline;
		internal readonly Vk.PipelineLayout VkPipelineLayout;
		internal readonly Vk.RenderPass VkRenderPass;
		#endregion // Fields

		/// <summary>
		/// Creates a new render pipeline using the given description.
		/// </summary>
		/// <param name="desc">The pipeline description. <see cref="PipelineDescription.IsComplete"/> must return <c>true</c>.</param>
		/// <param name="name">An optional name for the pipeline for debug purposes.</param>
		public Pipeline(PipelineDescription desc, string name = null)
		{
			if (desc == null)
				throw new ArgumentNullException(nameof(desc));
			if (!desc.IsComplete)
				throw new InvalidOperationException("Cannot create a pipeline from an incomplete description.");
			Name = name;

			// Create the layout
			var lci = new Vk.PipelineLayoutCreateInfo(null, null);
			VkPipelineLayout = Device.VkDevice.CreatePipelineLayout(lci);

			// Non-user-specified create infos
			var vsci = new Vk.PipelineViewportStateCreateInfo(
				// TODO
			);
			var mssci = new Vk.PipelineMultisampleStateCreateInfo(Vk.SampleCounts.Count1); // TODO: derive this from the framebuffer

			// Pipeline creation
			var gpci = new Vk.GraphicsPipelineCreateInfo(
				VkPipelineLayout,
				null, // TODO
				0,
				desc._shaderCIs,
				desc._piCI.Value,
				desc._vdCI.Value,
				desc._rsCI.Value,
				tessellationState: null, // TODO: control this once we support tessellation shaders
				viewportState: vsci,
				multisampleState: mssci,
				depthStencilState: desc._dssCI.Value,
				colorBlendState: desc._cbsCI.Value,
				dynamicState: DEFAULT_DYNAMIC_STATE,
				flags: Vk.PipelineCreateFlags.None, // TODO: look into derivative pipelines
				basePipelineHandle: null, // TODO: look into derivative pipelines
				basePipelineIndex: -1 // TODO: look into derivative pipelines
			);
			VkPipeline = Device.VkDevice.CreateGraphicsPipeline(gpci, null, null);
		}
	}
}
