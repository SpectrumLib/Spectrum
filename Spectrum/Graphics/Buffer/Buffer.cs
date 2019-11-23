/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
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

		private protected unsafe void SetDataInternal(ReadOnlySpan<byte> data, uint dstOff)
		{
			using (var tb = Core.Instance.GraphicsDevice.GetTransferBuffer())
			{

			}

			throw new NotImplementedException();
		}

		private protected unsafe void GetDataInternal(Span<byte> data, uint srcOff)
		{
			using (var tb = Core.Instance.GraphicsDevice.GetTransferBuffer())
			{

			}

			throw new NotImplementedException();
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
