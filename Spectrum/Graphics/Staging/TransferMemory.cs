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
	// Represents a reserved region of the staging buffer, and operations on that region
	internal class TransferMemory : IDisposable
	{
		#region Fields
		public readonly uint Offset; // In bytes
		public readonly uint Size;   // In bytes

		private readonly Vk.Fence _fence;
		private readonly Vk.CommandBuffer _cmd;
		private readonly Vk.SubmitInfo _submitInfo;

		private GraphicsDevice _device => Core.Instance.GraphicsDevice;

		private bool _isReserved = true;
		#endregion // Fields

		public TransferMemory(uint off, uint sz, Vk.Fence fence, Vk.CommandBuffer cb)
		{
			Offset = off;
			Size = sz;
			_fence = fence;
			_cmd = cb;
			_submitInfo = new Vk.SubmitInfo { CommandBuffers = new [] { _cmd } };
		}

		#region Push
		public unsafe void PushBuffer(byte* data, uint length, Vk.Buffer dstBuf, uint dstOffset)
		{
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

				// Copy the memory
				Unsafe.CopyBlock((byte*)StagingBuffer.MemoryPtr.ToPointer() + Offset, src, (uint)len);

				// Record and submit
				_cmd.Begin(Vk.CommandBufferUsageFlags.OneTimeSubmit);
				_cmd.CopyBuffer(StagingBuffer.Buffer, dstBuf, new [] { new Vk.BufferCopy(Offset, dst, len) });
				_cmd.End();
				_fence.Reset();
				_device.Queues.Transfer.Submit(_submitInfo, _fence);

				// DO NOT Reset(), or else it will block the next transfer forever
				_fence.Wait(UInt32.MaxValue);
			}

			// Leave the fence in the unsignaled state
			_fence.Reset();
		}
		#endregion // Push

		#region Pull
		public unsafe void PullBuffer(byte* data, uint length, Vk.Buffer srcBuf, uint srcOffset)
		{
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
				_cmd.Begin(Vk.CommandBufferUsageFlags.OneTimeSubmit);
				_cmd.CopyBuffer(srcBuf, StagingBuffer.Buffer, new [] { new Vk.BufferCopy(src, Offset, len) });
				_cmd.End();
				_fence.Reset();
				_device.Queues.Transfer.Submit(_submitInfo, _fence);

				// Wait on transfer
				_fence.Wait(UInt64.MaxValue);

				// Copy the memory
				Unsafe.CopyBlock(dst, (byte*)StagingBuffer.MemoryPtr.ToPointer() + Offset, (uint)len);
			}

			// Leave the fence in the unsignaled state
			_fence.Reset();
		}
		#endregion // Pull

		#region Free
		~TransferMemory()
		{
			Dispose();
			GC.SuppressFinalize(this);
		}

		public void Dispose()
		{
			if (_isReserved)
			{
				StagingBuffer.Free(Offset, _cmd);
				_fence.Dispose();
			}
			_isReserved = false;
		}
		#endregion // Free
	}
}
