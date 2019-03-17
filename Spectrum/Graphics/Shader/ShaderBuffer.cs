using System;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	// Maintains a monolithic buffer on the graphics device that is used to source shader uniform data
	// Each pipeline instance is given part of this buffer to use for its shader, and the shader uploads
	//   information into their portion to be sourced as uniforms.
	// Memory allocation is managed on the host, and is performed in pages
	// It is very possible for this memory to get fragmented, so we will check on this once we get 
	//   realistic programs up and running
	internal static class ShaderBuffer
	{
		private const uint BUFFER_SIZE = 32768; // 32KB for the global uniform buffer (keep this a multiple of MAX_SHADER_SIZE)
		private const uint PAGE_SIZE = 128; // 128 bytes per memory page (keep this a divisor of MAX_SHADER_SIZE)
		private const uint PAGE_COUNT = BUFFER_SIZE / PAGE_SIZE;
		private const uint MAX_SHADER_SIZE = 1024; // Maximum of 1Kb for any one shader (used to size the staging buffer)
		private const uint MAX_PAGE_COUNT = MAX_SHADER_SIZE / PAGE_SIZE;

		#region Fields
		// Device local buffer
		public static Vk.Buffer DeviceBuffer { get; private set; } = null;
		private static Vk.DeviceMemory s_deviceMemory;

		// Host local staging buffer
		private static Vk.Buffer s_stagingBuffer;
		private static Vk.DeviceMemory s_stagingMemory;

		// Memory allocation objects
		private static MemBlock s_memBlock; // Start of the sequence of memory blocks
		private static readonly object s_allocLock = new object();
		public static uint BlockCount { get; private set; }
		#endregion // Fields

		// Attempt to allocate a portion of the uniform buffer that is at least `size` bytes large
		public static bool Allocate(uint size, out BufferRange br)
		{
			if (size > MAX_SHADER_SIZE)
				throw new ArgumentException($"Cannot reserve uniform buffer space larger than {MAX_SHADER_SIZE} bytes.");
			var pCount = (size / PAGE_SIZE) + (((size % PAGE_SIZE) == 0) ? 0u : 1u); // ceiling

			lock (s_allocLock)
			{
				var curr = s_memBlock;
				do
				{
					if ((curr.Size >= pCount) && !curr.Reserved)
					{
						// The new block for the reserved memory, update previous block (if exists)
						var reserved = new MemBlock
						{
							Offset = curr.Offset,
							Size = pCount,
							Prev = curr.Prev,
							Next = curr.Next,
							Reserved = true
						};
						if (curr.Prev != null)
							curr.Prev.Next = reserved;
						else
							s_memBlock = reserved; // This is the new first block

						// Update (or create) the next block
						if (pCount == curr.Size) // Used up the rest of the block, cut it out
						{
							if (reserved.Next != null)
								reserved.Next.Prev = reserved;
						}
						else // Shrink the current block and adjust the references
						{
							curr.Size -= pCount;
							curr.Offset += pCount;
							reserved.Next = curr;
							curr.Prev = reserved;
							// curr.Next.Prev still correct
						}

						// Return
						br = new BufferRange(reserved.Offset, reserved.Size);
						BlockCount += 1;
						return true;
					}

					curr = curr.Next;
				}
				while (curr != null); 
			}

			br = default;
			return false;
		}

		// Free the memory block
		public static void Free(in BufferRange br)
		{
			lock (s_allocLock)
			{
				// Find the block to free
				MemBlock block = s_memBlock;
				do
				{
					if (block.Offset == br.BlockOffset)
						break;
					block = block.Next;
				}
				while (block != null);
				if (block == null)
					throw new InvalidOperationException("Attempted to free invalid uniform buffer range.");
				if (!block.Reserved)
					throw new InvalidOperationException("Attempted to free a uniform buffer range that was not reserved.");

				// Get surrounding info
				bool first = block.IsFirst;
				bool lFree = !block.IsFirst && !block.Prev.Reserved;
				bool rFree = !block.IsLast && !block.Next.Reserved;

				// Act on the different options for surrounding blocks
				if (!lFree && !rFree) // Very easy, simply free this one
				{
					block.Reserved = false;
				}
				else if (rFree && !lFree) // Merge right (use the current block, discard the next)
				{
					block.Reserved = false;
					block.Size += block.Next.Size;
					block.Next = block.Next.Next;
					if (block.Next != null)
						block.Next.Prev = block;
				}
				else if (lFree && !rFree) // Merge left (use the previous block, discard the current)
				{
					block.Prev.Size += block.Size;
					block.Prev.Next = block.Next;
					if (block.Next != null)
						block.Next.Prev = block.Prev;
				}
				else // Merge in both directions (use the previous, discard the current and next)
				{
					block.Prev.Size += (block.Size + block.Next.Size);
					block.Prev.Next = block.Next.Next;
					if (block.Next.Next != null)
						block.Next.Next.Prev = block.Prev;
				}

				// Update the count
				BlockCount -= 1;
			}
		}

		// Called at the beginning of the program to set up the buffer
		public static void CreateResources()
		{
			var dev = SpectrumApp.Instance.GraphicsDevice;

			// Allocate the device buffer
			var bci = new Vk.BufferCreateInfo(
				BUFFER_SIZE,
				Vk.BufferUsages.TransferDst | Vk.BufferUsages.UniformBuffer,
				flags: Vk.BufferCreateFlags.None,
				sharingMode: Vk.SharingMode.Exclusive
			);
			DeviceBuffer = dev.VkDevice.CreateBuffer(bci);
			var memr = DeviceBuffer.GetMemoryRequirements();
			var memi = dev.FindMemoryTypeIndex(memr.MemoryTypeBits, Vk.MemoryProperties.DeviceLocal);
			if (memi == -1)
				throw new InvalidOperationException("There is no memory type that supports uniform buffers.");
			var mai = new Vk.MemoryAllocateInfo(memr.Size, memi);
			s_deviceMemory = dev.VkDevice.AllocateMemory(mai);
			DeviceBuffer.BindMemory(s_deviceMemory);

			// Allocate the staging buffer
			bci = new Vk.BufferCreateInfo(
				MAX_SHADER_SIZE,
				Vk.BufferUsages.TransferSrc,
				flags: Vk.BufferCreateFlags.None,
				sharingMode: Vk.SharingMode.Exclusive
			);
			s_stagingBuffer = dev.VkDevice.CreateBuffer(bci);
			memr = s_stagingBuffer.GetMemoryRequirements();
			memi = dev.FindMemoryTypeIndex(memr.MemoryTypeBits, Vk.MemoryProperties.HostVisible | Vk.MemoryProperties.HostCoherent);
			if (memi == -1)
				throw new InvalidOperationException("There is no memory type that supports host staging buffers.");
			mai = new Vk.MemoryAllocateInfo(memr.Size, memi);
			s_stagingMemory = dev.VkDevice.AllocateMemory(mai);
			s_stagingBuffer.BindMemory(s_stagingMemory);

			// Initial memory block of the full buffer
			s_memBlock = new MemBlock {
				Size = PAGE_COUNT,
				Offset = 0,
				Reserved = false
			};
			BlockCount = 0;
		}

		// Called at the end of the program to release the resources
		public static void Cleanup()
		{
			DeviceBuffer.Dispose();
			s_stagingBuffer.Dispose();
			s_deviceMemory.Dispose();
			s_stagingMemory.Dispose();
		}

		// Holds information related to a memory block in the uniform buffer
		private class MemBlock
		{
			public uint Size = 0; // In terms of pages
			public uint Offset = 0; // In terms of pages
			public MemBlock Prev = null;
			public MemBlock Next = null;
			public bool Reserved = false; // If this block is currently in use

			public bool IsFirst => (Prev == null);
			public bool IsLast => (Next == null);
		}

		// Used by pipelines and shaders to hold on to their portion of the uniform buffer to use
		public struct BufferRange
		{
			public readonly uint Offset; // The offset into the uniform buffer, in bytes
			public readonly uint Size; // The size of the available space, in bytes
			public readonly uint BlockOffset; // The offset value of the MemBlock instance that holds the data for this range

			public BufferRange(uint bOff, uint bSize)
			{
				Offset = bOff * PAGE_SIZE;
				Size = bSize * PAGE_SIZE;
				BlockOffset = bOff;
			}
		}
	}
}
