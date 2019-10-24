/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.CompilerServices;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	// Host visible buffer for transfering data to and from the graphics device
	// A small set of global transfer buffers are available in the graphics device
	// These take up multiple vulkan objects and quite a lot memory, so try to keep instances of this to a minimum
	// The push and pull functions are mutually threadsafe, and guarenteed FIFO ordered and starvation free
	internal class TransferBuffer : IDisposable
	{
		#region Fields
		// Reference to graphics device objects
		public GraphicsDevice Device => Core.Instance.GraphicsDevice;
		private Vk.Device _vkDevice => Device.VkDevice;
		private GraphicsDevice.DeviceQueues _queues => Device.Queues;

		// Vulkan objects for the buffer
		private Vk.CommandPool _commandPool;
		private Vk.CommandBuffer _commandBuffer;
		private Vk.Buffer _buffer;
		private Vk.DeviceMemory _memory;
		private Vk.Fence _pushFence;
		private Vk.Fence _pullFence;
		private Vk.SubmitInfo _submitInfo;

		// The size of the transfer buffer, in bytes
		public readonly uint Size;

		private readonly FifoLock _lock;

		private bool _isDisposed = false;
		#endregion // Fields

		public TransferBuffer(uint size)
		{
			Size = size;
			_commandPool = _vkDevice.CreateCommandPool(_queues.FamilyIndex, Vk.CommandPoolCreateFlags.Transient);
			_commandBuffer = _vkDevice.AllocateCommandBuffer(_commandPool, Vk.CommandBufferLevel.Primary);
			_buffer = _vkDevice.CreateBuffer(
				size: Size,
				usage: Vk.BufferUsageFlags.TransferSource | Vk.BufferUsageFlags.TransferDestination,
				sharingMode: Vk.SharingMode.Exclusive,
				queueFamilyIndices: new [] { _queues.FamilyIndex },
				flags: Vk.BufferCreateFlags.None
			);
			var req = _buffer.GetMemoryRequirements();
			_memory = _vkDevice.AllocateMemory(
				allocationSize: req.Size,
				memoryTypeIndex: Device.FindMemoryTypeIndex(req.MemoryTypeBits, Vk.MemoryPropertyFlags.HostCoherent | Vk.MemoryPropertyFlags.HostVisible).Value
			);
			_buffer.BindMemory(_memory, 0);
			_pushFence = _vkDevice.CreateFence(Vk.FenceCreateFlags.Signaled);
			_pullFence = _vkDevice.CreateFence(Vk.FenceCreateFlags.None);
			_submitInfo = new Vk.SubmitInfo { CommandBuffers = new [] { _commandBuffer } };
			_lock = new FifoLock(false);
		}
		~TransferBuffer()
		{
			dispose(false);
		}

		#region Host -> Device
		public unsafe void PushBuffer(byte* data, uint length, Vk.Buffer dstBuf, uint dstOffset)
		{
			try
			{
				_lock.Lock();

				// Number of transfer blocks
				uint bcount = (uint)Math.Ceiling((double)length / Size);

				// Transfer per-block
				for (uint bi = 0; bi < bcount; ++bi)
				{
					// Current offsets
					uint off = bi * Size;
					var src = data + off;
					var dst = dstOffset + off;
					var len = (ulong)Math.Min(length - off, Size);

					// Wait on previous transfer
					_pushFence.Wait(UInt64.MaxValue);
					_pushFence.Reset();

					// Copy the memory
					var dstptr = _memory.Map(0, len, Vk.MemoryMapFlags.None);
					Unsafe.CopyBlock(dstptr.ToPointer(), src, (uint)len);
					_memory.Unmap();

					// Record and submit
					_commandBuffer.Begin(Vk.CommandBufferUsageFlags.OneTimeSubmit);
					_commandBuffer.CopyBuffer(_buffer, dstBuf, new[] { new Vk.BufferCopy(0, dst, len) });
					_commandBuffer.End();
					_queues.Transfer.Submit(_submitInfo, _pushFence);

					// DO NOT Reset(), or else it will block the next transfer forever
					_pushFence.Wait(UInt32.MaxValue);
				}
			}
			finally
			{
				_lock.Unlock();
			}
		}
		#endregion // Host -> Device

		#region Device -> Host
		public unsafe void PullBuffer(byte* data, uint length, Vk.Buffer srcBuf, uint srcOffset)
		{
			try
			{
				_lock.Lock();

				// Number of transfer blocks
				uint bcount = (uint)Math.Ceiling((double)length / Size);

				// Transfer per-block
				for (uint bi = 0; bi < bcount; ++bi)
				{
					// Current offsets
					uint off = bi * Size;
					var dst = data + off;
					var src = srcOffset + off;
					var len = (ulong)Math.Min(length - off, Size);

					// Record and submit
					_commandBuffer.Begin(Vk.CommandBufferUsageFlags.OneTimeSubmit);
					_commandBuffer.CopyBuffer(srcBuf, _buffer, new[] { new Vk.BufferCopy(src, 0, len) });
					_commandBuffer.End();
					_queues.Transfer.Submit(_submitInfo, _pullFence);

					// Wait on previous transfer
					_pullFence.Wait(UInt64.MaxValue);
					_pullFence.Reset();

					// Copy the memory
					var srcptr = _memory.Map(0, len, Vk.MemoryMapFlags.None);
					Unsafe.CopyBlock(data, srcptr.ToPointer(), (uint)len);
					_memory.Unmap();
				}
			}
			finally
			{
				_lock.Unlock();
			}
		}
		#endregion // Device -> Host

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
				_queues.Transfer.WaitIdle();
				_pushFence.Dispose();
				_buffer.Dispose();
				_memory.Free();
				_commandPool.Dispose();
			}
			_isDisposed = true;
		}
		#endregion // IDisposable
	}
}
