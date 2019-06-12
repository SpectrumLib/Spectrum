using System;
using System.Collections.Generic;
using System.Linq;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Fully describes a rendering state, such as the vertex format, shader program, and fixed function settings.
	/// </summary>
	/// <remarks>Encapsulates multiple Vulkan objects into one type: a Pipeline, a RenderPass, and a Framebuffer.</remarks>
	public class Pipeline : IDisposable
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
		internal readonly Vk.PipelineLayout VkLayout;
		internal readonly Vk.RenderPass VkRenderPass;
		internal readonly Vk.Framebuffer VkFramebuffer;

		// List of render targets used in this pipeline
		private List<RenderTarget> _renderTargets = new List<RenderTarget>();

		// Default objects
		internal readonly Vk.Viewport DefaultViewport;
		internal readonly Vk.Rect2D DefaultScissor;

		private bool _isDisposed = false;
		#endregion // Fields

		/// <summary>
		/// Creates a new render pipeline using the given description.
		/// </summary>
		/// <param name="desc">The pipeline description, which must be complete or an error will occur.</param>
		/// <param name="name">An optional name for the pipeline for debug purposes.</param>
		public Pipeline(PipelineDescription desc, string name = null)
		{
			if (desc == null)
				throw new ArgumentNullException(nameof(desc));
			if (!desc.HasTargets) // Also checked by IsComplete, but this is used to give a more helpful error message
				throw new InvalidOperationException("Cannot create a pipeline without any render targets.");
			if (!desc.IsComplete)
				throw new InvalidOperationException("Cannot create a pipeline from an incomplete description.");
			Name = name;

			// Feature checking
			if ((desc.RasterizerState.Value.FillMode != FillMode.Solid) && !Device.Features.FillModeNonSolid)
				throw new InvalidOperationException("Pipeline cannot be created with non-solid fill modes if that feature is not enabled.");
			if (desc.RasterizerState.Value.LineWidth.HasValue && (desc.RasterizerState.Value.LineWidth.Value != 1) && !Device.Features.WideLines)
				throw new InvalidOperationException("Pipeline cannot be created with wide lines if that feature is not enabled.");
			if (desc.RasterizerState.Value.DepthClampEnable && !Device.Features.DepthClamp)
				throw new InvalidOperationException("Pipeline cannot be created with depth clamping if that feature is not enabled.");

			// Compatibility checking
			if ((desc._dssCI.Value.DepthTestEnable || desc._dssCI.Value.DepthWriteEnable) && !desc._depthAI.HasValue)
				throw new InvalidOperationException("Pipelines with depth operations enabled must be given a depth buffer.");
			if (!desc.CheckRenderTargetSizes())
				throw new InvalidOperationException("Pipeline render targets must all be the same size.");

			// Create the renderpass, framebuffer, and layout
			VkRenderPass = CreateRenderPass(Device.VkDevice, desc, _renderTargets);
			VkFramebuffer = CreateFramebuffer(VkRenderPass, _renderTargets, desc.TargetSize);
			VkLayout = CreateLayout(Device.VkDevice);

			// Non-user-specified create infos
			var vsci = new Vk.PipelineViewportStateCreateInfo(
				DefaultViewport = desc.DefaultViewport.Value.ToVulkanNative(),
				DefaultScissor = desc.DefaultScissor.Value.ToVulkanNative()
			);
			var mssci = new Vk.PipelineMultisampleStateCreateInfo(Vk.SampleCounts.Count1); // TODO: derive this from the framebuffer once we support multisampling

			// Pipeline creation
			var gpci = new Vk.GraphicsPipelineCreateInfo(
				VkLayout,
				VkRenderPass,
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
		~Pipeline()
		{
			dispose(false);
		}

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (disposing && !_isDisposed)
			{
				// Not the best idea, but we cant dispose this until RenderQueues using it are done
				Device.Queues.Graphics.WaitIdle();

				// Destroy the Vulkan objects
				VkPipeline.Dispose();
				VkLayout.Dispose();
				VkRenderPass.Dispose();
				VkFramebuffer.Dispose();

				// Decrement the render target ref counts
				foreach (var rt in _renderTargets)
					rt.DecRefCount();
			}
			_isDisposed = true;
		}
		#endregion // IDisposable

		// Helper function to create a renderpass (and fill a list) from a set of render targets
		// The color attachments will always come first, in the order they are specified
		private static Vk.RenderPass CreateRenderPass(Vk.Device device, PipelineDescription desc, List<RenderTarget> rts)
		{
			// Fill the list
			rts.Clear();
			if (desc.HasColorTargets)
				rts.AddRange(desc.ColorTargets);
			if (desc.HasDepthTarget)
				rts.Add(desc.DepthTarget);

			// Collect the attachment descriptions and create the references
			Vk.AttachmentDescription[] atts = new Vk.AttachmentDescription[desc.TargetCount];
			Vk.AttachmentReference[] crefs = new Vk.AttachmentReference[desc.TargetCount - (desc.HasDepthTarget ? 1 : 0)];
			if (desc.HasColorTargets)
			{
				for (int i = 0; i < desc._colorAIs.Length; ++i)
				{
					atts[i] = desc._colorAIs[i];
					crefs[i] = new Vk.AttachmentReference(i, desc.ColorTargets[i].DefaultImageLayout);
				}
			}
			if (desc.HasDepthTarget)
				atts[atts.Length - 1] = desc._depthAI.Value;

			// Specify the lone subpass
			Vk.SubpassDescription subpass = new Vk.SubpassDescription(
				colorAttachments: crefs,
				depthStencilAttachment: desc.HasDepthTarget ? 
					new Vk.AttachmentReference(atts.Length - 1, desc.DepthTarget.DefaultImageLayout) : (Vk.AttachmentReference?)null
			);

			// Create the renderpass
			var rpci = new Vk.RenderPassCreateInfo(
				new[] { subpass },
				attachments: atts,
				dependencies: new[] { // Create a two dependencies so that the color and depth buffers do not overwrite each other
									  // TODO: Validate the flags used in these
					new Vk.SubpassDependency(Vk.Constant.SubpassExternal, 0, Vk.PipelineStages.ColorAttachmentOutput,
						Vk.PipelineStages.ColorAttachmentOutput, Vk.Accesses.None, Vk.Accesses.ColorAttachmentRead, 
						Vk.Dependencies.ByRegion),
					new Vk.SubpassDependency(Vk.Constant.SubpassExternal, 0, Vk.PipelineStages.LateFragmentTests,
						Vk.PipelineStages.EarlyFragmentTests, Vk.Accesses.None, Vk.Accesses.DepthStencilAttachmentRead,
						Vk.Dependencies.ByRegion)
				}
			);
			return device.CreateRenderPass(rpci);
		}

		// Helper function to create a framebuffer from a renderpass and set of render targets
		private static Vk.Framebuffer CreateFramebuffer(Vk.RenderPass rp, List<RenderTarget> rts, Point size)
		{
			// Increment the ref count of the render targets as they are now part of a new framebuffer
			foreach (var rt in rts)
				rt.IncRefCount();

			// Create the framebuffer
			var fbci = new Vk.FramebufferCreateInfo(
				rts.Select(rt => rt.VkView).ToArray(),
				size.X, size.Y,
				layers: 1
			);
			return rp.CreateFramebuffer(fbci);
		}

		// Helper function to create a pipeline layout
		private static Vk.PipelineLayout CreateLayout(Vk.Device device)
		{
			var lci = new Vk.PipelineLayoutCreateInfo(null, null); // TODO: Check shader to get layout
			return device.CreatePipelineLayout(lci);
		}
	}
}
