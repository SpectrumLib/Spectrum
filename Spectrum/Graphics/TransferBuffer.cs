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
		private static Vk.CommandPool s_commandPool;
		private static Vk.CommandBuffer s_pushCommands;
		private static Vk.Buffer s_pushBuffer;
		private static Vk.DeviceMemory s_pushMemory;
		private static Vk.Fence s_pushFence;
		private static Vk.SubmitInfo s_pushSubmitInfo;
		private static Vk.CommandBuffer s_pullCommands;
		private static Vk.Buffer s_pullBuffer;
		private static Vk.DeviceMemory s_pullMemory;
		private static Vk.Fence s_pullFence;
		private static Vk.SubmitInfo s_pullSubmitInfo;

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
				s_pushFence.Wait();
				s_pushFence.Reset();

				// Map the staging buffer, and copy the memory
				IntPtr mapped = s_pushMemory.Map(0, currLength);
				Buffer.MemoryCopy(src, mapped.ToPointer(), currLength, currLength);
				s_pushMemory.Unmap();

				// Start recording
				s_pushCommands.Begin(ONE_TIME_SUBMIT_INFO);

				// Add the transfer command
				Vk.BufferCopy bc = new Vk.BufferCopy(currLength, 0, currDstOffset);
				s_pushCommands.CmdCopyBuffer(s_pushBuffer, dst, bc);

				// End recording, and submit
				s_pushCommands.End();
				s_queue.Submit(s_pushSubmitInfo, fence: s_pushFence);
			}
		}

		// Starts a transfer of raw data from the host to a device image
		// Unlike the buffers, which are easy to divide into blocks, it is very difficult to divide 3D spaces into blocks
		//   with alignment and size constraints. For now, we see if we can easily upload the entire image region with the
		//   existing staging buffer, otherwise we allocate a massive temp buffer to upload the data all at once with.
		//   Yes, this is inefficient. Yes, this is slow. Yes, I hate this as much as you do.
		//   We will improve this once the library is in a usable state.
		//   As it stands, we do have 16MB available, which is a 2048x2048 image before the temp buffers are created.
		public unsafe static void PushImage(byte *src, uint length, TextureType type, Vk.Image dst, in Vk.Offset3D dstOff, in Vk.Extent3D dstSize, uint layer, uint layerCount)
		{
			var buffer = s_pushBuffer;
			var memory = s_pushMemory;
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
			s_pushFence.Wait();
			s_pushFence.Reset();

			// Map the staging buffer, and copy the memory
			IntPtr mapped = memory.Map(0, length);
			Buffer.MemoryCopy(src, mapped.ToPointer(), length, length);
			memory.Unmap();

			// Start recording
			s_pushCommands.Begin(ONE_TIME_SUBMIT_INFO);

			// Transition to the transfer dst layout
			var imb = new Vk.ImageMemoryBarrier(
				dst,
				new Vk.ImageSubresourceRange(Vk.ImageAspects.Color, 0, 1, (int)layer, (int)layerCount),
				Vk.Accesses.None,
				Vk.Accesses.TransferWrite,
				Vk.ImageLayout.ShaderReadOnlyOptimal,
				Vk.ImageLayout.TransferDstOptimal
			);
			s_pushCommands.CmdPipelineBarrier(
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
			s_pushCommands.CmdCopyBufferToImage(buffer, dst, Vk.ImageLayout.TransferDstOptimal, bic);

			// Transition back to the shader layout
			imb.SrcAccessMask = Vk.Accesses.TransferWrite;
			imb.DstAccessMask = Vk.Accesses.ShaderRead;
			imb.OldLayout = Vk.ImageLayout.TransferDstOptimal;
			imb.NewLayout = Vk.ImageLayout.ShaderReadOnlyOptimal;
			s_pushCommands.CmdPipelineBarrier(
				Vk.PipelineStages.Transfer,
				Vk.PipelineStages.VertexShader,
				imageMemoryBarriers: new[] { imb }
			);

			// End recording, and submit
			s_pushCommands.End();
			s_queue.Submit(s_pushSubmitInfo, fence: s_pushFence);

			// Release the temp buffers if needed
			if (length > DEFAULT_BUFFER_SIZE)
			{
				buffer.Dispose();
				memory.Dispose();
			}
		}
		#endregion // Host -> Device

		#region Device -> Host
		// Starts a transfer of raw data from a device buffer to the host
		public unsafe static void PullBuffer(byte *dst, uint length, Vk.Buffer src, uint srcOffset)
		{
			// Calculate transfer information
			uint blockCount = (uint)Mathf.Ceiling(length / (float)DEFAULT_BUFFER_SIZE);

			// Iterate over the transfer blocks
			for (uint bidx = 0; bidx < blockCount; ++bidx)
			{
				// Calculate offsets and block sizes
				uint blockOff = bidx * DEFAULT_BUFFER_SIZE;
				byte* currDst = dst + blockOff;
				uint currSrcOffset = srcOffset + blockOff;
				uint currLength = Math.Min(length - blockOff, DEFAULT_BUFFER_SIZE);

				// start recording
				s_pullCommands.Begin(ONE_TIME_SUBMIT_INFO);

				// Add the transfer command
				Vk.BufferCopy bc = new Vk.BufferCopy(currLength, 0, currSrcOffset);
				s_pullCommands.CmdCopyBuffer(src, s_pullBuffer, bc);

				// End recording, and submit
				s_pullCommands.End();
				s_queue.Submit(s_pullSubmitInfo, fence: s_pullFence);

				// Wait for the transfer to the host to complete
				s_pullFence.Wait();
				s_pullFence.Reset();

				// Map the staging buffer, and copy the memory
				IntPtr mapped = s_pullMemory.Map(0, currLength);
				Buffer.MemoryCopy(mapped.ToPointer(), dst, currLength, currLength);
				s_pullMemory.Unmap();
			}
		}

		// Starts a transfer of raw data from a device image to the host
		// Unlike the buffers, which are easy to divide into blocks, it is very difficult to divide 3D spaces into blocks
		//   with alignment and size constraints. For now, we see if we can easily download the entire image region with the
		//   existing staging buffer, otherwise we allocate a massive temp buffer to download the data all at once with.
		//   Yes, this is inefficient. Yes, this is slow. Yes, I hate this as much as you do.
		//   We will improve this once the library is in a usable state.
		//   As it stands, we do have 16MB available, which is a 2048x2048 image before the temp buffers are created.
		public unsafe static void PullImage(byte* dst, uint length, TextureType type, Vk.Image src, in Vk.Offset3D srcOff, in Vk.Extent3D srcSize, uint layer, uint layerCount)
		{
			var buffer = s_pullBuffer;
			var memory = s_pullMemory;
			if (length > DEFAULT_BUFFER_SIZE) // Boo! Make a huge temp buffer
			{
				var bci = new Vk.BufferCreateInfo(
					length,
					Vk.BufferUsages.TransferDst,
					flags: Vk.BufferCreateFlags.None,
					sharingMode: Vk.SharingMode.Exclusive
				);
				buffer = s_device.CreateBuffer(bci);
				var memReq = buffer.GetMemoryRequirements();
				var mai = new Vk.MemoryAllocateInfo(memReq.Size, s_bufferFamily);
				memory = s_device.AllocateMemory(mai);
				buffer.BindMemory(memory);
			}

			// Start recording
			s_pullCommands.Begin(ONE_TIME_SUBMIT_INFO);

			// Transition to the transfer src layout
			var imb = new Vk.ImageMemoryBarrier(
				src,
				new Vk.ImageSubresourceRange(Vk.ImageAspects.Color, 0, 1, (int)layer, (int)layerCount),
				Vk.Accesses.None,
				Vk.Accesses.TransferRead,
				Vk.ImageLayout.ShaderReadOnlyOptimal,
				Vk.ImageLayout.TransferSrcOptimal
			);
			s_pullCommands.CmdPipelineBarrier(
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
				ImageOffset = srcOff,
				ImageExtent = srcSize
			};
			s_pullCommands.CmdCopyImageToBuffer(src, Vk.ImageLayout.TransferSrcOptimal, s_pullBuffer, bic);

			// Transition back to the shader layout
			imb.SrcAccessMask = Vk.Accesses.TransferRead;
			imb.DstAccessMask = Vk.Accesses.ShaderRead;
			imb.OldLayout = Vk.ImageLayout.TransferSrcOptimal;
			imb.NewLayout = Vk.ImageLayout.ShaderReadOnlyOptimal;
			s_pullCommands.CmdPipelineBarrier(
				Vk.PipelineStages.Transfer,
				Vk.PipelineStages.VertexShader,
				imageMemoryBarriers: new[] { imb }
			);

			// End recording, and submit
			s_pullCommands.End();
			s_queue.Submit(s_pullSubmitInfo, fence: s_pullFence);

			// Wait for the transfer to the host to complete
			s_pullFence.Wait();
			s_pullFence.Reset();

			// Map the staging buffer, and copy the memory
			IntPtr mapped = memory.Map(0, length);
			Buffer.MemoryCopy(mapped.ToPointer(), dst, length, length);
			memory.Unmap();

			// Release the temp buffers if needed
			if (length > DEFAULT_BUFFER_SIZE)
			{
				buffer.Dispose();
				memory.Dispose();
			}
		}
		#endregion // Device -> Host

		#region Resource Management
		// Called from the graphics device when it is initialized
		public static void CreateResources()
		{
			s_graphicsDevice = SpectrumApp.Instance.GraphicsDevice;

			// Create the transfer command buffers
			var cpci = new Vk.CommandPoolCreateInfo(
				s_queue.FamilyIndex,
				Vk.CommandPoolCreateFlags.Transient | Vk.CommandPoolCreateFlags.ResetCommandBuffer
			);
			s_commandPool = s_device.CreateCommandPool(cpci);
			var cbai = new Vk.CommandBufferAllocateInfo(Vk.CommandBufferLevel.Primary, 2);
			var bufs = s_commandPool.AllocateBuffers(cbai);
			s_pushCommands = bufs[0];
			s_pullCommands = bufs[1];

			// Create the staging buffers
			var bci = new Vk.BufferCreateInfo(
				DEFAULT_BUFFER_SIZE,
				Vk.BufferUsages.TransferSrc,
				flags: Vk.BufferCreateFlags.None,
				sharingMode: Vk.SharingMode.Exclusive
			);
			s_pushBuffer = s_device.CreateBuffer(bci);
			bci.Usage = Vk.BufferUsages.TransferDst;
			s_pullBuffer = s_device.CreateBuffer(bci);

			// Allocate the staging memories
			var memReq = s_pushBuffer.GetMemoryRequirements();
			s_bufferFamily = s_graphicsDevice.FindMemoryTypeIndex(memReq.MemoryTypeBits, Vk.MemoryProperties.HostVisible | Vk.MemoryProperties.HostCoherent);
			if (s_bufferFamily == -1)
				throw new InvalidOperationException("Cannot find a memory type that supports host buffers (this means bad or out-of-date hardware)");
			var mai = new Vk.MemoryAllocateInfo(memReq.Size, s_bufferFamily);
			s_pushMemory = s_device.AllocateMemory(mai);
			s_pushBuffer.BindMemory(s_pushMemory);
			s_pullMemory = s_device.AllocateMemory(mai);
			s_pullBuffer.BindMemory(s_pullMemory);

			// Create the transfer fences
			var fci = new Vk.FenceCreateInfo(Vk.FenceCreateFlags.Signaled);
			s_pushFence = s_device.CreateFence(fci); // Start signaled since we wait BEFORE we submit
			fci.Flags = Vk.FenceCreateFlags.None;
			s_pullFence = s_device.CreateFence(fci); // Start unsignaled since we wait AFTER we submit

			// Pre-build the queue submit infos for a little speed boost
			s_pushSubmitInfo = new Vk.SubmitInfo(commandBuffers: new[] { s_pushCommands });
			s_pullSubmitInfo = new Vk.SubmitInfo(commandBuffers: new[] { s_pullCommands });
		}

		// Called when the graphics device is disposing to perform object cleanup
		internal static void Cleanup()
		{
			s_queue.WaitIdle();

			s_pushFence.Dispose();
			s_pullFence.Dispose();

			s_pushBuffer.Dispose();
			s_pullBuffer.Dispose();
			s_pushMemory.Dispose();
			s_pullMemory.Dispose();

			s_commandPool.Dispose(); // Also disposes the command buffers
		}
		#endregion // Resource Management
	}
}
