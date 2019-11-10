/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	// Creation functions for renderer objects
	public sealed partial class Renderer : IDisposable
	{
		private static void CreateAttachmentInfo(RenderPass[] passes, Framebuffer fb, out Attachment[] atts,
			out Vk.AttachmentDescription[] descs, out Vk.AttachmentReference[][] refs, out Vk.SubpassDependency[] spdeps)
		{
			// Create the descriptions
			fb.CopyAttachments(out atts);
			descs = atts.Select(at => at.GetDescription()).ToArray();

			// Convert the pass attachment names into attachment indices
			(uint? ds, uint[] c, uint[] i)[] paidx = passes.Select((pass, pidx) => (
				ds: pass.UseDepthStencil ? fb.GetDepthStencilIndex().Value : (uint?)null,
				c:  pass.ColorAttachments.Select(aname => fb.GetColorIndex(aname).Value).ToArray(),
				i:  pass.InputAttachments.Select(aname => fb.GetColorIndex(aname).Value).ToArray()
			)).ToArray();

			// Generate the attachment references
			refs = paidx.Select(pass => {
				var ar = new List<Vk.AttachmentReference>();
				if (pass.c.Length > 0)
				{
					ar.AddRange(pass.c.Select(idx => new Vk.AttachmentReference {
						Attachment = idx,
						Layout = Vk.ImageLayout.ColorAttachmentOptimal
					}));
				}
				if (pass.i.Length > 0)
				{
					ar.AddRange(pass.i.Select(idx => new Vk.AttachmentReference {
						Attachment = idx,
						Layout = Vk.ImageLayout.ShaderReadOnlyOptimal
					}));
				}
				if (pass.ds.HasValue)
				{
					ar.Add(new Vk.AttachmentReference { 
						Attachment = pass.ds.Value,
						Layout = Vk.ImageLayout.DepthStencilAttachmentOptimal
					});
				}
				return ar.ToArray();
			}).ToArray();

			// Generate use matrix for each attachment, across all subpasses
			// Use: 1 = color, 2 = input, 3 = depth/stencil
			(bool p, byte[] u)[] uses = new (bool, byte[])[descs.Length];
			for (int i = 0; i < descs.Length; ++i)
				uses[i] = (atts[i].Preserve, new byte[passes.Length]);
			paidx.ForEach((pass, pidx) => {
				pass.c.ForEach(aidx => uses[aidx].u[pidx] = 1);
				pass.i.ForEach(aidx => uses[aidx].u[pidx] = 2);
				if (pass.ds.HasValue)
					uses[pass.ds.Value].u[pidx] = 3;
			});

			// Convert the use matrix into subpass dependencies
			HashSet<Vk.SubpassDependency> spd = new HashSet<Vk.SubpassDependency>(new SubpassDependencyComparer());
			uses.ForEach((auses, aidx) => {
				var uset = auses.u.Select((use, pidx) => (idx: (byte)pidx, use))
							      .Where(p => p.use > 0)
							      .Select(p => (idx: p.idx, d: p.use == 3, i: p.use == 2, c: p.use == 1))
							      .ToArray();
				if (uset.Length > 0)
				{
					// Create external input dependency
					if (auses.p)
					{
						spd.Add(new Vk.SubpassDependency(
							sourceSubpass: Vk.Constants.SubpassExternal,
							destinationSubpass: uset[0].idx,
							sourceStageMask: (uset[0].d ? Vk.PipelineStageFlags.LateFragmentTests : Vk.PipelineStageFlags.ColorAttachmentOutput) |
													Vk.PipelineStageFlags.Transfer,
							destinationStageMask: uset[0].d ? Vk.PipelineStageFlags.EarlyFragmentTests : Vk.PipelineStageFlags.FragmentShader,
							sourceAccessMask: (uset[0].d ? Vk.AccessFlags.DepthStencilAttachmentWrite : Vk.AccessFlags.ColorAttachmentWrite) |
													Vk.AccessFlags.TransferWrite,
							destinationAccessMask: uset[0].d ? Vk.AccessFlags.DepthStencilAttachmentRead :
												   uset[0].i ? Vk.AccessFlags.InputAttachmentRead : Vk.AccessFlags.ColorAttachmentRead,
							dependencyFlags: Vk.DependencyFlags.ByRegion
						));
					}

					// Create inter-pass dependencies
					for (uint pidx = 1; pidx < uset.Length; ++pidx)
					{
						ref var src = ref uset[pidx - 1];
						ref var dst = ref uset[pidx];
						spd.Add(new Vk.SubpassDependency(
							sourceSubpass: src.idx,
							destinationSubpass: dst.idx,
							sourceStageMask: src.d ? Vk.PipelineStageFlags.LateFragmentTests : Vk.PipelineStageFlags.ColorAttachmentOutput,
							destinationStageMask: dst.d ? Vk.PipelineStageFlags.EarlyFragmentTests : Vk.PipelineStageFlags.FragmentShader,
							sourceAccessMask: src.d ? Vk.AccessFlags.DepthStencilAttachmentWrite : Vk.AccessFlags.ColorAttachmentWrite,
							destinationAccessMask: dst.d ? Vk.AccessFlags.DepthStencilAttachmentRead :
												   dst.i ? Vk.AccessFlags.InputAttachmentRead : Vk.AccessFlags.ColorAttachmentRead,
							dependencyFlags: Vk.DependencyFlags.ByRegion
						));
					}

					// Create output external dependency (TODO: change this when transient buffers are supported)
					ref var last = ref uset[^1];
					spd.Add(new Vk.SubpassDependency(
						sourceSubpass: last.idx,
						destinationSubpass: Vk.Constants.SubpassExternal,
						sourceStageMask: last.d ? Vk.PipelineStageFlags.LateFragmentTests : Vk.PipelineStageFlags.ColorAttachmentOutput,
						destinationStageMask: (last.d ? Vk.PipelineStageFlags.EarlyFragmentTests : Vk.PipelineStageFlags.FragmentShader) |
												Vk.PipelineStageFlags.Transfer,
						sourceAccessMask: last.d ? Vk.AccessFlags.DepthStencilAttachmentWrite : Vk.AccessFlags.ColorAttachmentWrite,
						destinationAccessMask: (last.d ? Vk.AccessFlags.DepthStencilAttachmentRead : Vk.AccessFlags.ColorAttachmentRead) |
												Vk.AccessFlags.TransferRead,
						dependencyFlags: Vk.DependencyFlags.ByRegion
					));
				}
			});

			// Convert the deps to an array
			spdeps = spd.ToArray();
		}

		private static void CreateSubpasses(RenderPass[] passes, Framebuffer fb, Vk.AttachmentReference[][] atts, out Vk.SubpassDescription[] spasses)
		{
			// Convert the pass attachment names into attachment indices
			(uint? ds, uint[] c, uint[] i)[] paidx = passes.Select((pass, pidx) => (
				ds: pass.UseDepthStencil ? fb.GetDepthStencilIndex().Value : (uint?)null,
				c: pass.ColorAttachments.Select(aname => fb.GetColorIndex(aname).Value).ToArray(),
				i: pass.InputAttachments.Select(aname => fb.GetColorIndex(aname).Value).ToArray()
			)).ToArray();

			// Create the subpass descriptions
			spasses = paidx.Select((pass, pidx) => {
				// Find the unused attachments, and preserve them
				List<uint> preserve = Enumerable.Range(0, (int)fb.Count).Select(idx => (uint)idx).ToList();
				preserve.RemoveAll(idx => pass.c.Contains(idx) || pass.i.Contains(idx) || (pass.ds.HasValue && idx == pass.ds.Value));

				return new Vk.SubpassDescription {
					DepthStencilAttachment = pass.ds.HasValue ? atts[pidx][^1] : (Vk.AttachmentReference?)null,
					ColorAttachments = atts[pidx].Where(at => at.Layout == Vk.ImageLayout.ColorAttachmentOptimal).ToArray(),
					InputAttachments = atts[pidx].Where(at => at.Layout == Vk.ImageLayout.ShaderReadOnlyOptimal).ToArray(),
					ResolveAttachments = null,
					PreserveAttachments = preserve.ToArray(),
					PipelineBindPoint = Vk.PipelineBindPoint.Graphics,
					Flags = Vk.SubpassDescriptionFlags.None
				};
			}).ToArray();
		}

		//private static void CreatePipelines(Pipeline[] plines, Vk.RenderPass rp, int acount, out Vk.Pipeline[] vkplines)
		//{
		//	vkplines = plines.Select((pl, pidx) => Core.Instance.GraphicsDevice.VkDevice.CreateGraphicsPipeline(
		//		pipelineCache: null,
		//		stages: null, // TODO
		//		rasterizationState: pl.VkRasterizerState,
		//		layout: null, // TODO
		//		renderPass: rp,
		//		subpass: (uint)pidx,
		//		basePipelineHandle: null,
		//		basePipelineIndex: 0,
		//		flags: Vk.PipelineCreateFlags.None,
		//		vertexInputState: pl.VkVertexDescription,
		//		inputAssemblyState: pl.VkPrimitiveInput,
		//		tessellationState: new Vk.PipelineTessellationStateCreateInfo
		//		{
		//			PatchControlPoints = 1,
		//			Flags = Vk.PipelineTessellationStateCreateFlags.None
		//		},
		//		viewportState: new Vk.PipelineViewportStateCreateInfo
		//		{ // This state is dynamic, so dummy values are okay
		//			Scissors = new[] { new Vk.Rect2D { Offset = new Vk.Offset2D(0, 0), Extent = new Vk.Extent2D(1, 1) } },
		//			Viewports = new[] { new Vk.Viewport(0, 0, 1, 1, 0, 1) },
		//			Flags = Vk.PipelineViewportStateCreateFlags.None
		//		},
		//		multisampleState: new Vk.PipelineMultisampleStateCreateInfo
		//		{
		//			RasterizationSamples = Vk.SampleCountFlags.SampleCount1,
		//			SampleShadingEnable = false,
		//			AlphaToOneEnable = false,
		//			AlphaToCoverageEnable = false,
		//			MinSampleShading = 0,
		//			SampleMask = null
		//		},
		//		depthStencilState: pl.VkDepthStencilState,
		//		colorBlendState: new Vk.PipelineColorBlendStateCreateInfo
		//		{
		//			Attachments = Enumerable.Repeat(pl.VkColorBlendState, acount).ToArray(),
		//			LogicOpEnable = false,
		//			BlendConstants = pl.VkColorBlendConstants,
		//			Flags = Vk.PipelineColorBlendStateCreateFlags.None
		//		},
		//		dynamicState: new Vk.PipelineDynamicStateCreateInfo
		//		{
		//			DynamicStates = new[] { Vk.DynamicState.Viewport, Vk.DynamicState.Scissor },
		//			Flags = Vk.PipelineDynamicStateCreateFlags.None
		//		}
		//	)).ToArray();
		//}

		// Used to ensure unique subpass dependencies
		private class SubpassDependencyComparer : IEqualityComparer<Vk.SubpassDependency>
		{
			bool IEqualityComparer<Vk.SubpassDependency>.Equals(Vk.SubpassDependency x, Vk.SubpassDependency y) =>
				x.SourceSubpass == y.SourceSubpass && x.DestinationSubpass == y.DestinationSubpass &&
				x.SourceStageMask == y.SourceStageMask && x.DestinationStageMask == y.DestinationStageMask &&
				x.SourceAccessMask == y.SourceAccessMask && x.DestinationAccessMask == y.DestinationAccessMask &&
				x.DependencyFlags == y.DependencyFlags;

			int IEqualityComparer<Vk.SubpassDependency>.GetHashCode(Vk.SubpassDependency obj) =>
				(int)obj.SourceSubpass | (int)(obj.DestinationSubpass << 3) | ((int)obj.DependencyFlags << 6) |
				((int)obj.SourceStageMask << 8) | ((int)obj.DestinationStageMask << 8) |
				((int)obj.SourceAccessMask << 8) | ((int)obj.DestinationAccessMask << 8);
		}
	}
}
