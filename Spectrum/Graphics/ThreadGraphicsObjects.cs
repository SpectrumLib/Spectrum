/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using Vk = SharpVk;
using static Spectrum.InternalLog;

namespace Spectrum.Graphics
{
	// Contains the set of graphics objects that exist per-thread
	internal class ThreadGraphicsObjects : IDisposable
	{
		private const uint SCRATCH_POOL_COUNT = 8; // Number of pooled scratch buffers
		private const uint TRANSFER_BUFFER_COUNT = 2; // Number of transfer buffers

		#region Fields
		public readonly int ThreadId;
		public readonly Vk.CommandPool CommandPool;
		public (Vk.CommandBuffer Buffer, Vk.Fence Fence, bool Free)[] ScratchPool;
		private uint _scratchIndex;
		public readonly Vk.DeviceMemory TransferMemory;
		public (Vk.Buffer Buffer, Vk.CommandBuffer Commands, Vk.Fence Fence, bool Free)[] TransferPool;
		public readonly bool CoherentTransfer;
		public readonly IntPtr TransferPointer;
		private uint _transferIndex;
		#endregion // Fields

		public ThreadGraphicsObjects(int tid, GraphicsDevice dev)
		{
			ThreadId = tid;
			CommandPool = dev.VkDevice.CreateCommandPool(dev.Queues.FamilyIndex,
				Vk.CommandPoolCreateFlags.ResetCommandBuffer | Vk.CommandPoolCreateFlags.Transient);

			// Create scratch pool objects
			ScratchPool = new (Vk.CommandBuffer, Vk.Fence, bool)[SCRATCH_POOL_COUNT];
			var sbufs = dev.VkDevice.AllocateCommandBuffers(CommandPool, Vk.CommandBufferLevel.Primary, SCRATCH_POOL_COUNT);
			for (uint i = 0; i < SCRATCH_POOL_COUNT; ++i)
			{
				ScratchPool[i].Buffer = sbufs[i];
				ScratchPool[i].Fence = dev.VkDevice.CreateFence(Vk.FenceCreateFlags.Signaled); // Need to start signaled
				ScratchPool[i].Free = true;
			}
			_scratchIndex = 0;

			// Create transfer buffers
			TransferPool = new (Vk.Buffer, Vk.CommandBuffer, Vk.Fence, bool)[TRANSFER_BUFFER_COUNT];
			sbufs = dev.VkDevice.AllocateCommandBuffers(CommandPool, Vk.CommandBufferLevel.Primary, TRANSFER_BUFFER_COUNT);
			for (uint i = 0; i < TRANSFER_BUFFER_COUNT; ++i)
			{
				TransferPool[i].Buffer = dev.VkDevice.CreateBuffer(
					TransferBuffer.SIZE, Vk.BufferUsageFlags.TransferDestination | Vk.BufferUsageFlags.TransferSource,
					Vk.SharingMode.Exclusive, Vk.Constants.QueueFamilyIgnored, Vk.BufferCreateFlags.None
				);
				TransferPool[i].Commands = sbufs[i];
				TransferPool[i].Fence = dev.VkDevice.CreateFence(Vk.FenceCreateFlags.Signaled); // Need to start signaled
				TransferPool[i].Free = true;
			}
			var memidx = GetMemoryInfo(dev, TransferPool[0].Buffer, out CoherentTransfer);
			TransferMemory = dev.VkDevice.AllocateMemory(TransferBuffer.SIZE * TRANSFER_BUFFER_COUNT, memidx);
			TransferPointer = TransferMemory.Map(0, TransferBuffer.SIZE * TRANSFER_BUFFER_COUNT, Vk.MemoryMapFlags.None);
			for (uint i = 0; i < TRANSFER_BUFFER_COUNT; ++i)
			{
				TransferPool[i].Buffer.BindMemory(TransferMemory, i * TransferBuffer.SIZE);
			}
			_transferIndex = 0;

			IINFO($"Created graphics objects for thread {tid}.");
		}

		#region Scratch Buffer
		// Finds the next scratch buffer available
		public uint NextScratchBuffer()
		{
			while (true)
			{
				ref var buf = ref ScratchPool[_scratchIndex];
				// Make sure it is not in use AND has finished processing
				if (buf.Free && (buf.Fence.GetStatus() == Vk.Result.Success))
				{
					buf.Free = false;
					return _scratchIndex;
				}
				_scratchIndex = (_scratchIndex + 1) % SCRATCH_POOL_COUNT;
			}
		}

		// Releases the scratch buffer to be used again
		public void ReleaseScratchBuffer(uint index)
		{
			ref var buf = ref ScratchPool[index];
			if (buf.Free)
				throw new InvalidOperationException("Attempted to free unacquired scratch buffer (BUG IN LIBRARY).");
			buf.Free = true;
		}
		#endregion // Scratch Buffer

		#region Transfer Buffer
		// Finds the next transfer buffer available
		public uint NextTransferBuffer()
		{
			while (true)
			{
				ref var buf = ref TransferPool[_transferIndex];
				if (buf.Free && (buf.Fence.GetStatus() == Vk.Result.Success))
				{
					buf.Free = false;
					return _transferIndex;
				}
				_transferIndex = (_transferIndex + 1) % TRANSFER_BUFFER_COUNT;
			}
		}

		// Releases the transfer buffer to be used again
		public void ReleaseTransferBuffer(uint index)
		{
			ref var buf = ref TransferPool[index];
			if (buf.Free)
				throw new InvalidOperationException("Attempted to free unaquired transfer buffer (BUG IN LIBRARY)");
			buf.Free = true;
		}
		#endregion // Transfer Buffer

		public void Dispose()
		{
			if (ScratchPool != null)
			{
				foreach (var b in ScratchPool) // Buffers get freed below
					b.Fence.Dispose();
			}

			if (TransferMemory != null)
			{
				TransferMemory.Unmap();
				foreach (var b in TransferPool) // Commands get freed below
				{
					b.Buffer.Dispose();
					b.Fence.Dispose();
				}
				TransferMemory.Free();
			}

			CommandPool?.Dispose();
			IINFO($"Destroyed graphics objects for thread {ThreadId}.");
		}

		private static uint GetMemoryInfo(GraphicsDevice dev, Vk.Buffer tbuf, out bool coherent)
		{
			var memreq = tbuf.GetMemoryRequirements();
			var memidx = dev.Memory.Find(memreq.MemoryTypeBits, // Preferred flags
				Vk.MemoryPropertyFlags.HostVisible | Vk.MemoryPropertyFlags.HostCached | Vk.MemoryPropertyFlags.HostCoherent
			);
			memidx ??= dev.Memory.Find(memreq.MemoryTypeBits, // Backup flags
				Vk.MemoryPropertyFlags.HostVisible | Vk.MemoryPropertyFlags.HostCoherent
			);
			memidx ??= dev.Memory.Find(memreq.MemoryTypeBits, // Last chance flags
				Vk.MemoryPropertyFlags.HostVisible
			);
			if (!memidx.HasValue)
				throw new PlatformNotSupportedException("Device does not support transfer buffers.");

			coherent = (dev.Memory.Properties.MemoryTypes[memidx.Value].PropertyFlags & Vk.MemoryPropertyFlags.HostCoherent) > 0 ||
					   (dev.Memory.Properties.MemoryTypes[memidx.Value].PropertyFlags & Vk.MemoryPropertyFlags.HostCached) == 0;
			coherent &= !Runtime.OS.IsOSX; // MoltenVK has bug where texture memory is never coherent
			return memidx.Value;
		}
	}
}
