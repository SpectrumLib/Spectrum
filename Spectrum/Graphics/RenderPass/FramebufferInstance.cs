using System;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	// Holds the vulkan framebuffer object used by one or more render passes, and uses reference counting to stay alive
	//   while any render passes are still referencing it. Also implements functionality to track when the public
	//   Framebuffer objects are disposed before they should be, when render passes are still referencing them.
	internal class FramebufferInstance
	{
		#region Fields
		public readonly Vk.Framebuffer VkFramebuffer;
		public readonly Framebuffer[] Sources;

		private readonly object _countLock = new object();
		private uint _refCount = 0;
		#endregion // Fields

		// The vulkan framebuffer objects, and the source framebuffers containing the referenced attachments
		public FramebufferInstance(Vk.Framebuffer fb, Framebuffer[] sources)
		{
			VkFramebuffer = fb;
			Sources = sources;
		}

		// Increments the internal reference count and the ref counts for all sources
		public void IncRefCount()
		{
			lock (_countLock)
			{
				_refCount += 1;

				foreach (var src in Sources)
					src.IncRefCount();
			}
		}

		// Increments the internal reference count and the ref counts for all sources
		//   Also destroys the object when the reference count is zero
		public void DecRefCount()
		{
			lock (_countLock)
			{
				_refCount -= 1;

				foreach (var src in Sources)
					src.DecRefCount();

				if (_refCount == 0)
					destroy();
			}
		}

		// Destroys the vulkan object
		private void destroy()
		{
			VkFramebuffer?.Dispose();
		}
	}
}
