using System;
using Spectrum.Utilities;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	// Contains internal resource management functions
	public sealed partial class GraphicsDevice
	{
		#region Fields
		// General-use graphics command pool for one-off (scratch) queue submissions (like setting images to their initial layout)
		private Vk.CommandPool _scratchPool;
		private Vk.Fence _scratchFence;
		private Vk.CommandBufferAllocateInfo _scratchAllocInfo;
		private Vk.CommandBufferBeginInfo _scratchBeginInfo;

		private Texture2D _tex;
		#endregion // Fields

		// Initializes the various graphics resources found throughout the library
		internal void InitializeResources()
		{
			// Scratch command pool
			var pci = new Vk.CommandPoolCreateInfo(Queues.Graphics.FamilyIndex, Vk.CommandPoolCreateFlags.Transient | Vk.CommandPoolCreateFlags.ResetCommandBuffer);
			_scratchPool = VkDevice.CreateCommandPool(pci);
			_scratchAllocInfo = new Vk.CommandBufferAllocateInfo(Vk.CommandBufferLevel.Primary, 1);
			_scratchBeginInfo = new Vk.CommandBufferBeginInfo(Vk.CommandBufferUsages.OneTimeSubmit, null);
			_scratchFence = VkDevice.CreateFence();

			// Resources throughout the library
			TransferBuffer.CreateResources();

			_tex = new Texture2D(4, 4);
			_tex.SetData(new Color[] {
				Color.Blue, Color.Green, Color.Blue, Color.Green,
				Color.Green, Color.Blue, Color.Green, Color.Blue,
				Color.Green, Color.Blue, Color.Green, Color.Blue,
				Color.Blue, Color.Green, Color.Blue, Color.Green
			});

			// TEMPORARY
			var pool = VkDevice.CreateCommandPool(new Vk.CommandPoolCreateInfo(Queues.Graphics.FamilyIndex, Vk.CommandPoolCreateFlags.Transient | Vk.CommandPoolCreateFlags.ResetCommandBuffer));
			var buff = pool.AllocateBuffers(new Vk.CommandBufferAllocateInfo(Vk.CommandBufferLevel.Primary, 1))[0];
			buff.Begin();
			buff.CmdPipelineBarrier(Vk.PipelineStages.AllCommands, Vk.PipelineStages.AllCommands, imageMemoryBarriers: new[] { new Vk.ImageMemoryBarrier(
				_tex.VkImage, new Vk.ImageSubresourceRange(Vk.ImageAspects.Color, 0, 1, 0, 1), Vk.Accesses.None, Vk.Accesses.TransferRead,
				Vk.ImageLayout.Undefined, Vk.ImageLayout.TransferSrcOptimal
			)});
			buff.End();
			var fence = VkDevice.CreateFence();
			Queues.Graphics.Submit(null, Vk.PipelineStages.AllCommands, buff, null, fence);
			fence.Wait();
			fence.Reset();
			fence.Dispose();
			pool.Dispose();
		}

		private void cleanResources()
		{
			// GraphicsDevice internal resources
			_scratchPool.Dispose();
			_scratchFence.Dispose();

			_tex.Dispose();

			// Resources scattered thorughout the library
			Sampler.Samplers.ForEach(pair => pair.Value.Dispose());
			TransferBuffer.Cleanup();
		}

		// Finds the best type of memory for the given constraints
		// TODO: In the future, we will probably cache the best indices for all common property flag combinations,
		//       and check against that and make sure the memory types are valid, before performing the expensive
		//       calculation to find the best
		internal int FindMemoryTypeIndex(int bits, Vk.MemoryProperties props)
		{
			int? index = null;
			Memory.MemoryTypes.ForEach((type, idx) => {
				// If: (not already found) AND (valid memory type) AND (all required properties are present)
				if (!index.HasValue && (bits & (0x1 << idx)) > 0 && (type.PropertyFlags & props) == props)
				{
					index = idx;
				}
			});
			return index.HasValue ? index.Value : -1;
		}

		// Submits a one-time action that needs a graphics queue command buffer, will be synchronous
		// The command buffer will already have Begin() called when it is passed to the action, and will automatically call End()
		internal void SubmitScratchCommand(Action<Vk.CommandBuffer> action, Vk.Semaphore waitSem = null, Vk.PipelineStages waitStages = Vk.PipelineStages.AllGraphics, 
			Vk.Fence waitFence = null)
		{
			// Record the command
			var cb = _scratchPool.AllocateBuffers(_scratchAllocInfo)[0];
			cb.Begin(_scratchBeginInfo);
			action(cb);
			cb.End();

			// Submit
			if (waitFence != null)
			{
				waitFence.Wait();
				waitFence.Reset();
			}
			Queues.Graphics.Submit(waitSem, waitStages, cb, null, _scratchFence);
			_scratchFence.Wait();
			_scratchFence.Reset();
		}
	}
}