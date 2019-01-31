using System;
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

		// The Vulkan object containing this render pass
		internal readonly Vk.RenderPass VkRenderPass;

		private bool _isDisposed = false;
		#endregion // Fields

		internal RenderPass(string name, Vk.RenderPass pass)
		{
			Name = name;
			VkRenderPass = pass;

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
			}
			LINFO($"Disposed render pass '{Name}'.");
			_isDisposed = true;
		}
		#endregion // IDisposable
	}
}
