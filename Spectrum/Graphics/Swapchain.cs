/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using Vk = SharpVk;
using VkKhr = SharpVk.Khronos;
using static Spectrum.InternalLog;
using System.Linq;
using System.Diagnostics;

namespace Spectrum.Graphics
{
	// Manages the display swapchain
	internal sealed class Swapchain : IDisposable
	{
		private static readonly VkKhr.SurfaceFormat[] PREFERRED_FORMATS = {
			new VkKhr.SurfaceFormat { Format = Vk.Format.B8G8R8A8UNorm, ColorSpace = VkKhr.ColorSpace.SrgbNonlinear },
			new VkKhr.SurfaceFormat { Format = Vk.Format.R8G8B8A8UNorm, ColorSpace = VkKhr.ColorSpace.SrgbNonlinear }
		};
		private const uint MAX_IMAGE_COUNT = 3;
		private static readonly Vk.ImageSubresourceRange COLOR_SUBRESOURCE =
			new Vk.ImageSubresourceRange(Vk.ImageAspectFlags.Color, 0, 1, 0, 1);

		#region Fields
		// Global Vulkan objects
		private readonly GraphicsDevice _device;
		private Vk.Instance _vkInstance => _device.VkInstance;
		private Vk.PhysicalDevice _vkPhysicalDevice => _device.VkPhysicalDevice;
		private Vk.Device _vkDevice => _device.VkDevice;

		// Vulkan surface objects
		public readonly VkKhr.Surface Surface;
		private readonly VkKhr.SurfaceFormat _surfaceFormat;
		private readonly VkKhr.PresentMode _presentMode;

		// Blitting objects
		private Vk.CommandPool _commandPool;
		private Vk.CommandBuffer _commandBuffer;
		private Vk.Fence _blitFence;
		private Vk.ImageMemoryBarrier _rtTransferBarrier;
		private Vk.ImageMemoryBarrier _rtAttachBarrier;

		// Swapchain objects
		private VkKhr.Swapchain _swapChain;
		private SwapchainImage[] _swapChainImages;
		private SyncObjects _syncObjects;
		public bool Dirty { get; private set; } = false;
		public Extent Extent { get; private set; } = Extent.Zero;

		private bool _isDisposed = false;
		#endregion // Fields

		public Swapchain(GraphicsDevice device)
		{
			_device = device;

			// Create the window surface
			var sHandle = Core.Instance.Window.Glfw.CreateWindowSurface(_vkInstance, Core.Instance.Window.Handle);
			if (sHandle == 0)
			{
				IERROR("Failed to create window surface.");
				throw new PlatformNotSupportedException("Failed to create window surface.");
			}
			Surface = VkKhr.Surface.CreateFromHandle(_vkInstance, (ulong)sHandle);
			if (!VkKhr.PhysicalDeviceExtensions.GetSurfaceSupport(_vkPhysicalDevice, _device.Queues.FamilyIndex, Surface))
			{
				IERROR("Device does not support presentation surfaces.");
				throw new PlatformNotSupportedException("Device does not support presentation surfaces.");
			}
			IINFO("Created Vulkan presentation surface.");

			// Select the surface format and present mode to use
			var sfmts = VkKhr.PhysicalDeviceExtensions.GetSurfaceFormats(_vkPhysicalDevice, Surface);
			var pmodes = VkKhr.PhysicalDeviceExtensions.GetSurfacePresentModes(_vkPhysicalDevice, Surface);
			if (sfmts.Length == 0 || pmodes.Length == 0)
			{
				IERROR("Device does not support presentation operations.");
				throw new PlatformNotSupportedException("Device does not support presentation operations.");
			}
			if (sfmts.Length == 1 && sfmts[0].Format == Vk.Format.Undefined) // We are allowed to pick
				_surfaceFormat = PREFERRED_FORMATS[0];
			else // Try to get a preferred format, just use the first if available
			{
				var idx = Array.FindIndex(sfmts, fmt => PREFERRED_FORMATS.Contains(fmt));
				_surfaceFormat = (idx != -1) ? sfmts[idx] : sfmts[0];
			}
			_presentMode = pmodes.Contains(VkKhr.PresentMode.Mailbox) ? VkKhr.PresentMode.Mailbox :
						   pmodes.Contains(VkKhr.PresentMode.Fifo) ? VkKhr.PresentMode.Fifo :
						   VkKhr.PresentMode.Immediate;

			// Prepare the synchronization objects
			_syncObjects.ImageAvailable = new Vk.Semaphore[MAX_IMAGE_COUNT];
			_syncObjects.BlitComplete = new Vk.Semaphore[MAX_IMAGE_COUNT];
			for (uint i = 0; i < MAX_IMAGE_COUNT; ++i)
			{
				_syncObjects.ImageAvailable[i] = _vkDevice.CreateSemaphore();
				_syncObjects.BlitComplete[i] = _vkDevice.CreateSemaphore();
			}

			// Prepare the blitting objects
			_commandPool = _vkDevice.CreateCommandPool(_device.Queues.FamilyIndex, 
				Vk.CommandPoolCreateFlags.Transient | Vk.CommandPoolCreateFlags.ResetCommandBuffer);
			_commandBuffer = _vkDevice.AllocateCommandBuffer(_commandPool, Vk.CommandBufferLevel.Primary);
			_blitFence = _vkDevice.CreateFence(Vk.FenceCreateFlags.None); // Do NOT start this signaled, needed in rebuildSwapchain()
			_rtTransferBarrier = new Vk.ImageMemoryBarrier {
				Image = null,
				SubresourceRange = COLOR_SUBRESOURCE,
				SourceAccessMask = Vk.AccessFlags.ColorAttachmentWrite,
				DestinationAccessMask = Vk.AccessFlags.TransferRead,
				OldLayout = Vk.ImageLayout.ColorAttachmentOptimal,
				NewLayout = Vk.ImageLayout.TransferSourceOptimal
			};
			_rtAttachBarrier = new Vk.ImageMemoryBarrier {
				Image = null,
				SubresourceRange = COLOR_SUBRESOURCE,
				SourceAccessMask = Vk.AccessFlags.TransferRead,
				DestinationAccessMask = Vk.AccessFlags.ColorAttachmentWrite,
				OldLayout = Vk.ImageLayout.TransferSourceOptimal,
				NewLayout = Vk.ImageLayout.ColorAttachmentOptimal
			};

			// Build the swapchain for the first time
			rebuildSwapchain();
		}
		~Swapchain()
		{
			dispose(false);
		}

		// Marks the swapchain for recreation
		public void MarkDirty() => Dirty = true;

		// Rebuilds the swapchain
		private void rebuildSwapchain()
		{
			Stopwatch timer = Stopwatch.StartNew();
			var scaps = VkKhr.PhysicalDeviceExtensions.GetSurfaceCapabilities(_vkPhysicalDevice, Surface);

			// Get the size and number of images
			uint icnt = Math.Min(scaps.MinImageCount + 1, MAX_IMAGE_COUNT);
			if (scaps.MaxImageCount != 0)
				icnt = Math.Min(icnt, scaps.MaxImageCount);
			if (scaps.CurrentExtent.Width != UInt32.MaxValue) // We must use the given size
				Extent = (Extent)scaps.CurrentExtent;
			else // Match the window size
				Extent = Extent.Clamp(Core.Instance.Window.Size, (Extent)scaps.MinImageExtent, (Extent)scaps.MaxImageExtent);
			_syncObjects.MaxInflightFrames = icnt;

			// Create the swapchain
			var oldsc = _swapChain;
			_swapChain = VkKhr.DeviceExtensions.CreateSwapchain(
				extendedHandle: _vkDevice,
				surface: Surface,
				minImageCount: icnt,
				imageFormat: _surfaceFormat.Format,
				imageColorSpace: _surfaceFormat.ColorSpace,
				imageExtent: (Vk.Extent2D)Extent,
				imageArrayLayers: 1,
				imageUsage: Vk.ImageUsageFlags.ColorAttachment | Vk.ImageUsageFlags.TransferDestination,
				imageSharingMode: Vk.SharingMode.Exclusive,
				queueFamilyIndices: null,
				preTransform: scaps.CurrentTransform,
				compositeAlpha: VkKhr.CompositeAlphaFlags.Opaque,
				presentMode: _presentMode,
				clipped: true,
				oldSwapchain: oldsc
			);

			// Setup new swapchain images
			var imgs = _swapChain.GetImages();
			_swapChainImages = new SwapchainImage[icnt];
			imgs.ForEach((img, idx) => {
				var view = _vkDevice.CreateImageView(
					image: img,
					viewType: Vk.ImageViewType.ImageView2d,
					format: _surfaceFormat.Format,
					components: Vk.ComponentMapping.Identity,
					subresourceRange: COLOR_SUBRESOURCE,
					flags: Vk.ImageViewCreateFlags.None
				);
				_swapChainImages[idx] = new SwapchainImage {
					Image = img,
					View = view,
					TransferBarrier = new Vk.ImageMemoryBarrier {
						Image = img,
						SubresourceRange = COLOR_SUBRESOURCE,
						SourceAccessMask = Vk.AccessFlags.None,
						DestinationAccessMask = Vk.AccessFlags.TransferWrite,
						OldLayout = Vk.ImageLayout.Undefined,
						NewLayout = Vk.ImageLayout.TransferDestinationOptimal,
						SourceQueueFamilyIndex = Vk.Constants.QueueFamilyIgnored,
						DestinationQueueFamilyIndex = Vk.Constants.QueueFamilyIgnored
					},
					PresentBarrier = new Vk.ImageMemoryBarrier {
						Image = img,
						SubresourceRange = COLOR_SUBRESOURCE,
						SourceAccessMask = Vk.AccessFlags.MemoryRead,
						DestinationAccessMask = Vk.AccessFlags.None,
						OldLayout = Vk.ImageLayout.TransferDestinationOptimal,
						NewLayout = Vk.ImageLayout.PresentSource,
						SourceQueueFamilyIndex = Vk.Constants.QueueFamilyIgnored,
						DestinationQueueFamilyIndex = Vk.Constants.QueueFamilyIgnored
					}
				};
			});

			// Dispose the old swapchain once the device is done with it
			_device.Queues.Graphics.WaitIdle();
			oldsc?.Dispose();

			// Perform initial layout transitions to present mode
			_commandBuffer.Begin(Vk.CommandBufferUsageFlags.OneTimeSubmit);
			_commandBuffer.PipelineBarrier(
				sourceStageMask: Vk.PipelineStageFlags.TopOfPipe,
				destinationStageMask: Vk.PipelineStageFlags.Transfer,
				memoryBarriers: null,
				bufferMemoryBarriers: null,
				imageMemoryBarriers: _swapChainImages.Select(img => new Vk.ImageMemoryBarrier {
					Image = img.Image,
					SubresourceRange = COLOR_SUBRESOURCE,
					SourceAccessMask = Vk.AccessFlags.None,
					DestinationAccessMask = Vk.AccessFlags.MemoryRead,
					OldLayout = Vk.ImageLayout.Undefined,
					NewLayout = Vk.ImageLayout.PresentSource,
					SourceQueueFamilyIndex = Vk.Constants.QueueFamilyIgnored,
					DestinationQueueFamilyIndex = Vk.Constants.QueueFamilyIgnored
				}).ToArray()
			);
			_commandBuffer.End();
			_blitFence.Reset();
			_device.Queues.Graphics.Submit(
				submits: new [] { new Vk.SubmitInfo {
					CommandBuffers = new [] { _commandBuffer }
				}},
				_blitFence
			);
			_blitFence.Wait(UInt64.MaxValue); // Do not reset

			Dirty = false;
			IINFO($"Swapchain rebuilt @ {Extent} - F={_surfaceFormat.Format}:{_surfaceFormat.ColorSpace} C={_swapChainImages.Length}" +
				$" in {timer.Elapsed.TotalMilliseconds:.00} ms.");
		}

		private void cleanSwapchain()
		{
			_swapChainImages?.ForEach(img => img.View.Dispose());
		}

		#region Frame Functions
		public void BeginFrame()
		{
			// Wait until the last blit is done, so we know we have write access to the render target
			_blitFence.Wait(UInt64.MaxValue);
			_blitFence.Reset();

			// Rebuild step
			try_rebuild:
			if (Dirty)
			{
				cleanSwapchain();
				rebuildSwapchain();
			}

			// Try to get the next image, rebuild on out-of-date or suboptimal
			try
			{
				_syncObjects.CurrentImage =
					_swapChain.AcquireNextImage(UInt64.MaxValue, _syncObjects.CurrentImageAvailable, null);
			}
			catch (Vk.SharpVkException e)
				when (e.ResultCode == Vk.Result.ErrorOutOfDate || e.ResultCode == Vk.Result.Suboptimal)
			{
				Dirty = true;
				goto try_rebuild;
			}
		}

		public void EndFrame()
		{
			displayRenderTarget();

			try
			{
				VkKhr.QueueExtensions.Present(
					extendedHandle: _device.Queues.Graphics,
					waitSemaphores: new [] { _syncObjects.CurrentBlitComplete },
					swapchains: new [] { _swapChain },
					imageIndices: new [] { _syncObjects.CurrentImage }
				);
			}
			catch (Vk.SharpVkException e)
				when (e.ResultCode == Vk.Result.ErrorOutOfDate || e.ResultCode == Vk.Result.Suboptimal)
			{
				Dirty = true;
			}

			_syncObjects.MoveNext();
		}

		private void displayRenderTarget()
		{
			ref var cimg = ref _swapChainImages[_syncObjects.CurrentImage];
			_commandBuffer.Begin(Vk.CommandBufferUsageFlags.OneTimeSubmit);

			if (false)
			{

			}
			else // No render target, just clear the screen to black
			{
				// Swap image: undefined -> transfer dst
				_commandBuffer.PipelineBarrier(
					sourceStageMask: Vk.PipelineStageFlags.TopOfPipe,
					destinationStageMask: Vk.PipelineStageFlags.Transfer,
					memoryBarriers: null,
					bufferMemoryBarriers: null,
					imageMemoryBarriers: new [] { cimg.TransferBarrier }
				);

				// Clear to black
				_commandBuffer.ClearColorImage(
					image: cimg.Image,
					imageLayout: Vk.ImageLayout.TransferDestinationOptimal,
					color: new Vk.ClearColorValue(0.0f, 0.0f, 0.0f, 1.0f),
					ranges: new [] { COLOR_SUBRESOURCE }
				);

				// Swap image: transfer dst -> present src
				_commandBuffer.PipelineBarrier(
					sourceStageMask: Vk.PipelineStageFlags.Transfer,
					destinationStageMask: Vk.PipelineStageFlags.BottomOfPipe,
					memoryBarriers: null,
					bufferMemoryBarriers: null,
					imageMemoryBarriers: new [] { cimg.PresentBarrier }
				);
			}

			_commandBuffer.End();
			_blitFence.Reset();
			_device.Queues.Graphics.Submit(
				submits: new [] { new Vk.SubmitInfo {
					CommandBuffers = new [] { _commandBuffer },
					SignalSemaphores = new [] { _syncObjects.CurrentBlitComplete },
					WaitDestinationStageMask = new [] { Vk.PipelineStageFlags.Transfer },
					WaitSemaphores = new [] { _syncObjects.CurrentImageAvailable }
				}},
				_blitFence
			);
		}
		#endregion // Frame Functions

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
				_vkDevice.WaitIdle();

				// Clean the sync objects
				_syncObjects.ImageAvailable.ForEach(s => s.Dispose());
				_syncObjects.BlitComplete.ForEach(s => s.Dispose());

				// Clean the blit objects
				_blitFence.Dispose();
				_commandPool.Dispose();

				cleanSwapchain();
				_swapChain?.Dispose();
				IINFO("Destroyed Vulkan swapchain.");

				Surface.Dispose();
				IINFO("Destroyed Vulkan presentation surface.");
			}
			_isDisposed = true;
		}
		#endregion // IDisposable

		#region Swapchain Objects
		private struct SwapchainImage
		{
			public Vk.Image Image;
			public Vk.ImageView View;
			public Vk.ImageMemoryBarrier TransferBarrier;
			public Vk.ImageMemoryBarrier PresentBarrier;
		}

		private struct SyncObjects
		{
			// The maximum number of frames available for queueing before waiting
			public uint MaxInflightFrames;
			// The index if the current in-flight synchronization primitives to use
			public uint SyncIndex;
			// The index of the image currently acquired for this frame
			public uint CurrentImage;
			// Semaphores for coordinating when an image is available
			public Vk.Semaphore[] ImageAvailable;
			// Semaphores for coordinating when an image is done blitting to the swapchain
			public Vk.Semaphore[] BlitComplete;

			public Vk.Semaphore CurrentImageAvailable => ImageAvailable[SyncIndex];
			public Vk.Semaphore CurrentBlitComplete => BlitComplete[SyncIndex];

			public void MoveNext() => SyncIndex = (SyncIndex + 1) % MaxInflightFrames;
		}
		#endregion // Swapchain Objects
	}
}
