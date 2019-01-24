using Spectrum.Utilities;
using System;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	// Holds host-visible buffers to use as staging buffers to transfer data to/from the graphics device for images
	//   and buffers.
	// This works using the following general steps:
	//   1. An image or buffer calls one of the transfer functions
	//   2. The transfer fence is waited on for the last transfer to finish, then reset
	//   3. The staging buffer is mapped, written to, then unmapped
	//   4. The command buffer is recorded to transfer the data
	//   5. The transfer commands are submitted with the fence, and the transfer function returns immediately
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
		private static Vk.Buffer s_stagingBuffer;
		private static Vk.DeviceMemory s_stagingMemory;
		private static Vk.Fence s_transferFence;
		private static Vk.SubmitInfo s_submitInfo;

		// Cached values to making the stupid large temp buffers
		private static int s_bufferFamily;
		#endregion // Fields

		#region Host -> Device
		// Starts a transfer of raw data from the host to a device buffer
		public unsafe static void PushBuffer(byte *src, uint length, Vk.Buffer dst, uint dstOffset)
		{
			// Calculate transfer information
			uint blockCount = (uint)Mathf.Ceiling(length / (float)DEFAULT_BUFFER_SIZE);

			// Iterate over the transfer blocks
			for (uint bidx = 0; bidx < blockCount; ++bidx)
			{
				// Calculate offsets and block sizes
				uint blockOff = bidx * DEFAULT_BUFFER_SIZE;
				byte* currSrc = src + blockOff;
				uint currDstOffset = dstOffset + blockOff;
				uint currLength = Math.Min(length - blockOff, DEFAULT_BUFFER_SIZE);

				// Wait on the previous transfer
				s_transferFence.Wait();
				s_transferFence.Reset();

				// Map the staging buffer, and copy the memory
				IntPtr mapped = s_stagingMemory.Map(0, length);
				Buffer.MemoryCopy(src, mapped.ToPointer(), length, length);
				s_stagingMemory.Unmap();

				// Start recording
				s_transferCommands.Begin(ONE_TIME_SUBMIT_INFO);

				// Add the transfer command
				Vk.BufferCopy bc = new Vk.BufferCopy(length, 0, dstOffset);
				s_transferCommands.CmdCopyBuffer(s_stagingBuffer, dst, bc);

				// End recording, and submit
				s_transferCommands.End();
				s_queue.Submit(s_submitInfo, fence: s_transferFence);
			}
		}

		// Starts a transfer of raw data from the host to a device image
		// Unlike the buffers, which are easy to divide into blocks, it is very difficult to divide 3D spaces into blocks
		//   with alignment and size constraints. For now, we see if we can easily upload the entire image with the
		//   existing staging buffer, otherwise we allocate a massive temp buffer to upload the data all at once with.
		//   Yes, this is inefficient. Yes, this is slow. Yes, I hate this as much as you do.
		//   We will improve this once the library is in a usable state.
		//   As it stands, we do have 16MB available, which is a 2048x2048 image before the temp buffers are created.
		public unsafe static void PushImage(byte *src, uint length, TextureType type, Vk.Image dst, Vk.Offset3D dstOff, Vk.Extent3D dstSize, uint layer, uint layerCount)
		{
			// Validate transfer information
			uint totalSize = (uint)(dstSize.Width * dstSize.Height * dstSize.Depth);
			if (length != (totalSize * 4))
				throw new InvalidOperationException($"The source data length ({length}) does not make the destination image size ({totalSize})");

			var buffer = s_stagingBuffer;
			var memory = s_stagingMemory;
			if (length > DEFAULT_BUFFER_SIZE) // Boo! Make a huge temp buffer
			{
				var bci = new Vk.BufferCreateInfo(
					length,
					Vk.BufferUsages.TransferSrc,
					flags: Vk.BufferCreateFlags.None,
					sharingMode: Vk.SharingMode.Exclusive
				);
				buffer = s_device.CreateBuffer(bci);
				var memReq = buffer.GetMemoryRequirements();
				var mai = new Vk.MemoryAllocateInfo(memReq.Size, s_bufferFamily);
				memory = s_device.AllocateMemory(mai);
				buffer.BindMemory(memory);
			}

			// Wait on the previous transfer
			s_transferFence.Wait();
			s_transferFence.Reset();

			// Map the staging buffer, and copy the memory
			IntPtr mapped = memory.Map(0, length);
			Buffer.MemoryCopy(src, mapped.ToPointer(), length, length);
			memory.Unmap();

			// Start recording
			s_transferCommands.Begin(ONE_TIME_SUBMIT_INFO);

			// Transition to the transfer dst layout
			var imb = new Vk.ImageMemoryBarrier(
				dst,
				new Vk.ImageSubresourceRange(Vk.ImageAspects.Color, 0, 1, (int)layer, (int)layerCount),
				Vk.Accesses.None,
				Vk.Accesses.TransferWrite,
				Vk.ImageLayout.ShaderReadOnlyOptimal,
				Vk.ImageLayout.TransferDstOptimal
			);
			s_transferCommands.CmdPipelineBarrier(
				Vk.PipelineStages.TopOfPipe,
				Vk.PipelineStages.Transfer,
				imageMemoryBarriers: new[] { imb }
			);

			// Make the transfer
			var bic = new Vk.BufferImageCopy {
				BufferOffset = 0,
				BufferRowLength = 0,
				BufferImageHeight = 0,
				ImageSubresource = new Vk.ImageSubresourceLayers(Vk.ImageAspects.Color, 0, (int)layer, (int)layerCount),
				ImageOffset = dstOff,
				ImageExtent = dstSize
			};
			s_transferCommands.CmdCopyBufferToImage(buffer, dst, Vk.ImageLayout.TransferDstOptimal, bic);

			// Transition back to the shader layout
			imb.SrcAccessMask = Vk.Accesses.TransferWrite;
			imb.DstAccessMask = Vk.Accesses.ShaderRead;
			imb.OldLayout = Vk.ImageLayout.TransferDstOptimal;
			imb.NewLayout = Vk.ImageLayout.ShaderReadOnlyOptimal;
			s_transferCommands.CmdPipelineBarrier(
				Vk.PipelineStages.Transfer,
				Vk.PipelineStages.VertexShader,
				imageMemoryBarriers: new[] { imb }
			);

			// End recording, and submit
			s_transferCommands.End();
			s_queue.Submit(s_submitInfo, fence: s_transferFence);

			// Release the temp buffers if needed
			if (length > DEFAULT_BUFFER_SIZE)
			{
				buffer.Dispose();
				memory.Dispose();
			}
		}
		#endregion // Host -> Device

		#region Resource Management
		// Called from the graphics device when it is initialized
		public static void CreateResources()
		{
			s_graphicsDevice = SpectrumApp.Instance.GraphicsDevice;

			// Create the transfer command buffer
			var cpci = new Vk.CommandPoolCreateInfo(
				s_queue.FamilyIndex,
				Vk.CommandPoolCreateFlags.Transient | Vk.CommandPoolCreateFlags.ResetCommandBuffer
			);
			s_transferCommandPool = s_device.CreateCommandPool(cpci);
			var cbai = new Vk.CommandBufferAllocateInfo(Vk.CommandBufferLevel.Primary, 1);
			s_transferCommands = s_transferCommandPool.AllocateBuffers(cbai)[0];

			// Create the staging buffer
			var bci = new Vk.BufferCreateInfo(
				DEFAULT_BUFFER_SIZE,
				Vk.BufferUsages.TransferSrc,
				flags: Vk.BufferCreateFlags.None,
				sharingMode: Vk.SharingMode.Exclusive
			);
			s_stagingBuffer = s_device.CreateBuffer(bci);

			// Allocate the staging memory
			var memReq = s_stagingBuffer.GetMemoryRequirements();
			s_bufferFamily = s_graphicsDevice.FindMemoryTypeIndex(memReq.MemoryTypeBits, Vk.MemoryProperties.HostVisible | Vk.MemoryProperties.HostCoherent);
			if (s_bufferFamily == -1)
				throw new InvalidOperationException("Cannot find a memory type that supports host buffers (this means bad or out-of-date hardware)");
			var mai = new Vk.MemoryAllocateInfo(memReq.Size, s_bufferFamily);
			s_stagingMemory = s_device.AllocateMemory(mai);
			s_stagingBuffer.BindMemory(s_stagingMemory);

			// Create the transfer fence
			var fci = new Vk.FenceCreateInfo(Vk.FenceCreateFlags.Signaled);
			s_transferFence = s_device.CreateFence(fci);

			// Pre-build the queue submit info for a little speed boost
			s_submitInfo = new Vk.SubmitInfo(commandBuffers: new[] { s_transferCommands });
		}

		// Called when the graphics device is disposing to perform object cleanup
		internal static void Cleanup()
		{
			s_queue.WaitIdle();

			s_transferFence.Dispose();

			s_stagingBuffer.Dispose();
			s_stagingMemory.Dispose();

			s_transferCommandPool.Dispose(); // Also disposes the command buffer
		}
		#endregion // Resource Management
	}
}
