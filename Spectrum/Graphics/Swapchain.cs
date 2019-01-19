using System;
using Vk = VulkanCore;
using VkExt = VulkanCore.Ext;
using VkKhr = VulkanCore.Khr;
using static Spectrum.InternalLog;

namespace Spectrum.Graphics
{
	// Manages all of the objects and synchronization required to operate a Vulkan swapchain. Additionally manages the
	// surface object.
	internal sealed class Swapchain : IDisposable
	{
		#region Fields
		public readonly GraphicsDevice Device;
		private readonly Vk.Instance _vkInstance;
		private readonly Vk.Device _vkDevice;
		private Vk.Queue _presentQueue => Device.Queues.Graphics;

		// The window presentation surface
		internal readonly VkKhr.SurfaceKhr Surface;

		private bool _isDisposed = false;
		#endregion // Fields

		public Swapchain(GraphicsDevice gdevice, Vk.Instance instance, Vk.Device device)
		{
			Device = gdevice;
			_vkInstance = instance;
			_vkDevice = device;

			// Create the surface
			long surfHandle = Glfw.CreateWindowSurface(instance, gdevice.Application.Window.Handle);
			Vk.AllocationCallbacks? acb = null;
			Surface = new VkKhr.SurfaceKhr(instance, ref acb, surfHandle);
			LINFO("Created Vulkan presentation surface.");
		}
		~Swapchain()
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
				Surface.Dispose();
				LINFO("Destroyed Vulkan presentation surface.");
			}

			_isDisposed = true;
		}
		#endregion // IDisposable
	}
}
