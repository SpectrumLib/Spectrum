using System;
using Vk = VulkanCore;

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

		// The handle to the graphics device for this buffer
		protected readonly GraphicsDevice Device;
		// If the buffer is disposed
		protected bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		// Size in bytes, and the usages
		private protected Buffer(uint size, BufferType type)
		{
			Device = SpectrumApp.Instance.GraphicsDevice;
			Size = size;
			Type = type;

			// Create the buffer
			var bci = new Vk.BufferCreateInfo(
				size,
				Vk.BufferUsages.TransferDst | (Vk.BufferUsages)type,
				flags: Vk.BufferCreateFlags.None,
				sharingMode: Vk.SharingMode.Exclusive
			);
			VkBuffer = Device.VkDevice.CreateBuffer(bci);

			// Create the backing memory
			var memReq = VkBuffer.GetMemoryRequirements();
			var memIdx = Device.FindMemoryTypeIndex(memReq.MemoryTypeBits, Vk.MemoryProperties.DeviceLocal);
			if (memIdx == -1)
				throw new InvalidOperationException("Cannot find a memory type that supports buffers (this means bad or out-of-date hardware)");
			var mai = new Vk.MemoryAllocateInfo(memReq.Size, memIdx);
			VkMemory = Device.VkDevice.AllocateMemory(mai);
			VkBuffer.BindMemory(VkMemory);
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
				VkMemory?.Dispose();
			}
		}
		#endregion // IDisposable
	}

	/// <summary>
	/// Describes how a buffer is used by the graphics device.
	/// </summary>
	public enum BufferType
	{

	}
}
