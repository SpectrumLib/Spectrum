/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	// Management of internal resources
	public sealed partial class GraphicsDevice : IDisposable
	{
		#region Fields
		// General-use graphics command pool for one-off (scratch) queue submissions (like setting images to their initial layout)
		private Vk.CommandPool _scratchPool;
		private Vk.CommandBuffer _scratchBuffer;
		private Vk.Fence _scratchFence;
		#endregion // Fields

		// Initializes the various graphics resources found throughout the library
		private void initializeResources()
		{
			// Scratch command pool
			_scratchPool = VkDevice.CreateCommandPool(Queues.FamilyIndex, Vk.CommandPoolCreateFlags.ResetCommandBuffer | Vk.CommandPoolCreateFlags.Transient);
			_scratchBuffer = VkDevice.AllocateCommandBuffer(_scratchPool, Vk.CommandBufferLevel.Primary);
			_scratchFence = VkDevice.CreateFence();

			// Resources throughout the library
			// TransferBuffer.CreateResources();
		}

		private void cleanResources()
		{
			// GraphicsDevice internal resources
			_scratchPool.Dispose();
			_scratchFence.Dispose();

			// Resources scattered thorughout the library
			//Sampler.Samplers.ForEach(pair => pair.Value.Dispose());
			//TransferBuffer.Cleanup();
		}

		// Finds the best type of memory for the given constraints
		internal uint? FindMemoryTypeIndex(uint bits, Vk.MemoryPropertyFlags props)
		{
			// TODO: Cache the results for faster lookups
			uint? index = null;
			Memory.MemoryTypes.ForEach((type, idx) => {
				// If: (not already found) AND (valid memory type) AND (all required properties are present)
				if (!index.HasValue && (bits & (0x1 << idx)) > 0 && (type.PropertyFlags & props) == props)
					index = (uint)idx;
			});
			return index;
		}

		// Submits a one-time action that needs a graphics queue command buffer, will be synchronous
		// The command buffer will already have Begin() called when it is passed to the action, and will automatically call End()
		internal void SubmitScratchCommand(Action<Vk.CommandBuffer> action, Vk.Semaphore waitSem = null, Vk.PipelineStageFlags waitStages = Vk.PipelineStageFlags.AllGraphics,
			Vk.Fence waitFence = null)
		{
			// Record the command
			_scratchBuffer.Begin(Vk.CommandBufferUsageFlags.OneTimeSubmit);
			action(_scratchBuffer);
			_scratchBuffer.End();

			// Submit
			if (waitFence != null)
			{
				waitFence.Wait(UInt64.MaxValue);
				waitFence.Reset();
			}
			Queues.Graphics.Submit(new [] { new Vk.SubmitInfo { 
				CommandBuffers = new [] { _scratchBuffer }, 
				WaitSemaphores = waitSem != null ? new [] { waitSem } : null, 
				WaitDestinationStageMask = new [] { waitStages } } 
			}, _scratchFence);

			// Wait for completion
			_scratchFence.Wait(UInt64.MaxValue);
			_scratchFence.Reset();
		}
	}
}
