﻿using System;
using System.Collections.ObjectModel;
using Vk = VulkanCore;
using static Spectrum.InternalLog;

namespace Spectrum.Graphics
{
	/// <summary>
	/// !!!TODO!!!
	/// Rendering cannot happen without a valid RenderPass instance bound.
	/// </summary>
	public sealed class RenderPass : IDisposable
	{
		#region Fields
		/// <summary>
		/// The name of the render pass, used for debugging and identification.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// The size of the attachments rendered to by this render pass.
		/// </summary>
		public readonly Point Size;

		/// <summary>
		/// The default viewport for rendering with this render pass.
		/// </summary>
		public readonly Viewport DefaultViewport;
		/// <summary>
		/// The default scissor region for rendering with this render pass.
		/// </summary>
		public readonly Scissor DefaultScissor;

		// The Vulkan object containing this render pass
		internal readonly Vk.RenderPass VkRenderPass;
		// The instance of the framebuffer referenced by this render pass (and maybe others)
		internal readonly FramebufferInstance Framebuffer;

		/// <summary>
		/// The names of the subpasses within this render pass.
		/// </summary>
		public readonly ReadOnlyCollection<string> SubpassNames;

		private bool _isDisposed = false;
		#endregion // Fields

		internal RenderPass(string name, Vk.RenderPass pass, FramebufferInstance inst, string[] subpasses, Point size)
		{
			Name = name;
			Size = size;
			DefaultViewport = new Viewport(0, 0, (uint)size.X, (uint)size.Y);
			DefaultScissor = new Scissor(0, 0, (uint)size.X, (uint)size.Y);
			VkRenderPass = pass;
			Framebuffer = inst;
			SubpassNames = Array.AsReadOnly(subpasses);

			Framebuffer.IncRefCount();

			LINFO($"Created new render pass '{name}'.");
		}
		~RenderPass()
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
			if (!_isDisposed && disposing)
			{
				VkRenderPass.Dispose();
				Framebuffer.DecRefCount(); // Will dispose of the framebuffer if this is the last reference
			}
			LINFO($"Disposed render pass '{Name}'.");
			_isDisposed = true;
		}
		#endregion // IDisposable
	}
}