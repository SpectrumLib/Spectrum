using System;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	// Maintains a monolithic buffer on the graphics device that is used to source shader uniform data
	// Each pipeline instance is given part of this buffer to use for its shader, and the shader uploads
	//   information into their portion to be sourced as uniforms.
	// Memory allocation is managed on the host, and is performed in pages
	internal static class ShaderBuffer
	{
		private const uint BUFFER_SIZE = 32768; // 32KB for the global uniform buffer (keep this a multiple of MAX_SHADER_SIZE)
		private const uint PAGE_SIZE = 128; // 128 bytes per memory page (keep this a divisor of MAX_SHADER_SIZE)
		private const uint PAGE_COUNT = BUFFER_SIZE / PAGE_SIZE;
		private const uint MAX_SHADER_SIZE = 1024; // Maximum of 1Kb for any one shader (used to size the staging buffer)
		private const uint MAX_PAGE_COUNT = MAX_SHADER_SIZE / PAGE_SIZE; 

		#region Fields
		// Device local buffer
		internal static Vk.Buffer DeviceBuffer { get; private set; } = null;
		private static Vk.DeviceMemory s_deviceMemory;
		// Host local staging buffer
		private static Vk.Buffer s_stagingBuffer;
		private static Vk.DeviceMemory s_stagingMemory;
		#endregion // Fields

		// Called at the beginning of the program to set up the buffer
		public static void CreateResources()
		{
			var dev = SpectrumApp.Instance.GraphicsDevice;

			// Allocate the device buffer
			var bci = new Vk.BufferCreateInfo(
				BUFFER_SIZE,
				Vk.BufferUsages.TransferDst | Vk.BufferUsages.UniformBuffer,
				flags: Vk.BufferCreateFlags.None,
				sharingMode: Vk.SharingMode.Exclusive
			);
			DeviceBuffer = dev.VkDevice.CreateBuffer(bci);
			var memr = DeviceBuffer.GetMemoryRequirements();
			var memi = dev.FindMemoryTypeIndex(memr.MemoryTypeBits, Vk.MemoryProperties.DeviceLocal);
			if (memi == -1)
				throw new InvalidOperationException("There is no memory type that supports uniform buffers.");
			var mai = new Vk.MemoryAllocateInfo(memr.Size, memi);
			s_deviceMemory = dev.VkDevice.AllocateMemory(mai);
			DeviceBuffer.BindMemory(s_deviceMemory);

			// Allocate the staging buffer
			bci = new Vk.BufferCreateInfo(
				MAX_SHADER_SIZE,
				Vk.BufferUsages.TransferSrc,
				flags: Vk.BufferCreateFlags.None,
				sharingMode: Vk.SharingMode.Exclusive
			);
			s_stagingBuffer = dev.VkDevice.CreateBuffer(bci);
			memr = s_stagingBuffer.GetMemoryRequirements();
			memi = dev.FindMemoryTypeIndex(memr.MemoryTypeBits, Vk.MemoryProperties.HostVisible | Vk.MemoryProperties.HostCoherent);
			if (memi == -1)
				throw new InvalidOperationException("There is no memory type that supports host staging buffers.");
			mai = new Vk.MemoryAllocateInfo(memr.Size, memi);
			s_stagingMemory = dev.VkDevice.AllocateMemory(mai);
			s_stagingBuffer.BindMemory(s_stagingMemory);
		}

		// Called at the end of the program to release the resources
		public static void Cleanup()
		{
			DeviceBuffer.Dispose();
			s_stagingBuffer.Dispose();
			s_deviceMemory.Dispose();
			s_stagingMemory.Dispose();
		}
	}
}
