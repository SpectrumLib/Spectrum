using Spectrum.Utilities;
using System;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	// Holds host-visible buffers to use as staging buffers to transfer data to/from the graphics device for images
	//   and buffers.
	internal static class TransferBuffer
	{
		// Default transfer buffer size (16MB) - still need to test if this is a good size
		public const uint DEFAULT_BUFFER_SIZE = 16 * 1024 * 1024;
		// Constant for one-time submit buffers
		private static readonly Vk.CommandBufferBeginInfo ONE_TIME_SUBMIT_INFO = new Vk.CommandBufferBeginInfo(
			Vk.CommandBufferUsages.OneTimeSubmit | Vk.CommandBufferUsages.SimultaneousUse
		);

		#region Fields
		private static GraphicsDevice s_graphicsDevice = null;
		private static Vk.Device s_device => s_graphicsDevice.VkDevice;
		private static Vk.Queue s_queue => s_graphicsDevice.Queues.Transfer;
		private static bool s_separateQueue => s_graphicsDevice.Queues.SeparateTransfer;

		// Vulkan objects for processing transfers
		private static Vk.CommandPool s_transferCommandPool;
		private static Vk.CommandBuffer s_transferCommands;
		private static Vk.Buffer s_transferBuffer;
		private static Vk.DeviceMemory s_transferMemory;
		private static Vk.Fence s_transferFence;
		#endregion // Fields

		// Called from the graphics device when it is initialized
		public static void CreateResources(GraphicsDevice gd)
		{
			s_graphicsDevice = gd;

			// Create the transfer command buffer
			var cpci = new Vk.CommandPoolCreateInfo(s_queue.FamilyIndex, Vk.CommandPoolCreateFlags.Transient);
			s_transferCommandPool = s_device.CreateCommandPool(cpci);
			var cbai = new Vk.CommandBufferAllocateInfo(Vk.CommandBufferLevel.Primary, 1);
			s_transferCommands = s_transferCommandPool.AllocateBuffers(cbai)[0];

			// Create the transfer buffer
			var bci = new Vk.BufferCreateInfo(
				DEFAULT_BUFFER_SIZE,
				Vk.BufferUsages.TransferSrc,
				flags: Vk.BufferCreateFlags.None,
				sharingMode: Vk.SharingMode.Exclusive
			);
			s_transferBuffer = s_device.CreateBuffer(bci);

			// Allocate the buffer memory
			var memReq = s_transferBuffer.GetMemoryRequirements();
			int memIdx = s_graphicsDevice.FindMemoryTypeIndex(memReq.MemoryTypeBits, Vk.MemoryProperties.HostVisible | Vk.MemoryProperties.HostCoherent);
			if (memIdx == -1)
				throw new InvalidOperationException("Cannot find a memory type that supports host buffers (this means bad or out-of-date hardware)");
			var mai = new Vk.MemoryAllocateInfo(memReq.Size, memIdx);
			s_transferMemory = s_device.AllocateMemory(mai);
			s_transferBuffer.BindMemory(s_transferMemory);

			// Create the transfer fence
			var fci = new Vk.FenceCreateInfo(Vk.FenceCreateFlags.Signaled);
			s_transferFence = s_device.CreateFence(fci);
		}

		// Called when the graphics device is disposing to perform object cleanup
		internal static void Cleanup()
		{
			s_queue.WaitIdle();

			s_transferFence.Dispose();

			s_transferBuffer.Dispose();
			s_transferMemory.Dispose();

			s_transferCommandPool.Dispose(); // Also disposes the command buffer
		}
	}
}
