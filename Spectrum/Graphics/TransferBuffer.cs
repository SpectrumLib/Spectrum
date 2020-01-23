/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections;
using System.Collections.Generic;
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

				// Wait, then get the data
				Fence.Wait(UInt64.MaxValue);
				getData(dst);

				// Calculate next block values
				dst = dst.Slice((int)blen);
				srcOff += blen;
				blen = Math.Min((uint)dst.Length, SIZE);
			}
		}
		#endregion // Buffers

		#region Textures

		public void PushImage(ReadOnlySpan<byte> src, Vk.Image dst, in TextureRegion reg, 
			uint layer, uint tsize, Vk.PipelineStageFlags srcStages,
			Vk.PipelineStageFlags dstStages, Vk.ImageLayout layout, Vk.ImageAspectFlags aspects)
		{
			// Create the blocks for the image
			var blocks = new Blockifier(reg, tsize);

			// Loop over each block
			foreach (var bl in blocks.GetBlocks())
			{
				// Wait for last transfer
				Fence.Wait(UInt64.MaxValue);

				// Upload to transfer buffer
				setData(src.Slice(0, (int)bl.sz));

				// Record the transfer
				_commands.Begin(Vk.CommandBufferUsageFlags.OneTimeSubmit);
				_commands.PipelineBarrier(
					sourceStageMask: srcStages,
					destinationStageMask: Vk.PipelineStageFlags.Transfer,
					memoryBarriers: null,
					bufferMemoryBarriers: null,
					imageMemoryBarriers: new Vk.ImageMemoryBarrier {
						Image = dst,
						OldLayout = layout,
						NewLayout = Vk.ImageLayout.TransferDestinationOptimal,
						SourceAccessMask = Vk.AccessFlags.MemoryWrite,
						DestinationAccessMask = Vk.AccessFlags.TransferRead,
						SourceQueueFamilyIndex = Vk.Constants.QueueFamilyIgnored,
						DestinationQueueFamilyIndex = Vk.Constants.QueueFamilyIgnored,
						SubresourceRange = new Vk.ImageSubresourceRange(aspects, 0, 1, layer, 1)
					},
					dependencyFlags: Vk.DependencyFlags.ByRegion
				);
				_commands.CopyBufferToImage(
					sourceBuffer: _buffer,
					destinationImage: dst,
					destinationImageLayout: Vk.ImageLayout.TransferDestinationOptimal,
					regions: new Vk.BufferImageCopy(
						bufferOffset: 0,
						bufferRowLength: 0,
						bufferImageHeight: 0,
						imageSubresource: new Vk.ImageSubresourceLayers(aspects, 0, layer, 1),
						imageOffset: bl.off,
						imageExtent: bl.ext
					)
				);
				_commands.PipelineBarrier(
					sourceStageMask: Vk.PipelineStageFlags.Transfer,
					destinationStageMask: dstStages,
					memoryBarriers: null,
					bufferMemoryBarriers: null,
					imageMemoryBarriers: new Vk.ImageMemoryBarrier {
						Image = dst,
						OldLayout = Vk.ImageLayout.TransferDestinationOptimal,
						NewLayout = layout,
						SourceAccessMask = Vk.AccessFlags.TransferWrite,
						DestinationAccessMask = Vk.AccessFlags.MemoryRead,
						SourceQueueFamilyIndex = Vk.Constants.QueueFamilyIgnored,
						DestinationQueueFamilyIndex = Vk.Constants.QueueFamilyIgnored,
						SubresourceRange = new Vk.ImageSubresourceRange(aspects, 0, 1, layer, 1)
					},
					dependencyFlags: Vk.DependencyFlags.ByRegion
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
				src = src.Slice((int)bl.sz);
			}

			// Wait for the last transfer
			Fence.Wait(UInt64.MaxValue);
		}
		#endregion // Textures

		#region Host Buffer
		private unsafe void setData(ReadOnlySpan<byte> src)
		{
			uint len = Math.Min((uint)src.Length, SIZE);
			fixed (void* srcptr = src)
			{
				Unsafe.CopyBlock(_offset.ToPointer(), srcptr, len);
			}
			if (!_coherent)
			{
				Core.Instance.GraphicsDevice.VkDevice.FlushMappedMemoryRanges(
					new Vk.MappedMemoryRange { 
						Memory = _memory, 
						Offset = (uint)_offset.ToInt64(), 
						Size = len
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

		// Subdivides texture space into blocks for multiple uploads
		private readonly struct Blockifier
		{
			#region Fields
			public readonly (uint W, uint H, uint D) Step;
			public readonly TextureRegion Region;
			public readonly uint TexelSize;
			#endregion // Fields

			public Blockifier(in TextureRegion reg, uint tsize)
			{
				uint llen = reg.Width * tsize,
					 plen = reg.Width * reg.Height * tsize,
					 flen = reg.Width * reg.Height * reg.Depth * tsize;
				Step.D = (uint)Math.Max(Math.Floor((double)SIZE / plen), 1);
				Step.H = (uint)Math.Clamp(Math.Floor((double)SIZE / llen), 1, reg.Height);
				Step.W = (uint)Math.Clamp(Math.Floor((double)SIZE / tsize), 1, reg.Width);
				Region = reg;
				TexelSize = tsize;
			}

			public IEnumerable<(Vk.Offset3D off, Vk.Extent3D ext, uint sz)> GetBlocks()
			{
				for (uint dstart = 0; dstart < Region.Depth; dstart += Step.D)
				{
					uint dsize = Math.Min(Region.Depth - dstart, Step.D);
					for (uint hstart = 0; hstart < Region.Height; hstart += Step.H)
					{
						uint hsize = Math.Min(Region.Height - hstart, Step.H);
						for (uint wstart = 0; wstart < Region.Width; wstart += Step.W)
						{
							uint wsize = Math.Min(Region.Width - wstart, Step.W);
							uint blen = wsize * hsize * dsize * TexelSize;

							yield return (
								new Vk.Offset3D((int)wstart, (int)hstart, (int)dstart), 
								new Vk.Extent3D(wsize, hsize, dsize), 
								blen
							);
						}
					}
				}
			}
		}
	}
}
