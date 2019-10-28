/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Threading;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	// Allocates a large block of host memory that can be reserved in chunks for staging buffer operations
	internal static class StagingBuffer
	{
		public const uint FULL_BUFFER_SIZE = 32 * 1024 * 1024; // 32 MB
		public const uint MAX_REQUEST_SIZE = FULL_BUFFER_SIZE / 4; // 8 MB - maximum size of a single request
		public const uint BLOCK_SIZE = 256 * 1024; // 256 KB - smallest subdivision of the full buffer
		public const ushort BLOCK_COUNT = (ushort)(FULL_BUFFER_SIZE / BLOCK_SIZE); // 128 - Number of blocks in the buffer
		private const Vk.MemoryPropertyFlags REQUIRED_MEM_PROPS = 
			Vk.MemoryPropertyFlags.HostVisible | Vk.MemoryPropertyFlags.HostCoherent;
		private const Vk.MemoryPropertyFlags IDEAL_MEM_PROPS = REQUIRED_MEM_PROPS | Vk.MemoryPropertyFlags.HostCached;

		#region Fields
		// Vulkan objects for the buffer
		private static Vk.Buffer _Buffer;
		private static Vk.DeviceMemory _Memory;
		public static Vk.Buffer Buffer => _Buffer;
		private static IntPtr _Mapped;
		public static IntPtr MemoryPtr => _Mapped;
		private static Vk.CommandPool _Pool;


		// Allocation tracking objects
		private static MemoryBlock[] _Blocks;
		private static FifoLock _ReserveLock; // Ensures thread-safe and ordered access to reserving memory
		private static readonly object _BlockLock = new object(); // Locks on modifications to '_Blocks'
		private static ManualResetEventSlim _FreeEvent; // Signals the reserve operation is waiting on a free operation
		#endregion // Fields

		#region Lifecycle
		// Allocates the memory and prepares the objects
		public static void Initialize(GraphicsDevice device)
		{
			_Pool = device.VkDevice.CreateCommandPool(device.Queues.FamilyIndex,
				Vk.CommandPoolCreateFlags.Transient | Vk.CommandPoolCreateFlags.ResetCommandBuffer);

			_Buffer = device.VkDevice.CreateBuffer(
				size: FULL_BUFFER_SIZE,
				usage: Vk.BufferUsageFlags.TransferDestination | Vk.BufferUsageFlags.TransferSource,
				sharingMode: Vk.SharingMode.Exclusive,
				queueFamilyIndices: Vk.Constants.QueueFamilyIgnored,
				flags: Vk.BufferCreateFlags.None
			);
			
			var mreq = _Buffer.GetMemoryRequirements();
			var midx = device.FindMemoryTypeIndex(mreq.MemoryTypeBits, IDEAL_MEM_PROPS);
			if (!midx.HasValue)
				midx = device.FindMemoryTypeIndex(mreq.MemoryTypeBits, REQUIRED_MEM_PROPS);
			if (!midx.HasValue)
				throw new PlatformNotSupportedException("Staging buffer failed - no valid memory types.");

			_Memory = device.VkDevice.AllocateMemory(
				allocationSize: mreq.Size,
				memoryTypeIndex: midx.Value
			);
			_Buffer.BindMemory(_Memory, 0);
			_Mapped = _Memory.Map(0, mreq.Size, Vk.MemoryMapFlags.None); // Persistent mapping

			_Blocks = new MemoryBlock[BLOCK_COUNT];
			_Blocks[0].Prev = 0;
			_Blocks[0].Size = BLOCK_COUNT;
			_Blocks[0].Free = true;
			_ReserveLock = new FifoLock(false);
			_FreeEvent = new ManualResetEventSlim(false);
		}
		
		// Destroys the staging buffer and frees the associated memory
		public static void Terminate()
		{
			_Memory?.Unmap();
			_Buffer?.Dispose();
			_Memory?.Free();
			_Pool?.FreeCommandBuffers(null);
			_Pool?.Dispose();
		}
		#endregion // Lifecycle

		#region Regions
		// Reserves a section of the staging buffer.
		//   size - the full size requested - will be clamped to MAX_REQUEST_SIZE
		//   minSize - the minimum size required - > MAX_REQUEST_SIZE will clamp
		public static TransferMemory Reserve(uint size, uint minSize = BLOCK_SIZE)
		{
			minSize = Math.Clamp(minSize, BLOCK_SIZE, MAX_REQUEST_SIZE);
			size = Math.Clamp(size, minSize, MAX_REQUEST_SIZE);

			try
			{
				_ReserveLock.Lock();

				do
				{
					_FreeEvent.Reset();

					var ridx = FindAndReserve(size, minSize);
					if (ridx.HasValue)
					{
						return new TransferMemory(ridx.Value.idx * BLOCK_SIZE, ridx.Value.size * BLOCK_SIZE, 
							Core.Instance.GraphicsDevice.VkDevice.CreateFence(Vk.FenceCreateFlags.None),
							Core.Instance.GraphicsDevice.VkDevice.AllocateCommandBuffer(_Pool, Vk.CommandBufferLevel.Primary));
					}

					if (!_FreeEvent.IsSet)
						_FreeEvent.Wait(100); // There is no point in searching again until we know that there have been 
											  // blocks freed up. Having a timeout allows the thread to continue, even in 
											  // the event of a state corruption or missed event.
				}
				while (true);
			}
			finally
			{
				_ReserveLock.Unlock();
			}
		}

		// Performs a single-pass search of the memory blocks
		private static (uint idx, ushort size)? FindAndReserve(uint size, uint minSize)
		{
			uint idx = 0;
			uint? minBestIdx = null;
			uint minBestSz = 0;

			lock (_BlockLock)
			{
				// Find one that is the requested size, while tracking ones that work for minSize
				while (idx < BLOCK_COUNT)
				{
					ref var block = ref _Blocks[idx];
					uint bytes = block.Size * BLOCK_SIZE;

					if (block.Free)
					{
						if (bytes >= size) // First block large enough for 'size'
						{
							block.Free = false;
							block.Size = (ushort)Math.Ceiling((double)size / BLOCK_SIZE);
							if ((idx + block.Size) < BLOCK_COUNT) // Create a new free block 
							{
								ref var nb = ref _Blocks[idx + block.Size];
								if (nb.Free)
								{
									nb.Size = (ushort)((bytes / BLOCK_SIZE) - block.Size);
									nb.Prev = block.Size;
								}
							}
							return (idx, block.Size);
						}
						else if (bytes >= minSize) // Check if it works for 'minSize'
						{
							if (bytes > minBestSz)
							{
								minBestSz = bytes;
								minBestIdx = idx;
							}
						}
					}

					idx += block.Size;
				}

				// Return the minimum size slot that works (might not have one)
				if (minBestIdx.HasValue)
				{
					_Blocks[minBestIdx.Value].Free = false;
					return (minBestIdx.Value, (ushort)(minBestSz / BLOCK_SIZE));
				}
				return null;
			}
		}

		// Frees a reserved section
		public static void Free(uint offset, Vk.CommandBuffer cmd)
		{
			uint index = offset / BLOCK_SIZE;

			lock (_BlockLock)
			{
				ref var block = ref _Blocks[index];
				if (block.Free)
					throw new InvalidOperationException("Attempt to free unreserved staging block.");
				block.Free = true;

				// Update the previous block
				if (block.Prev != 0)
				{
					ref var pb = ref _Blocks[index - block.Prev];
					if (pb.Free)
					{
						pb.Size += block.Size;
						index -= block.Size;
						block = ref pb; // Operate from the previous block moving forward
					}
				}

				// Update the next block
				if ((index + block.Size) < BLOCK_COUNT)
				{
					ref var nb = ref _Blocks[index + block.Size];
					if (nb.Free)
					{
						block.Size += nb.Size;
						if ((index + block.Size) < BLOCK_COUNT) // Update the prev on the next-next block
							_Blocks[index + block.Size].Prev = block.Size;
					}
				}

				// Signal a free event
				_FreeEvent.Set();
			}

			_Pool.FreeCommandBuffers(new [] { cmd });
		}
		#endregion // Regions

		// Describes the allocation state of a single block of memory
		private struct MemoryBlock
		{
			private const uint SIZE_MASK = 0x0000FFFF;
			private const uint PREV_MASK = 0x7FFF0000;
			private const uint FREE_MASK = 0x80000000;
			private const int PREV_SHFT = 16;

			private uint _data;
			public ushort Size // The size of the memory region
			{
				readonly get => (ushort)(_data & SIZE_MASK);
				set => _data = (_data & ~SIZE_MASK) | value;
			}
			public ushort Prev // The distance (in blocks) to the start of the previous region
			{
				readonly get => (ushort)((_data & PREV_MASK) >> PREV_SHFT);
				set => _data = (_data & ~PREV_MASK) | (uint)((value << PREV_SHFT) & PREV_MASK);
			}
			public bool Free // If the region is free, false = reserved
			{
				readonly get => (_data & FREE_MASK) > 0;
				set => _data = value ? (_data | FREE_MASK) : (_data & ~FREE_MASK);
			}
		}
	}
}
