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
	// Used to transfer data to/from the graphics device
	internal sealed class TransferBuffer : IDisposable
	{
		public const uint SIZE = 8 * 1024 * 1024; // 8MB transfer buffer

		#region Fields
		public readonly uint Index;

		// Buffer/memory objects
		private readonly Vk.Buffer _buffer;
		private readonly Vk.DeviceMemory _memory;
		private readonly IntPtr _offset;
		private readonly bool _coherent;

		// Commanding objects
		private readonly Vk.CommandBuffer _commands;
		public readonly Vk.Fence Fence;
		#endregion // Fields

		public TransferBuffer(uint index, ThreadGraphicsObjects tgo)
		{
			Index = index;

			_buffer = tgo.TransferPool[index].Buffer;
			_memory = tgo.TransferMemory;
			_offset = new IntPtr(tgo.TransferPointer.ToInt64() + (index * SIZE));
			_coherent = tgo.CoherentTransfer;

			_commands = tgo.TransferPool[index].Commands;
			Fence = tgo.TransferPool[index].Fence;
		}

		#region Buffers
		public void PushBuffer(ReadOnlySpan<byte> src, Vk.Buffer dst, uint dstOff, 
			Vk.PipelineStageFlags srcStages, Vk.PipelineStageFlags dstStages)
		{
			// Calculate transfer values
			uint bcount = (uint)Math.Ceiling((double)src.Length / SIZE);
			uint blen = Math.Min((uint)src.Length, SIZE);

			// Loop over each block to transfer
			for (uint bi = 0; bi < bcount; ++bi)
			{
				// Wait for last transfer
				Fence.Wait(UInt64.MaxValue);

				// Upload to transfer buffer
				setData(src);

				// Record the transfer
				_commands.Begin(Vk.CommandBufferUsageFlags.OneTimeSubmit);
				_commands.PipelineBarrier(
					sourceStageMask: srcStages,
					destinationStageMask: Vk.PipelineStageFlags.Transfer,
					memoryBarriers: null,
					bufferMemoryBarriers: new Vk.BufferMemoryBarrier {
						Buffer = dst,
						Offset = dstOff,
						Size = blen,
						SourceAccessMask = Vk.AccessFlags.MemoryWrite,
						DestinationAccessMask = Vk.AccessFlags.TransferWrite
					},
					imageMemoryBarriers: null,
					dependencyFlags: Vk.DependencyFlags.None
				);
				_commands.CopyBuffer(
					sourceBuffer: _buffer,
					destinationBuffer: dst,
					regions: new Vk.BufferCopy(
						sourceOffset: 0,
						destinationOffset: dstOff,
						size: blen
					)
				);
				_commands.PipelineBarrier(
					sourceStageMask: Vk.PipelineStageFlags.Transfer,
					destinationStageMask: dstStages,
					memoryBarriers: null,
					bufferMemoryBarriers: new Vk.BufferMemoryBarrier {
						Buffer = dst,
						Offset = dstOff,
						Size = blen,
						SourceAccessMask = Vk.AccessFlags.TransferWrite,
						DestinationAccessMask = Vk.AccessFlags.MemoryRead
					},
					imageMemoryBarriers: null,
					dependencyFlags: Vk.DependencyFlags.None
				);
				_commands.End();

				// Submit
				Fence.Reset();
				Core.Instance.GraphicsDevice.Queues.Transfer.Submit(
					submits: new Vk.SubmitInfo {
						CommandBuffers = new[] { _commands },
						SignalSemaphores = null,
						WaitSemaphores = null,
						WaitDestinationStageMask = null
					},
					fence: Fence
				);

				// Calculate next block values
				src = src.Slice((int)blen);
				dstOff += blen;
				blen = Math.Min((uint)src.Length, SIZE);
			}

			// Wait for the last transfer
			Fence.Wait(UInt64.MaxValue);
		}

		public void PullBuffer(Span<byte> dst, Vk.Buffer src, uint srcOff,
			Vk.PipelineStageFlags srcStages, Vk.PipelineStageFlags dstStages)
		{
			// Calculate transfer values
			uint bcount = (uint)Math.Ceiling((double)dst.Length / SIZE);
			uint blen = Math.Min((uint)dst.Length, SIZE);

			// Loop over each block to transfer
			for (uint bi = 0; bi < bcount; ++bi)
			{
				// Record the copy
				_commands.Begin(Vk.CommandBufferUsageFlags.OneTimeSubmit);
				_commands.PipelineBarrier(
					sourceStageMask: srcStages,
					destinationStageMask: Vk.PipelineStageFlags.Transfer,
					memoryBarriers: null,
					bufferMemoryBarriers: new Vk.BufferMemoryBarrier {
						Buffer = src,
						Offset = srcOff,
						Size = blen,
						SourceAccessMask = Vk.AccessFlags.MemoryWrite,
						DestinationAccessMask = Vk.AccessFlags.TransferRead
					},
					imageMemoryBarriers: null,
					dependencyFlags: Vk.DependencyFlags.None
				);
				_commands.CopyBuffer(
					sourceBuffer: src,
					destinationBuffer: _buffer,
					regions: new Vk.BufferCopy(
						sourceOffset: srcOff,
						destinationOffset: 0,
						size: blen
					)
				);
				_commands.PipelineBarrier(
					sourceStageMask: Vk.PipelineStageFlags.Transfer,
					destinationStageMask: dstStages,
					memoryBarriers: null,
					bufferMemoryBarriers: new Vk.BufferMemoryBarrier {
						Buffer = src,
						Offset = srcOff,
						Size = blen,
						SourceAccessMask = Vk.AccessFlags.TransferRead,
						DestinationAccessMask = Vk.AccessFlags.MemoryWrite
					},
					imageMemoryBarriers: null,
					dependencyFlags: Vk.DependencyFlags.None
				);
				_commands.End();

				// Submit
				Fence.Reset();
				Core.Instance.GraphicsDevice.Queues.Transfer.Submit(
					submits: new Vk.SubmitInfo {
						CommandBuffers = new[] { _commands },
						SignalSemaphores = null,
						WaitSemaphores = null,
						WaitDestinationStageMask = null
					},
					fence: Fence
				);

				// Calculate next block values
				dst = dst.Slice((int)blen);
				srcOff += blen;
				blen = Math.Min((uint)dst.Length, SIZE);

				// Wait, then get the data
				Fence.Wait(UInt64.MaxValue);
				getData(dst);
			}
		}
		#endregion // Buffers

		#region Host Buffer
		private unsafe void setData(ReadOnlySpan<byte> src)
		{
			fixed (void* srcptr = src)
			{
				uint len = Math.Min((uint)src.Length, SIZE);
				Unsafe.CopyBlock(_offset.ToPointer(), srcptr, len);
			}
			if (!_coherent)
			{
				Core.Instance.GraphicsDevice.VkDevice.FlushMappedMemoryRanges(
					new Vk.MappedMemoryRange { 
						Memory = _memory, 
						Offset = (uint)_offset.ToInt64(), 
						Size = (uint)src.Length
					}
				);
			}
		}

		private unsafe void getData(Span<byte> dst)
		{
			fixed (void* dstptr = dst)
			{
				uint len = Math.Min((uint)dst.Length, SIZE);
				Unsafe.CopyBlock(dstptr, _offset.ToPointer(), len);
			}
		}
		#endregion // Host Buffer

		public void Wait() => Fence.Wait(UInt64.MaxValue);

		#region IDisposable
		public void Dispose()
		{
			Core.Instance.GraphicsDevice.ReleaseTransferBuffer(this);
		}
		#endregion // IDisposable
	}
}
