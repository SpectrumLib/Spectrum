/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Base type for all buffers that store data in GPU memory. Implements common functionality, but buffer
	/// instantiation must occur through the buffer specialization classes.
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

		/// <summary>
		/// The handle to the graphics device for this buffer
		/// </summary>
		public GraphicsDevice Device => Core.Instance.GraphicsDevice;
		// If the buffer is disposed
		protected bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		// Size in bytes, and the usages
		private protected Buffer(uint size, BufferType type, Vk.BufferUsageFlags use)
		{
			Size = size;
			Type = type;

			// Create the buffer
			VkBuffer = Device.VkDevice.CreateBuffer(
				size: size,
				usage: use | Vk.BufferUsageFlags.TransferDestination | Vk.BufferUsageFlags.TransferSource,
				sharingMode: Vk.SharingMode.Exclusive,
				queueFamilyIndices: Device.Queues.FamilyIndex,
				flags: Vk.BufferCreateFlags.None
			);

			// Create the backing memory
			var memReq = VkBuffer.GetMemoryRequirements();
			var memIdx = Device.FindMemoryTypeIndex(memReq.MemoryTypeBits, Vk.MemoryPropertyFlags.DeviceLocal);
			if (memIdx == -1)
				throw new InvalidOperationException("Cannot find a memory type that supports buffers (this means bad or out-of-date hardware)");
			VkMemory = Device.VkDevice.AllocateMemory(memReq.Size, memIdx.Value);
			VkBuffer.BindMemory(VkMemory, 0);
		}

		// Length is in array indices, start is in array indices, dstOff is in bytes
		private protected unsafe void SetDataInternal<T>(T[] data, uint length, uint start, uint dstOff)
			where T : struct
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));
			else if ((start + length) > data.Length)
				throw new ArgumentException("The source array is not large enough to supply the requested amount of data");

			uint typeSize = (uint)Unsafe.SizeOf<T>();
			uint srcLen = typeSize * length; // Bytes
			uint srcOff = typeSize * start; // Bytes

			if ((Size - dstOff) < srcLen)
				throw new ArgumentException("The buffer is not large enough to accept the source data");

			var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
			try
			{
				Device.ThisTransferBuffer.PushBuffer(
					new ReadOnlySpan<byte>(handle.AddrOfPinnedObject().ToPointer(), (int)srcLen), VkBuffer, dstOff);
			}
			finally
			{
				handle.Free();
			}
		}

		// Length is in array indices, start is in array indices, srcOff is in bytes
		private protected unsafe void GetDataInternal<T>(ref T[] data, uint length, uint start, uint srcOff)
			where T : struct
		{
			uint typeSize = (uint)Unsafe.SizeOf<T>();
			uint dstLen = typeSize * length; // Bytes
			uint dstOff = typeSize * start; // Bytes

			if (dstLen > (Size - srcOff))
				throw new ArgumentException("The buffer is not large enough to supply the requested amount of data");
			if (data == null)
				data = new T[length + start];
			else if (dstLen > ((data.Length - start) * 4))
				throw new ArgumentException("The array is not large enough to accept the buffer data");

			var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
			try
			{
				Device.ThisTransferBuffer.PullBuffer(
					new Span<byte>(handle.AddrOfPinnedObject().ToPointer(), (int)dstLen), VkBuffer, srcOff);
			}
			finally
			{
				handle.Free();
			}
		}

		#region IDisposble
		public void Dispose()
		{
			if (!IsDisposed)
				Dispose(true);
			IsDisposed = true;
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
	}

	/// <summary>
	/// Describes how a buffer is used by the graphics device.
	/// </summary>
	public enum BufferType
	{
		/// <summary>
		/// The buffer is used to source vertex attribute data for use in shaders.
		/// </summary>
		Vertex,
		/// <summary>
		/// The buffer is used to source index data when performing indexed rendering.
		/// </summary>
		Index,
		/// <summary>
		/// The buffer is used to store general structured memory, which shaders have read and store access to, as well
		/// as atomic operations on buffer members that are unsigned integers.
		/// </summary>
		Storage
	}
}
