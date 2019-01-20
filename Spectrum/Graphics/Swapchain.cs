using System;
using Vk = VulkanCore;
using VkExt = VulkanCore.Ext;
using VkKhr = VulkanCore.Khr;
using static Spectrum.InternalLog;
using System.Linq;
using Spectrum.Utilities;

namespace Spectrum.Graphics
{
	// Manages all of the objects and synchronization required to operate a Vulkan swapchain. Additionally manages the
	// surface object.
	internal sealed class Swapchain : IDisposable
	{
		// The preferred surface formats for the swapchain, in order
		private static readonly VkKhr.SurfaceFormatKhr[] PREFERRED_SURFACE_FORMATS = {
			new VkKhr.SurfaceFormatKhr { Format = Vk.Format.B8G8R8A8UNorm, ColorSpace = VkKhr.ColorSpaceKhr.SRgbNonlinear },
			new VkKhr.SurfaceFormatKhr { Format = Vk.Format.R8G8B8A8UNorm, ColorSpace = VkKhr.ColorSpaceKhr.SRgbNonlinear }
		};
		// The default subresource range for swapchain image views
		private static readonly Vk.ImageSubresourceRange DEFAULT_SUBRESOURCE_RANGE = new Vk.ImageSubresourceRange(
			Vk.ImageAspects.Color, 0, 1, 0, 1
		);

		#region Fields
		public readonly GraphicsDevice Device;
		private readonly AppWindow _window;
		private readonly Vk.Instance _vkInstance;
		private readonly Vk.PhysicalDevice _vkPhysicalDevice;
		private readonly Vk.Device _vkDevice;
		private Vk.Queue _presentQueue => Device.Queues.Graphics;

		// The window presentation surface
		internal readonly VkKhr.SurfaceKhr Surface;

		// The current swapchain
		private VkKhr.SwapchainKhr _swapChain;
		// The current swapchain images
		private SwapchainImage[] _swapChainImages;

		// Current chosen swapchain parameters
		private VkKhr.SurfaceFormatKhr _surfaceFormat;
		private VkKhr.PresentModeKhr _presentMode;
		// The current extent of the swapchain images
		public Point Extent { get; private set; } = Point.Zero;

		private bool _isDisposed = false;
		#endregion // Fields

		public Swapchain(GraphicsDevice gdevice, Vk.Instance instance, Vk.PhysicalDevice pDevice, Vk.Device device)
		{
			Device = gdevice;
			_window = gdevice.Application.Window;
			_vkInstance = instance;
			_vkPhysicalDevice = pDevice;
			_vkDevice = device;

			// Create the surface
			long surfHandle = Glfw.CreateWindowSurface(instance, _window.Handle);
			Vk.AllocationCallbacks? acb = null;
			Surface = new VkKhr.SurfaceKhr(instance, ref acb, surfHandle);
			LINFO("Created Vulkan presentation surface.");

			// Check the surface for swapchain support levels
			var sFmts = VkKhr.PhysicalDeviceExtensions.GetSurfaceFormatsKhr(pDevice, Surface);
			var pModes = VkKhr.PhysicalDeviceExtensions.GetSurfacePresentModesKhr(pDevice, Surface);
			if (sFmts.Length == 0)
				throw new PlatformNotSupportedException("The chosen device does not support any presentation formats.");
			if (pModes.Length == 0)
				throw new PlatformNotSupportedException("The chosen device does not support any presentation modes.");

			// Choose the best available surface format
			if (sFmts.Length == 1 && sFmts[0].Format == Vk.Format.Undefined) // We are allowed to pick!
				_surfaceFormat = PREFERRED_SURFACE_FORMATS[0];
			else // Check if one of the preferred formats is available, otherwise just use the first format given
			{
				var sfmt = PREFERRED_SURFACE_FORMATS.FirstOrDefault(fmt => sFmts.Contains(fmt));
				if (sfmt.Format == 0 && sfmt.ColorSpace == 0)
					_surfaceFormat = sFmts[0];
				else
					_surfaceFormat = sfmt;
			}

			// Choose the presentation mode (prefer mailbox -> fifo -> imm)
			_presentMode =
				pModes.Contains(VkKhr.PresentModeKhr.Mailbox) ? VkKhr.PresentModeKhr.Mailbox :
				pModes.Contains(VkKhr.PresentModeKhr.Fifo) ? VkKhr.PresentModeKhr.Fifo :
				VkKhr.PresentModeKhr.Immediate;

			// Build the swapchain
			rebuildSwapchain();
		}
		~Swapchain()
		{
			dispose(false);
		}

		private void rebuildSwapchain()
		{
			var sCaps = VkKhr.PhysicalDeviceExtensions.GetSurfaceCapabilitiesKhr(_vkPhysicalDevice, Surface);

			// Calculate the size of the images
			if (sCaps.CurrentExtent.Width != Int32.MaxValue) // We have to use the given size
				Extent = sCaps.CurrentExtent;
			else // We can choose an extent, but we will just make it the size of the window
				Extent = Point.Max(sCaps.MinImageExtent, Point.Min(sCaps.MaxImageExtent, _window.Size));

			// Calculate the number of images
			int imCount = sCaps.MinImageCount + 1;
			if (sCaps.MaxImageCount != 0)
				imCount = Math.Min(imCount, sCaps.MaxImageCount);

			// Create the swapchain
			VkKhr.SwapchainCreateInfoKhr cInfo = new VkKhr.SwapchainCreateInfoKhr(
				Surface, 
				_surfaceFormat.Format, 
				Extent, 
				minImageCount: imCount,
				imageColorSpace: _surfaceFormat.ColorSpace,
				presentMode: _presentMode,
				oldSwapchain: null
			);
			_swapChain = VkKhr.DeviceExtensions.CreateSwapchainKhr(_vkDevice, cInfo);

			// Get the new swapchain images
			var imgs = _swapChain.GetImages();
			_swapChainImages = new SwapchainImage[imgs.Length];
			imgs.ForEach((img, idx) => {
				Vk.ImageViewCreateInfo vInfo = new Vk.ImageViewCreateInfo(
					_surfaceFormat.Format,
					DEFAULT_SUBRESOURCE_RANGE,
					viewType: Vk.ImageViewType.Image2D,
					components: default
				);
				_swapChainImages[idx] = new SwapchainImage { Image = img, View = img.CreateView(vInfo) };
			});

			LINFO($"Presentation swapchain rebuilt @ {Extent} (F:{_surfaceFormat.Format}:{_surfaceFormat.ColorSpace==VkKhr.ColorSpaceKhr.SRgbNonlinear} I:{_swapChainImages.Length}).");
		}

		private void cleanSwapchain()
		{
			// Cleanup the existing image views
			_swapChainImages?.ForEach(img => img.View.Dispose());

			// Destroy existing images
			_swapChain?.Dispose();
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
				cleanSwapchain();
				LINFO("Destroyed Vulkan swapchain.");

				Surface.Dispose();
				LINFO("Destroyed Vulkan presentation surface.");
			}

			_isDisposed = true;
		}
		#endregion // IDisposable

		// Small struct for holding swapchain image objects
		private struct SwapchainImage
		{
			public Vk.Image Image;
			public Vk.ImageView View;
		}
	}
}
