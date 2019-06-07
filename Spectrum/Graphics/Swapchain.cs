using System;
using System.Linq;
using Vk = VulkanCore;
using VkExt = VulkanCore.Ext;
using VkKhr = VulkanCore.Khr;
using static Spectrum.InternalLog;
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
		// "Infinite" timeout period
		private const long INFINITE_TIMEOUT = -1;
		// The maximum number of "in-flight" frames waiting to be rendered, past this we wait for them to be finished
		// Note that this is the global maximum, and the actual number may be lower depending on the presentation engine capabilities
		private const uint MAX_INFLIGHT_FRAMES = 3;

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
		// Objects used to synchronize rendering
		private SyncObjects _syncObjects;

		// Current chosen swapchain parameters
		private VkKhr.SurfaceFormatKhr _surfaceFormat;
		private VkKhr.PresentModeKhr _presentMode;
		// The current extent of the swapchain images
		public Point Extent { get; private set; } = Point.Zero;

		// Used to mark the swapchain for re-creation
		public bool Dirty { get; private set; } = false;

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
			if (VkKhr.PhysicalDeviceExtensions.GetSurfaceSupportKhr(pDevice, gdevice.Queues.Graphics.FamilyIndex, Surface) == Vk.Constant.False)
			{
				LFATAL($"The physical device '{gdevice.Info.Name}' does not support surface presentation.");
				throw new PlatformNotSupportedException("Physical device does not support surface presentation.");
			}
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

			// Prepare the synchronization objects
			_syncObjects.ImageAvailable = new Vk.Semaphore[MAX_INFLIGHT_FRAMES];
			_syncObjects.RenderComplete = new Vk.Semaphore[MAX_INFLIGHT_FRAMES];
			_syncObjects.Processed = new Vk.Fence[MAX_INFLIGHT_FRAMES];
			for (int i = 0; i < MAX_INFLIGHT_FRAMES; ++i)
			{
				_syncObjects.ImageAvailable[i] = device.CreateSemaphore();
				_syncObjects.RenderComplete[i] = device.CreateSemaphore();
				_syncObjects.Processed[i] = device.CreateFence(new Vk.FenceCreateInfo(Vk.FenceCreateFlags.Signaled));
			}

			// Build the swapchain
			rebuildSwapchain();
		}
		~Swapchain()
		{
			dispose(false);
		}

		// Called externally to force the swapchain to rebuild before starting the next render frame
		public void MarkForRebuild() => Dirty = true;

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
			_syncObjects.MaxInflightFrames = (uint)Math.Min(imCount, MAX_INFLIGHT_FRAMES);

			// Create the swapchain
			var oldSwapChain = _swapChain;
			VkKhr.SwapchainCreateInfoKhr cInfo = new VkKhr.SwapchainCreateInfoKhr(
				Surface, 
				_surfaceFormat.Format, 
				Extent, 
				minImageCount: imCount,
				imageColorSpace: _surfaceFormat.ColorSpace,
				presentMode: _presentMode,
				oldSwapchain: oldSwapChain
			);
			_swapChain = VkKhr.DeviceExtensions.CreateSwapchainKhr(_vkDevice, cInfo);

			// Destroy the old swapchain
			oldSwapChain?.Dispose();

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

			LDEBUG($"Presentation swapchain rebuilt @ {Extent} " +
				$"(F:{_surfaceFormat.Format}:{_surfaceFormat.ColorSpace==VkKhr.ColorSpaceKhr.SRgbNonlinear} I:{_swapChainImages.Length}:{_syncObjects.MaxInflightFrames}).");
			Dirty = false;
		}

		private void cleanSwapchain()
		{
			// Cleanup the existing image views
			_swapChainImages?.ForEach(img => img.View.Dispose());
		}

		// Acquires the next image to render to, and recreates the swapchain if needed
		public void BeginFrame()
		{
			// Wait for the oldest inflight frame to be done
			_syncObjects.CurrentProcessed.Wait(INFINITE_TIMEOUT);
			//_syncObjects.CurrentProcessed.Reset(); // TODO: BRING THIS BACK ONCE THE FENCE IS SIGNALLED BY RENDER CODE, RIGHT NOW THIS HANGS THE PROGRAM

			// Rebuild if needed
		try_rebuild:
			if (Dirty)
			{
				cleanSwapchain();
				rebuildSwapchain();
			}
			
			// Try to get the next image, with a rebuild and second attempt to acquire on failure
			try
			{
				_syncObjects.CurrentImage = _swapChain.AcquireNextImage(INFINITE_TIMEOUT, _syncObjects.CurrentImageAvailable, null);
			}
			catch (Vk.VulkanException e) 
				when (e.Result == Vk.Result.ErrorOutOfDateKhr || e.Result == Vk.Result.SuboptimalKhr)
			{
				Dirty = true;
				goto try_rebuild;
			}
			catch { throw; }
		}

		// Submits the currently aquired image to be presented
		public void EndFrame()
		{
			// Present and check for dirty swapchain
			try
			{
				VkKhr.QueueExtensions.PresentKhr(_presentQueue, _syncObjects.CurrentRenderComplete, _swapChain, _syncObjects.CurrentImage);
			}
			catch (Vk.VulkanException e)
				when (e.Result == Vk.Result.ErrorOutOfDateKhr || e.Result == Vk.Result.SuboptimalKhr)
			{
				Dirty = true;
			}
			catch { throw; }

			_syncObjects.MoveNext();
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
				// We need to wait for the final in flight frames to finish
				_vkDevice.WaitIdle();

				// Clean the sync objects
				_syncObjects.ImageAvailable.ForEach(s => s.Dispose());
				_syncObjects.RenderComplete.ForEach(s => s.Dispose());
				_syncObjects.Processed.ForEach(f => f.Dispose());

				cleanSwapchain();
				_swapChain?.Dispose();
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

		// Objects used to synchronize rendering
		private struct SyncObjects
		{
			// The maximum number of frames available for queueing before waiting
			public uint MaxInflightFrames;
			// The index if the current in-flight synchronization primitives to use
			public uint SyncIndex;
			// The index of the image currently acquired for this frame
			public int CurrentImage;
			// Semaphores for coordinating when an image is available
			public Vk.Semaphore[] ImageAvailable;
			// Semaphores for coordinating when frames are finished rendering
			public Vk.Semaphore[] RenderComplete;
			// Fence for synching application to presentation image
			public Vk.Fence[] Processed;

			public Vk.Semaphore CurrentImageAvailable => ImageAvailable[SyncIndex];
			public Vk.Semaphore CurrentRenderComplete => RenderComplete[SyncIndex];
			public Vk.Fence CurrentProcessed => Processed[SyncIndex];

			public void MoveNext() => SyncIndex = (SyncIndex + 1) % MaxInflightFrames;
		}
	}
}
