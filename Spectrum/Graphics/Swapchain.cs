/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using Vk = SharpVk;
using VkExt = SharpVk.Multivendor;
using VkKhr = SharpVk.Khronos;
using static Spectrum.InternalLog;
using System.Linq;

namespace Spectrum.Graphics
{
	// Manages the objects, commands, and synchronization of the Vulkan display swapchain
	internal sealed class Swapchain : IDisposable
	{
		// The preferred surface formats for the swapchain, in order
		private static readonly VkKhr.SurfaceFormat[] PREFERRED_SURFACE_FORMATS = {
			new VkKhr.SurfaceFormat { Format = Vk.Format.B8G8R8A8UNorm, ColorSpace = VkKhr.ColorSpace.SrgbNonlinear },
			new VkKhr.SurfaceFormat { Format = Vk.Format.R8G8B8A8UNorm, ColorSpace = VkKhr.ColorSpace.SrgbNonlinear }
		};
		// The default subresource range for swapchain image views
		private static readonly Vk.ImageSubresourceRange DEFAULT_SUBRESOURCE_RANGE = new Vk.ImageSubresourceRange(
			Vk.ImageAspectFlags.Color, 0, 1, 0, 1
		);
		// One time submit flags
		private static readonly Vk.CommandBufferUsageFlags ONE_TIME_SUBMIT = Vk.CommandBufferUsageFlags.OneTimeSubmit;
		// Blitting constants
		private static readonly Vk.Offset3D BLIT_ZERO = new Vk.Offset3D(0, 0, 0);
		private static readonly Vk.ImageSubresourceLayers BLIT_SUBRESOURCE = new Vk.ImageSubresourceLayers(Vk.ImageAspectFlags.Color, 0, 0, 1);
		// "Infinite" timeout period
		private const long INFINITE_TIMEOUT = -1;
		// The maximum number of "in-flight" frames waiting to be rendered, past this we wait for them to be finished
		// Note that this is the global maximum, and the actual number may be lower depending on the presentation engine capabilities
		private const uint MAX_INFLIGHT_FRAMES = 3;

		#region Fields
		public readonly GraphicsDevice Device;
		private readonly CoreWindow _window;
		private readonly Vk.Instance _vkInstance;
		private readonly Vk.PhysicalDevice _vkPhysicalDevice;
		private readonly Vk.Device _vkDevice;
		private Vk.Queue _presentQueue => Device.Queues.Graphics;

		// The current swapchain
		private VkKhr.Swapchain _swapChain;
		// The current swapchain images
		private SwapchainImage[] _swapChainImages;
		// Objects used to synchronize rendering
		private SyncObjects _syncObjects;

		// Blitting objects
		private Vk.CommandPool _commandPool;
		private Vk.CommandBuffer _commandBuffer;
		private Vk.Fence _blitFence;
		private Vk.ImageMemoryBarrier _rtTransferBarrier;
		private Vk.ImageMemoryBarrier _rtAttachBarrier;

		// Current chosen swapchain parameters
		private VkKhr.SurfaceFormat _surfaceFormat;
		private VkKhr.PresentMode _presentMode;

		// The current extent of the swapchain images
		public Extent Extent { get; private set; } = Extent.Zero;

		// The window presentation surface
		public readonly VkKhr.Surface Surface;

		// Used to mark the swapchain for re-creation
		public bool Dirty { get; private set; } = false;

		private bool _isDisposed = false;
		#endregion // Fields

		public Swapchain(GraphicsDevice gdevice, Vk.Instance instance, Vk.PhysicalDevice pDevice, Vk.Device device)
		{
			Device = gdevice;
			_window = Core.Instance.Window;
			_vkInstance = instance;
			_vkPhysicalDevice = pDevice;
			_vkDevice = device;

			// Create the surface
			long surfHandle = _window.Glfw.CreateWindowSurface(instance, _window.Handle);
			if (surfHandle == 0)
			{
				IERROR("Could not create Vulkan surface.");
				throw new PlatformNotSupportedException("Could not create Vulkan surface.");
			}
			Surface = VkKhr.Surface.CreateFromHandle(instance, (ulong)surfHandle);
			if (!VkKhr.PhysicalDeviceExtensions.GetSurfaceSupport(pDevice, gdevice.Queues.FamilyIndex, Surface))
			{
				IERROR($"Device '{gdevice.Info.Name}' does not support surfaces.");
				throw new PlatformNotSupportedException("Device does not support surfaces.");
			}
			IINFO("Vulkan surface created.");

			// Check the surface for swapchain support levels
			var sFmts = VkKhr.PhysicalDeviceExtensions.GetSurfaceFormats(pDevice, Surface);
			var pModes = VkKhr.PhysicalDeviceExtensions.GetSurfacePresentModes(pDevice, Surface);
			if (sFmts.Length == 0)
				throw new PlatformNotSupportedException("Device does not support any presentation formats.");
			if (pModes.Length == 0)
				throw new PlatformNotSupportedException("Device does not support any presentation modes.");

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
				pModes.Contains(VkKhr.PresentMode.Mailbox) ? VkKhr.PresentMode.Mailbox :
				pModes.Contains(VkKhr.PresentMode.Fifo) ? VkKhr.PresentMode.Fifo :
				VkKhr.PresentMode.Immediate;

			// Prepare the synchronization objects
			_syncObjects.ImageAvailable = new Vk.Semaphore[MAX_INFLIGHT_FRAMES];
			_syncObjects.BlitComplete = new Vk.Semaphore[MAX_INFLIGHT_FRAMES];
			for (int i = 0; i < MAX_INFLIGHT_FRAMES; ++i)
			{
				_syncObjects.ImageAvailable[i] = device.CreateSemaphore();
				_syncObjects.BlitComplete[i] = device.CreateSemaphore();
			}

			// Setup the command buffers
			_commandPool = device.CreateCommandPool(Device.Queues.FamilyIndex, Vk.CommandPoolCreateFlags.Transient | Vk.CommandPoolCreateFlags.ResetCommandBuffer);
			_commandBuffer = device.AllocateCommandBuffer(_commandPool, Vk.CommandBufferLevel.Primary);
			_blitFence = device.CreateFence(Vk.FenceCreateFlags.None); // Do NOT start this signaled, as it is needed in rebuildSwapchain() below
			_rtTransferBarrier = new Vk.ImageMemoryBarrier
			{
				Image = null,
				SubresourceRange = new Vk.ImageSubresourceRange(Vk.ImageAspectFlags.Color, 0, 1, 0, 1),
				SourceAccessMask = Vk.AccessFlags.ColorAttachmentWrite,
				DestinationAccessMask = Vk.AccessFlags.TransferRead,
				OldLayout = Vk.ImageLayout.ColorAttachmentOptimal,
				NewLayout = Vk.ImageLayout.TransferSourceOptimal
			};
			_rtAttachBarrier = new Vk.ImageMemoryBarrier
			{
				Image = null,
				SubresourceRange = new Vk.ImageSubresourceRange(Vk.ImageAspectFlags.Color, 0, 1, 0, 1),
				SourceAccessMask = Vk.AccessFlags.TransferRead,
				DestinationAccessMask = Vk.AccessFlags.ColorAttachmentWrite,
				OldLayout = Vk.ImageLayout.TransferSourceOptimal,
				NewLayout = Vk.ImageLayout.ColorAttachmentOptimal
			};

			// Build the swapchain
			rebuildSwapchain();
		}
		~Swapchain()
		{
			dispose(false);
		}

		#region Swapchain Build/Clean
		// Called externally to force the swapchain to rebuild before starting the next render frame
		public void MarkForRebuild() => Dirty = true;

		private void rebuildSwapchain()
		{
			var sCaps = VkKhr.PhysicalDeviceExtensions.GetSurfaceCapabilities(_vkPhysicalDevice, Surface);

			// Calculate the size of the images
			if (sCaps.CurrentExtent.Width != Int32.MaxValue) // We have to use the given size
				Extent = (Extent)sCaps.CurrentExtent;
			else // We can choose an extent, but we will just make it the size of the window
				Extent = Extent.Clamp(_window.Size, (Extent)sCaps.MinImageExtent, (Extent)sCaps.MaxImageExtent);

			// Calculate the number of images
			uint imCount = sCaps.MinImageCount + 1;
			if (sCaps.MaxImageCount != 0)
				imCount = Math.Min(imCount, sCaps.MaxImageCount);
			_syncObjects.MaxInflightFrames = Math.Min(imCount, MAX_INFLIGHT_FRAMES);

			// Create the swapchain
			var oldSwapchain = _swapChain;
			_swapChain = VkKhr.DeviceExtensions.CreateSwapchain(
				extendedHandle: _vkDevice,
				surface: Surface,
				minImageCount: imCount,
				imageFormat: _surfaceFormat.Format,
				imageColorSpace: _surfaceFormat.ColorSpace,
				imageExtent: (Vk.Extent2D)Extent,
				imageArrayLayers: 1,
				imageUsage: Vk.ImageUsageFlags.ColorAttachment | Vk.ImageUsageFlags.TransferDestination,
				imageSharingMode: Vk.SharingMode.Exclusive,
				queueFamilyIndices: null,
				preTransform: sCaps.CurrentTransform,
				compositeAlpha: VkKhr.CompositeAlphaFlags.Opaque,
				presentMode: _presentMode,
				clipped: true,
				oldSwapchain: oldSwapchain
			);

			// Destroy the old swapchain
			oldSwapchain?.Dispose();

			// Get the new swapchain images
			var imgs = _swapChain.GetImages();
			_swapChainImages = new SwapchainImage[imgs.Length];
			imgs.ForEach((img, idx) => {
				var view = _vkDevice.CreateImageView(
					image: img,
					viewType: Vk.ImageViewType.ImageView2d,
					_surfaceFormat.Format,
					components: Vk.ComponentMapping.Identity,
					subresourceRange: DEFAULT_SUBRESOURCE_RANGE,
					flags: Vk.ImageViewCreateFlags.None
				);
				_swapChainImages[idx] = new SwapchainImage
				{
					Image = img,
					View = view,
					TransferBarrier = new Vk.ImageMemoryBarrier
					{
						Image = img,
						SubresourceRange = new Vk.ImageSubresourceRange(Vk.ImageAspectFlags.Color, 0, 1, 0, 1),
						SourceAccessMask = Vk.AccessFlags.ColorAttachmentRead,
						DestinationAccessMask = Vk.AccessFlags.TransferWrite,
						OldLayout = Vk.ImageLayout.PresentSource,
						NewLayout = Vk.ImageLayout.TransferDestinationOptimal
					},
					PresentBarrier = new Vk.ImageMemoryBarrier
					{
						Image = img,
						SubresourceRange = new Vk.ImageSubresourceRange(Vk.ImageAspectFlags.Color, 0, 1, 0, 1),
						SourceAccessMask = Vk.AccessFlags.TransferWrite,
						DestinationAccessMask = Vk.AccessFlags.ColorAttachmentRead,
						OldLayout = Vk.ImageLayout.TransferDestinationOptimal,
						NewLayout = Vk.ImageLayout.PresentSource
					}
				};
			});

			// Perform the initial layout transitions to present mode
			_commandBuffer.Begin(ONE_TIME_SUBMIT);
			var imb = new Vk.ImageMemoryBarrier
			{
				Image = null,
				SubresourceRange = new Vk.ImageSubresourceRange(Vk.ImageAspectFlags.Color, 0, 1, 0, 1),
				SourceAccessMask = Vk.AccessFlags.TransferWrite,
				DestinationAccessMask = Vk.AccessFlags.ColorAttachmentRead,
				OldLayout = Vk.ImageLayout.Undefined,
				NewLayout = Vk.ImageLayout.PresentSource,
				SourceQueueFamilyIndex = Device.Queues.FamilyIndex,
				DestinationQueueFamilyIndex = Device.Queues.FamilyIndex
			};
			_commandBuffer.PipelineBarrier(
				sourceStageMask: Vk.PipelineStageFlags.AllGraphics,
				destinationStageMask: Vk.PipelineStageFlags.AllGraphics,
				memoryBarriers: null,
				bufferMemoryBarriers: null,
				imageMemoryBarriers: _swapChainImages.Select(sci => { imb.Image = sci.Image; return imb; }).ToArray(),
				dependencyFlags: Vk.DependencyFlags.None
			);
			_commandBuffer.End();
			_presentQueue.Submit(new [] { new Vk.SubmitInfo { CommandBuffers = new [] { _commandBuffer } } }, _blitFence);
			_blitFence.Wait(UInt64.MaxValue); // Do not reset

			// Report
			IINFO($"Presentation swapchain rebuilt @ {Extent} " +
				$"(F:{_surfaceFormat.Format}:{_surfaceFormat.ColorSpace} I:{_swapChainImages.Length}:{_syncObjects.MaxInflightFrames}).");
			Dirty = false;
		}

		private void cleanSwapchain()
		{
			// Cleanup the existing image views
			_swapChainImages?.ForEach(img => img.View.Dispose());
		}
		#endregion // Swapchain Build/Clean

		#region Frame Functions
		// Acquires the next image to render to, and recreates the swapchain if needed
		public void BeginFrame()
		{
			// Make sure the blit command from the last frame is finished, so we have access to the source image
			// This is a modified version of the classic Vulkan CPU-GPU swapchain synchronization
			_blitFence.Wait(UInt64.MaxValue);
			_blitFence.Reset();

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
				_syncObjects.CurrentImage = _swapChain.AcquireNextImage(UInt64.MaxValue, _syncObjects.CurrentImageAvailable, null);
			}
			catch (Vk.SharpVkException e)
				when (e.ResultCode == Vk.Result.ErrorOutOfDate || e.ResultCode == Vk.Result.Suboptimal)
			{
				Dirty = true;
				goto try_rebuild;
			}
			catch { throw; }
		}

		// Submits the currently aquired image to be presented
		// The passed image will be copied to the current swapchain image, and must be in transfer src layout
		public void EndFrame(RenderTarget rt)
		{
			// Blit the render output into the swapchain image for presentation
			displayRenderTarget(rt);

			// Present and check for dirty swapchain
			try
			{
				// Do not need to wait on the image to be ready, as there is synchonization in displayRenderTarget()
				VkKhr.QueueExtensions.Present(
					extendedHandle: _presentQueue,
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
			catch { throw; }

			_syncObjects.MoveNext();
		}

		private void displayRenderTarget(RenderTarget rt)
		{
			var cimg = _swapChainImages[_syncObjects.CurrentImage];

			// Begin recording
			_commandBuffer.Begin(ONE_TIME_SUBMIT);

			if (rt != null)
			{
				// Trasition the rt to transfer src, and the sc image to transfer dst
				_rtTransferBarrier.Image = rt.VkImage;
				_commandBuffer.PipelineBarrier(
					sourceStageMask: Vk.PipelineStageFlags.AllGraphics,
					destinationStageMask: Vk.PipelineStageFlags.AllGraphics,
					memoryBarriers: null,
					bufferMemoryBarriers: null,
					imageMemoryBarriers: new [] { _rtTransferBarrier, cimg.TransferBarrier }
				);
				
				if (rt.Size != Extent) // We need to do a filtered blit because of the size mismatch
				{
					_commandBuffer.BlitImage(
						sourceImage: rt.VkImage,
						sourceImageLayout: Vk.ImageLayout.TransferSourceOptimal,
						cimg.Image,
						Vk.ImageLayout.TransferDestinationOptimal,
						new [] { new Vk.ImageBlit 
						{
							SourceOffsets = (BLIT_ZERO, new Vk.Offset3D((int)rt.Size.Width, (int)rt.Size.Height, 1)),
							SourceSubresource = BLIT_SUBRESOURCE,
							DestinationOffsets = (BLIT_ZERO, new Vk.Offset3D((int)Extent.Width, (int)Extent.Height, 1)),
							DestinationSubresource = BLIT_SUBRESOURCE
						}},
						Vk.Filter.Linear
					);
				}
				else // Same size, we can do a much faster direct image copy
				{
					_commandBuffer.CopyImage(
						sourceImage: rt.VkImage,
						sourceImageLayout: Vk.ImageLayout.TransferSourceOptimal,
						destinationImage: cimg.Image,
						destinationImageLayout: Vk.ImageLayout.TransferDestinationOptimal,
						new [] { new Vk.ImageCopy 
						{
							SourceOffset = BLIT_ZERO,
							SourceSubresource = BLIT_SUBRESOURCE,
							DestinationOffset = BLIT_ZERO,
							DestinationSubresource = BLIT_SUBRESOURCE,
							Extent = new Vk.Extent3D(rt.Size.Width, rt.Size.Height, 1)
						}}
					);
				}

				// Transition both images back to their standard layouts
				_rtAttachBarrier.Image = rt.VkImage;
				_commandBuffer.PipelineBarrier(
					sourceStageMask: Vk.PipelineStageFlags.AllGraphics,
					destinationStageMask: Vk.PipelineStageFlags.AllGraphics,
					memoryBarriers: null,
					bufferMemoryBarriers: null,
					imageMemoryBarriers: new [] { _rtAttachBarrier, cimg.PresentBarrier }
				);
			}
			else // No render target, valid possibility if there is no active scene
			{
				// Trasition the sc image to transfer dst
				_commandBuffer.PipelineBarrier(
					sourceStageMask: Vk.PipelineStageFlags.AllGraphics,
					destinationStageMask: Vk.PipelineStageFlags.AllGraphics,
					memoryBarriers: null,
					bufferMemoryBarriers: null,
					imageMemoryBarriers: new [] { cimg.TransferBarrier }
				);

				// Simply clear the swapchain image to black
				var clear = new Vk.ClearColorValue(0, 0, 0, 1);
				_commandBuffer.ClearColorImage(
					cimg.Image,
					Vk.ImageLayout.TransferDestinationOptimal,
					clear,
					new Vk.ImageSubresourceRange(Vk.ImageAspectFlags.Color, 0, 1, 0, 1)
				);

				// Transition the image back to its present layout
				_commandBuffer.PipelineBarrier(
					sourceStageMask: Vk.PipelineStageFlags.AllGraphics,
					destinationStageMask: Vk.PipelineStageFlags.AllGraphics,
					memoryBarriers: null,
					bufferMemoryBarriers: null,
					imageMemoryBarriers: new [] { cimg.PresentBarrier }
				);
			}

			// End the buffer, submit, and wait for the blit to complete
			// This performs GPU-GPU synchonrization by waiting for the swapchain image to be available before blitting
			_commandBuffer.End();
			_blitFence.Reset(); // Can need to happen when the swapchain is invalidated or resized
			_presentQueue.Submit(new [] { new Vk.SubmitInfo 
			{
				CommandBuffers = new [] { _commandBuffer },
				SignalSemaphores = new [] { _syncObjects.CurrentBlitComplete },
				WaitDestinationStageMask = new [] { Vk.PipelineStageFlags.Transfer },
				WaitSemaphores = new [] { _syncObjects.CurrentImageAvailable }
			}}, _blitFence);
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
				// We need to wait for the final in flight frames to finish
				_vkDevice.WaitIdle();

				// Clean the sync objects
				_syncObjects.ImageAvailable.ForEach(s => s.Dispose());
				_syncObjects.BlitComplete.ForEach(s => s.Dispose());

				// Clean the blit objects
				_blitFence.Dispose();
				_commandPool.Dispose();

				cleanSwapchain();
				_swapChain?.Dispose();
				IINFO("Disposed Vulkan Swapchain.");

				Surface.Dispose();
				IINFO("Disposed Vulkan Surface.");
			}

			_isDisposed = true;
		}
		#endregion // IDisposable

		#region Swapchain Objects
		// Small struct for holding swapchain image objects
		private struct SwapchainImage
		{
			public Vk.Image Image;
			public Vk.ImageView View;
			public Vk.ImageMemoryBarrier TransferBarrier;
			public Vk.ImageMemoryBarrier PresentBarrier;
		}

		// Objects used to synchronize rendering
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
