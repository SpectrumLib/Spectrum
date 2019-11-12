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
	/// The core rendering type. Contains a set of <see cref="RenderTarget"/>s that can be used as attachments in 
	/// multiple rendering passes, which are each described by a <see cref="RenderPass"/> object.
	/// </summary>
	public sealed partial class Renderer : IDisposable
	{
		private static int _RendererUuid; // Creates unique identifiers for compatibility checking

		#region Fields
		// Attachment/subpass info
		private readonly Attachment[] _attachments;
		private readonly PassInfo[] _passes;

		// Vulkan objects
		internal readonly Vk.RenderPass VkRenderPass;
		internal readonly Vk.Framebuffer VkFramebuffer;
		internal readonly Vk.PipelineCache VkPipelineCache;

		// Pipeline objects
		internal readonly uint Uuid;

		private bool _isDisposed = false;
		#endregion // Fields
		
		/// <summary>
		/// Creates a new renderer using the given framebuffer and render passes.
		/// </summary>
		/// <param name="framebuffer">The attachments to use in the renderer.</param>
		/// <param name="passes">The render pass descriptions.</param>
		public Renderer(Framebuffer framebuffer, RenderPass[] passes)
		{
			var dev = Core.Instance.GraphicsDevice;
			if (framebuffer == null)
				throw new ArgumentNullException(nameof(framebuffer));
			if (passes == null)
				throw new ArgumentNullException(nameof(passes));
			if (passes.Length == 0)
				throw new ArgumentException("Renderer received zero RenderPasses.", nameof(passes));
			if (passes.Any(pass => pass == null))
				throw new ArgumentException("Renderer received null RenderPass.", nameof(passes));

			// Validate the render passes with the framebuffer
			{
				if (passes.GroupBy(rp => rp.Name).FirstOrDefault(g => g.Count() > 1) is var rname && rname != null)
					throw new ArgumentException($"Renderer received duplicate RenderPass name \"{rname.Key}\".");
				string incom = null;
				if (passes.FirstOrDefault(rp => (incom = rp.CheckCompatibility(framebuffer)) != null) is var brp && brp != null)
					throw new ArgumentException($"Incompatible RenderPass \"{brp.Name}\" - {incom}.");
				_passes = passes.Select((pass, pidx) => new PassInfo(pass, framebuffer, (uint)pidx)).ToArray();
			}

			// Create the attachment references and dependencies
			CreateAttachmentInfo(_passes, framebuffer, out _attachments, out var adescs, out var arefs, out var spdeps);

			// Create the subpasses, and finally the renderpass
			CreateSubpasses(_passes, framebuffer, arefs, out var subpasses);
			VkRenderPass = dev.VkDevice.CreateRenderPass(
				attachments: adescs,
				subpasses: subpasses,
				dependencies: spdeps,
				flags: Vk.RenderPassCreateFlags.None
			);

			// Create the framebuffer
			VkFramebuffer = dev.VkDevice.CreateFramebuffer(
				renderPass: VkRenderPass,
				attachments: _attachments.Select(at => at.Target.VkView).ToArray(),
				width: framebuffer.Size.Width,
				height: framebuffer.Size.Height,
				layers: 1,
				flags: Vk.FramebufferCreateFlags.None
			);
			foreach (var at in _attachments)
				at.Target.IncRefCount();

			// Create the pipeline cache for this renderer
			VkPipelineCache = dev.VkDevice.CreatePipelineCache(
				initialData: null,
				flags: Vk.PipelineCacheCreateFlags.None
			);
			Uuid = (uint)Interlocked.Increment(ref _RendererUuid);
		}
		~Renderer()
		{
			dispose(false);
		}

		#region Pipelines
		/// <summary>
		/// Creates a new pipeline compatible with the given render pass name, using the given render states.
		/// </summary>
		/// <param name="passName">The name of the <see cref="RenderPass"/> the pipeline will be used in.</param>
		/// <param name="states">The <see cref="RenderStates"/> to use in the pipeline.</param>
		/// <returns>A new pipeline object for this renderer, with the given states.</returns>
		public Pipeline CreatePipeline(string passName, RenderStates states)
		{
			// Get the pass
			if (_passes.FirstOrDefault(pi => pi.Name == passName) is var pinfo && pinfo == null)
				throw new ArgumentException($"Renderer does not contain a pass with the name \"{passName}\".");

			// Validate the states with the pass
			if (states.Validate(pinfo, _attachments) is var verr && verr != null)
				throw new ArgumentException($"Invalid RenderStates for pass \"{passName}\" - {verr}.");

			// Pipeline create infos
			var tss = new Vk.PipelineTessellationStateCreateInfo { // TODO: Revisit once we support tessellation
				PatchControlPoints = 1,
				Flags = Vk.PipelineTessellationStateCreateFlags.None
			};
			var vps = new Vk.PipelineViewportStateCreateInfo {
				Viewports = new [] { states.Viewport.GetValueOrDefault(_attachments[0].Target.DefaultViewport).ToVulkanType() },
				Scissors = new [] { states.Scissor.GetValueOrDefault(_attachments[0].Target.DefaultScissor).ToVulkanType() },
				Flags = Vk.PipelineViewportStateCreateFlags.None
			};
			var mss = new Vk.PipelineMultisampleStateCreateInfo { // TODO: Revisit once we have multisampling
				RasterizationSamples = Vk.SampleCountFlags.SampleCount1,
				SampleShadingEnable = false,
				AlphaToCoverageEnable = false,
				AlphaToOneEnable = false,
				Flags = Vk.PipelineMultisampleStateCreateFlags.None
			};
			var cbs = new Vk.PipelineColorBlendStateCreateInfo {
				Attachments = Enumerable.Repeat(states.VkColorBlendState, (int)pinfo.AttachmentCount).ToArray(),
				LogicOpEnable = false,
				BlendConstants = states.VkColorBlendConstants,
				Flags = Vk.PipelineColorBlendStateCreateFlags.None
			};

			// Create the pipeline layout (TODO: pull this info from the shader, or associate with each shader)
			var layout = Core.Instance.GraphicsDevice.VkDevice.CreatePipelineLayout(
				setLayouts: null,
				pushConstantRanges: null,
				flags: Vk.PipelineLayoutCreateFlags.None
			);

			// Create the pipeline object
			var pipeline = Core.Instance.GraphicsDevice.VkDevice.CreateGraphicsPipeline(
				pipelineCache:      VkPipelineCache,
				stages:             null, ////* TODO: pull this data from the shader *////
				rasterizationState: states.VkRasterizerState,
				layout:             layout,
				renderPass:         VkRenderPass,
				subpass:            pinfo.Index,
				basePipelineHandle: null,
				basePipelineIndex:  0,
				flags:              Vk.PipelineCreateFlags.None,
				vertexInputState:   states.VkVertexDescription,
				inputAssemblyState: states.VkPrimitiveInput,
				tessellationState:  tss,
				viewportState:      vps,
				multisampleState:   mss,
				depthStencilState:  states.VkDepthStencilState,
				colorBlendState:    cbs,
				dynamicState:       null ////* TODO: See if there are dynamic states we could support *////
			);
			return new Pipeline(this, pinfo, pipeline, layout);
		}
		#endregion // Pipelines

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}
		
		private void dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					Core.Instance.GraphicsDevice.VkDevice.WaitIdle();

					VkFramebuffer?.Dispose();
					VkRenderPass?.Dispose();

					VkPipelineCache?.Dispose();
				}

				foreach (var at in _attachments)
					at.Target.DecRefCount();
			}
			_isDisposed = true;
		}
		#endregion // IDisposable

		// Internal type for tracking RenderPass info
		internal class PassInfo
		{
			public readonly string Name;
			public readonly uint Index;
			public readonly uint? DepthStencil;
			public readonly (string Name, uint Index)[] ColorAttachments;
			public readonly (string Name, uint Index)[] InputAttachments;
			public readonly uint AttachmentCount;

			public PassInfo(RenderPass pass, Framebuffer fb, uint index)
			{
				Name = pass.Name;
				Index = index;
				DepthStencil = pass.UseDepthStencil ? fb.GetDepthStencilIndex().Value : (uint?)null;
				ColorAttachments = pass.ColorAttachments.Select((cat, cidx) => (cat, fb.GetColorIndex(cat).Value)).ToArray();
				InputAttachments = pass.InputAttachments.Select((cat, cidx) => (cat, fb.GetColorIndex(cat).Value)).ToArray();
				AttachmentCount = (uint)(ColorAttachments.Length + InputAttachments.Length + (DepthStencil.HasValue ? 1 : 0));
			}
		}
	}
}
