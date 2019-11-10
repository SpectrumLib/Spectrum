/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Linq;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	/// <summary>
	/// The core rendering type. Contains a set of <see cref="RenderTarget"/>s that can be used as attachments in 
	/// multiple rendering passes, which are each described by a <see cref="RenderPass"/> object.
	/// </summary>
	public sealed partial class Renderer : IDisposable
	{
		#region Fields
		// Attachment info
		private Attachment[] _attachments;

		// Vulkan objects
		internal readonly Vk.RenderPass VkRenderPass;
		internal readonly Vk.Framebuffer VkFramebuffer;

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
			}

			// Create the attachment references and dependencies
			CreateAttachmentInfo(passes, framebuffer, out _attachments, out var adescs, out var arefs, out var spdeps);

			// Create the subpasses, and finally the renderpass
			CreateSubpasses(passes, framebuffer, arefs, out var subpasses);
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
		}
		~Renderer()
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
			if (!_isDisposed)
			{
				if (disposing)
				{
					Core.Instance.GraphicsDevice.VkDevice.WaitIdle();

					VkFramebuffer?.Dispose();
					VkRenderPass?.Dispose();
				}

				foreach (var at in _attachments)
					at.Target.DecRefCount();
			}
			_isDisposed = true;
		}
		#endregion // IDisposable
	}
}
