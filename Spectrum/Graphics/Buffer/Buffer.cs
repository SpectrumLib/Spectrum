/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Base type for all buffers that store data in GPU memory. Implements common functionality, but cannot be
	/// instantiated directly.
	/// </summary>
	public abstract class Buffer : IDisposable
	{
		#region Fields
		/// <summary>
		/// Size of the buffer data, in bytes.
		/// </summary>
		public readonly uint Size;
		/// <summary>
		/// The type of this buffer.
		/// </summary>
		public readonly BufferType Type;

		// Vulkan objects
		internal readonly Vk.Buffer VkBuffer;
		internal readonly Vk.DeviceMemory VkMemory;

		// If the buffer is disposed
		protected bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		// Size in bytes, and the usages
		private protected Buffer(uint size, BufferType type, Vk.BufferUsageFlags usages)
		{
			var dev = Core.Instance.GraphicsDevice;
			Size = size;
			Type = type;

			// Create the buffer
			VkBuffer = dev.VkDevice.CreateBuffer(
				size: size,
				usage: Vk.BufferUsageFlags.TransferDestination | usages,
				sharingMode: Vk.SharingMode.Exclusive,
				queueFamilyIndices: Vk.Constants.QueueFamilyIgnored,
				flags: Vk.BufferCreateFlags.None
			);

			// Create the backing memory
			var memReq = VkBuffer.GetMemoryRequirements();
			var memIdx = dev.Memory.Find(memReq.MemoryTypeBits, Vk.MemoryPropertyFlags.DeviceLocal);
			if (!memIdx.HasValue)
				throw new InvalidOperationException("Device does not support buffer memory.");
			VkMemory = dev.VkDevice.AllocateMemory(
				allocationSize: memReq.Size,
				memoryTypeIndex: memIdx.Value
			);
			VkBuffer.BindMemory(VkMemory, 0);
		}
		~Buffer()
		{
			Dispose(false);
		}

		// Synchronous
		private protected unsafe void SetDataInternal(ReadOnlySpan<byte> data, uint dstOff)
		{
			// Check sizes
			if (dstOff >= Size)
				throw new ArgumentException("Transfer offset is outside of buffer range.");
			if (data.Length > (Size - dstOff))
				throw new ArgumentException("Source data too large for buffer transfer.");

			// Make the transfer
			using (var tb = Core.Instance.GraphicsDevice.GetTransferBuffer())
			{
				GetTransferStages(Type, out var sstage, out var dstage);
				tb.PushBuffer(data, VkBuffer, dstOff, sstage, dstage);
			}
		}

		// Asynchronous
		private protected Task SetDataInternalAsync<T>(ReadOnlyMemory<T> data, uint dstOff)
			where T : struct
		{
			// Check sizes
			if (dstOff >= Size)
				throw new ArgumentException("Transfer offset is outside of buffer range.");
			if ((data.Length * Unsafe.SizeOf<T>()) > (Size - dstOff))
				throw new ArgumentException("Source data too large for buffer transfer.");

			// Make the transfer
			var tb = Core.Instance.GraphicsDevice.GetTransferBuffer();
			return Task.Run(() => {
				using (tb)
				{
					GetTransferStages(Type, out var sstage, out var dstage);
					tb.PushBuffer(MemoryMarshal.AsBytes(data.Span), VkBuffer, dstOff, sstage, dstage);
				}
			});
		}

		// Synchronous
		private protected unsafe void GetDataInternal(Span<byte> data, uint srcOff)
		{
			// Check sizes
			if (srcOff >= Size)
				throw new ArgumentException("Transfer offset is outside of buffer range.");
			if (data.Length > (Size - srcOff))
				throw new ArgumentException("Buffer too small for requested buffer transfer.");

			// Make the transfer
			using (var tb = Core.Instance.GraphicsDevice.GetTransferBuffer())
			{
				GetTransferStages(Type, out var sstage, out var dstage);
				tb.PullBuffer(data, VkBuffer, srcOff, sstage, dstage);
			}
		}

		// Asynchronous
		private protected Task GetDataInternalAsync<T>(Memory<T> data, uint srcOff)
			where T : struct
		{
			// Check sizes
			if (srcOff >= Size)
				throw new ArgumentException("Transfer offset is outside of buffer range.");
			if ((data.Length * Unsafe.SizeOf<T>()) > (Size - srcOff))
				throw new ArgumentException("Buffer too small for requested buffer transfer.");

			// Make the transfer
			var tb = Core.Instance.GraphicsDevice.GetTransferBuffer();
			return Task.Run(() => {
				using (tb)
				{
					GetTransferStages(Type, out var sstage, out var dstage);
					tb.PullBuffer(MemoryMarshal.AsBytes(data.Span), VkBuffer, srcOff, sstage, dstage);
				}
			});
		}

		#region IDisposble
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		// ALWAYS call base.Dispose(disposing)
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				VkBuffer.Dispose();
				VkMemory?.Free();
			}
		}
		#endregion // IDisposable

		private static void GetTransferStages(BufferType typ, out Vk.PipelineStageFlags src, out Vk.PipelineStageFlags dst)
		{
			switch (typ)
			{
				case BufferType.Vertex:
				case BufferType.Index:
					src = Vk.PipelineStageFlags.VertexShader;
					dst = Vk.PipelineStageFlags.VertexInput;
					break;
				case BufferType.Storage:
					src = Vk.PipelineStageFlags.FragmentShader;
					dst = Vk.PipelineStageFlags.VertexShader;
					break;
				default:
					throw new ArgumentException("Invalid buffer type (library bug).");
			}
		}
	}

	/// <summary>
	/// Describes the type of data stored by a <see cref="Buffer"/>.
	/// </summary>
	public enum BufferType
	{
		/// <summary>
		/// Vertex attribute data for use in shaders.
		/// </summary>
		Vertex,
		/// <summary>
		/// Index data when performing indexed rendering.
		/// </summary>
		Index,
		/// <summary>
		/// General structured memory, which shaders have read and store access to.
		/// </summary>
		Storage
	}
}
