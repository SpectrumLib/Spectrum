/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	/// <summary>
	/// The core rendering type. Contains a set of <see cref="RenderTarget"/>s that can be used as attachments in 
	/// multiple rendering passes, which are each described by a <see cref="Pipeline"/> object.
	/// </summary>
	public sealed class Renderer : IDisposable
	{
		#region Fields
		// Attachment info
		private Attachment[] _attachments;

		// Vulkan objects
		internal readonly Vk.RenderPass VkRenderPass;

		private bool _isDisposed = false;
		#endregion // Fields
		
		/// <summary>
		/// Creates a new renderer using the given framebuffer and pipeline passes.
		/// </summary>
		/// <param name="framebuffer">The attachments to use in the renderer.</param>
		/// <param name="pipelines">The pipelines that describe the render passes.</param>
		public Renderer(Framebuffer framebuffer, Pipeline[] pipelines)
		{
			var dev = Core.Instance.GraphicsDevice;
			if (framebuffer == null)
				throw new ArgumentNullException(nameof(framebuffer));
			if (pipelines == null)
				throw new ArgumentNullException(nameof(pipelines));
			if (pipelines.Length == 0)
				throw new ArgumentException("Renderer must have at least one pipeline.");

			// Validate the framebuffer
			{
				if (!framebuffer.Validate(out var verr))
					throw new ArgumentException($"Invalid renderer attachments: {verr}.");
				framebuffer.CopyAttachments(out _attachments);
			}

			// Validate the pipelines
			{
				var bi = pipelines.IndexOf(pl => !pl.IsComplete);
				if (bi != -1)
					throw new ArgumentException($"Incomplete renderer pipeline at index {bi}.");
				string verr = null;
				bi = pipelines.IndexOf(pl => !pl.Validate(out verr, framebuffer));
				if (bi != -1)
					throw new ArgumentException($"Invalid renderer pipeline at index {bi}: {verr}.");
			}

			// Create the attachment references and dependencies
			var adescs = _attachments.Select(at => at.GetDescription()).ToArray();
			CreateAttachmentInfo(pipelines, adescs, out var arefs, out var spdeps);

			// Create the subpasses, and finally the renderpass
			CreateSubpasses(pipelines, framebuffer, arefs, out var subpasses);
			VkRenderPass = dev.VkDevice.CreateRenderPass(
				attachments: adescs,
				subpasses: subpasses,
				dependencies: spdeps,
				flags: Vk.RenderPassCreateFlags.None
			);

			// Create the framebuffer
			foreach (var at in _attachments)
				at.Target.IncRefCount();
		}
		~Renderer()
		{
			dispose(false);
		}

		#region Creation
		private static void CreateAttachmentInfo(Pipeline[] plines, Vk.AttachmentDescription[] descs,
			out Vk.AttachmentReference[][] refs, out Vk.SubpassDependency[] spdeps)
		{
			// Generate the attachment references
			refs = plines.Select(pl => {
				var ar = Enumerable.Empty<Vk.AttachmentReference>();
				if (pl.ColorAttachments != null)
				{
					ar.Concat(pl.ColorAttachments.Select(idx => new Vk.AttachmentReference {
						Attachment = idx,
						Layout = Vk.ImageLayout.ColorAttachmentOptimal
					}));
				}
				if (pl.InputAttachments != null)
				{
					ar.Concat(pl.InputAttachments?.Select(idx => new Vk.AttachmentReference {
						Attachment = idx,
						Layout = Vk.ImageLayout.ShaderReadOnlyOptimal
					}));
				}
				if (pl.UsesDepthBuffer || pl.UsesStencilBuffer)
					ar.Append(new Vk.AttachmentReference { Attachment = (uint)descs.Length - 1, Layout = Vk.ImageLayout.DepthStencilAttachmentOptimal });
				return ar.ToArray();
			}).ToArray();

			// Generate use matrix for each attachment, across all subpasses
			// Use: 1 = color, 2 = input, 3 = depth/stencil
			byte[][] uses = new byte[descs.Length][];
			for (int i = 0; i < descs.Length; ++i)
				uses[i] = new byte[plines.Length];
			plines.ForEach((pl, pidx) => {
				pl.ColorAttachments?.ForEach(aidx => uses[aidx][pidx] = 1);
				pl.InputAttachments?.ForEach(aidx => uses[aidx][pidx] = 2);
				if (pl.UsesDepthBuffer || pl.UsesStencilBuffer)
					uses[^1][pidx] = 3;
			});

			// Convert the use matrix into subpass dependencies
			HashSet<Vk.SubpassDependency> spd = new HashSet<Vk.SubpassDependency>(new SubpassDependencyComparer());
			uses.ForEach((uarr, aidx) => {
				var uset = uarr.Select((use, pidx) => (idx: (byte)pidx, use))
							   .Where(p => p.use > 0)
							   .Select(p => (idx: p.idx, d: p.use == 3, i: p.use == 2, c: p.use == 1))
							   .ToArray();
				if (uset.Length > 0)
				{
					// Create external input dependency
					if (descs[aidx].LoadOp == Vk.AttachmentLoadOp.Load)
					{
						spd.Add(new Vk.SubpassDependency(
							sourceSubpass:         Vk.Constants.SubpassExternal,
							destinationSubpass:    uset[0].idx,
							sourceStageMask:       (uset[0].d ? Vk.PipelineStageFlags.LateFragmentTests : Vk.PipelineStageFlags.ColorAttachmentOutput) |
							                        Vk.PipelineStageFlags.Transfer,
							destinationStageMask:  uset[0].d ? Vk.PipelineStageFlags.EarlyFragmentTests : Vk.PipelineStageFlags.FragmentShader,
							sourceAccessMask:      (uset[0].d ? Vk.AccessFlags.DepthStencilAttachmentWrite : Vk.AccessFlags.ColorAttachmentWrite) |
							                        Vk.AccessFlags.TransferWrite,
							destinationAccessMask: uset[0].d ? Vk.AccessFlags.DepthStencilAttachmentRead :
												   uset[0].i ? Vk.AccessFlags.InputAttachmentRead : Vk.AccessFlags.ColorAttachmentRead,
							dependencyFlags:       Vk.DependencyFlags.ByRegion
						));
					}

					// Create inter-pass dependencies
					for (uint pidx = 1; pidx < uset.Length; ++pidx)
					{
						ref var src = ref uset[pidx - 1];
						ref var dst = ref uset[pidx];
						spd.Add(new Vk.SubpassDependency(
							sourceSubpass:         src.idx,
							destinationSubpass:    dst.idx,
							sourceStageMask:       src.d ? Vk.PipelineStageFlags.LateFragmentTests : Vk.PipelineStageFlags.ColorAttachmentOutput,
							destinationStageMask:  dst.d ? Vk.PipelineStageFlags.EarlyFragmentTests: Vk.PipelineStageFlags.FragmentShader,
							sourceAccessMask:      src.d ? Vk.AccessFlags.DepthStencilAttachmentWrite : Vk.AccessFlags.ColorAttachmentWrite,
							destinationAccessMask: dst.d ? Vk.AccessFlags.DepthStencilAttachmentRead :
												   dst.i ? Vk.AccessFlags.InputAttachmentRead : Vk.AccessFlags.ColorAttachmentRead,
							dependencyFlags:       Vk.DependencyFlags.ByRegion
						));
					}

					// Create output external dependency (TODO: change this when transient buffers are supported)
					ref var last = ref uset[^1];
					spd.Add(new Vk.SubpassDependency(
						sourceSubpass:         last.idx,
						destinationSubpass:    Vk.Constants.SubpassExternal,
						sourceStageMask:       last.d ? Vk.PipelineStageFlags.LateFragmentTests : Vk.PipelineStageFlags.ColorAttachmentOutput,
						destinationStageMask:  (last.d ? Vk.PipelineStageFlags.EarlyFragmentTests : Vk.PipelineStageFlags.FragmentShader) |
						                        Vk.PipelineStageFlags.Transfer,
						sourceAccessMask:      last.d ? Vk.AccessFlags.DepthStencilAttachmentWrite : Vk.AccessFlags.ColorAttachmentWrite,
						destinationAccessMask: (last.d ? Vk.AccessFlags.DepthStencilAttachmentRead : last.i ? Vk.AccessFlags.InputAttachmentRead :
						                        Vk.AccessFlags.ColorAttachmentRead) | Vk.AccessFlags.TransferRead,
						dependencyFlags:       Vk.DependencyFlags.ByRegion
					));
				}
			});

			// Convert the deps to an array
			spdeps = spd.ToArray();
		}

		private static void CreateSubpasses(Pipeline[] plines, Framebuffer fb, Vk.AttachmentReference[][] atts, out Vk.SubpassDescription[] spasses)
		{
			spasses = plines.Select((pl, pidx) => {
				// Find the unused attachments, and preserve them
				List<uint> preserve = Enumerable.Range(0, (int)fb.Count).Select(idx => (uint)idx).ToList();
				preserve.RemoveAll(idx =>
					(pl.ColorAttachments?.Contains(idx) ?? false) ||
					(pl.InputAttachments?.Contains(idx) ?? false) ||
					((pl.UsesDepthBuffer || pl.UsesStencilBuffer) && idx == fb.Count - 1)
				);

				return new Vk.SubpassDescription {
					DepthStencilAttachment = (pl.UsesDepthBuffer || pl.UsesStencilBuffer) ? atts[pidx][^1] : (Vk.AttachmentReference?)null,
					ColorAttachments = atts[pidx].Where(at => at.Layout == Vk.ImageLayout.ColorAttachmentOptimal).ToArray(),
					InputAttachments = atts[pidx].Where(at => at.Layout == Vk.ImageLayout.ShaderReadOnlyOptimal).ToArray(),
					ResolveAttachments = null,
					PreserveAttachments = preserve.ToArray(),
					PipelineBindPoint = Vk.PipelineBindPoint.Graphics,
					Flags = Vk.SubpassDescriptionFlags.None
				};
			}).ToArray();
		}
		#endregion // Creation

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
					VkRenderPass?.Dispose();
				}

				foreach (var at in _attachments)
					at.Target.DecRefCount();
			}
			_isDisposed = true;
		}
		#endregion // IDisposable

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
