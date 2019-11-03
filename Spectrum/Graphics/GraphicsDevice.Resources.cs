/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.Threading;
using Vk = SharpVk;
using static Spectrum.InternalLog;

namespace Spectrum.Graphics
{
	// This file contains Vulkan resource management functions
	public sealed partial class GraphicsDevice : IDisposable
	{
		#region Fields
		private Dictionary<int, ThreadGraphicsObjects> _threadGraphicsObjects;
		#endregion // Fields

		#region Lifetime
		private void initializeResources()
		{
			// Create the thread graphics objects for the main thread
			_threadGraphicsObjects = new Dictionary<int, ThreadGraphicsObjects>();
			_threadGraphicsObjects.Add(Threading.MainThreadId, new ThreadGraphicsObjects(Threading.MainThreadId, this));
		}

		private void cleanupResources()
		{
			// Clean up the graphics objects for each thread
			foreach (var pair in _threadGraphicsObjects)
				pair.Value.Dispose();
			_threadGraphicsObjects.Clear();
		}
		#endregion // Lifetime

		#region Command Buffers
		// Acquires a scratch buffer from the pool of buffers on the current thread. Scratch buffers should be used
		//   only for one-off operations, such as initial layout transitions, or one time buffer copies. All scratch
		//   buffers are primary level only, and access to them is synchronized by the library.
		internal ScratchBuffer GetScratchCommandBuffer()
		{
			var tid = Thread.CurrentThread.ManagedThreadId;
			if (_threadGraphicsObjects.TryGetValue(tid, out var tgo))
			{
				var idx = tgo.NextScratchBuffer();
				return new ScratchBuffer(idx, tgo.ScratchPool[idx].Buffer, tgo.ScratchPool[idx].Fence);
			}
			else
				throw new InvalidOperationException("Attempted to acquire scratch buffer on non-graphics thread.");
		}

		// Called in Dispose() of the scratch buffer, should not be called directly
		internal void ReleaseScratchBuffer(ScratchBuffer sb)
		{
			var tid = Thread.CurrentThread.ManagedThreadId;
			if (_threadGraphicsObjects.TryGetValue(tid, out var tgo))
				tgo.ReleaseScratchBuffer(sb.Index);
			else
				throw new InvalidOperationException("Attempted to release scratch buffer on non-graphics thread.");
		}
		#endregion // Command Buffers

		// Contains the set of graphics objects that exist per-thread
		private class ThreadGraphicsObjects : IDisposable
		{
			private const uint SCRATCH_POOL_SIZE = 16; // Number of pooled scratch buffers

			#region Fields
			public readonly int ThreadId;
			public readonly Vk.CommandPool CommandPool;
			public (Vk.CommandBuffer Buffer, Vk.Fence Fence, bool Free)[] ScratchPool;
			private uint _scratchIndex;
			#endregion // Fields

			public ThreadGraphicsObjects(int tid, GraphicsDevice dev)
			{
				ThreadId = tid;
				CommandPool = dev.VkDevice.CreateCommandPool(dev.Queues.FamilyIndex,
					Vk.CommandPoolCreateFlags.ResetCommandBuffer | Vk.CommandPoolCreateFlags.Transient);

				ScratchPool = new (Vk.CommandBuffer, Vk.Fence, bool)[SCRATCH_POOL_SIZE];
				var sbufs = dev.VkDevice.AllocateCommandBuffers(CommandPool, Vk.CommandBufferLevel.Primary, SCRATCH_POOL_SIZE);
				for (uint i = 0; i < SCRATCH_POOL_SIZE; ++i)
				{
					ScratchPool[i].Buffer = sbufs[i];
					ScratchPool[i].Fence = dev.VkDevice.CreateFence(Vk.FenceCreateFlags.Signaled); // Need to start signaled
					ScratchPool[i].Free = true;
				}
				_scratchIndex = 0;

				IINFO($"Created graphics objects for thread {tid}.");
			}

			// Finds the next scratch buffer available
			public uint NextScratchBuffer()
			{
				while (true)
				{
					ref var buf = ref ScratchPool[_scratchIndex];
					// Make sure it is not in use AND has finished processing
					if (buf.Free && (buf.Fence.GetStatus() == Vk.Result.Success))
					{
						buf.Free = false;
						return _scratchIndex;
					}
					_scratchIndex = (_scratchIndex + 1) % SCRATCH_POOL_SIZE;
				}
			}

			// Releases the scratch buffer to be used again
			public void ReleaseScratchBuffer(uint index)
			{
				ref var buf = ref ScratchPool[index];
				if (buf.Free)
					throw new InvalidOperationException("Attempted to free unacquired scratch buffer (BUG IN LIBRARY).");
				buf.Free = true;
			}

			public void Dispose()
			{
				if (ScratchPool != null)
				{
					foreach (var b in ScratchPool) // Buffers get freed below
						b.Fence.Dispose();
				}

				CommandPool?.Dispose();
				IINFO($"Destroyed graphics objects for thread {ThreadId}.");
			}
		}
	}

	// Contains a reference to a pooled scratch command buffer for the current thread
	// The buffer can be disposed without waiting for the commands to finish, synchronization is done internally
	// Code using these objects **SHOULD NOT** submit the buffer manually
	// Additionally, acquiring a scratch buffer, but not submitting it, incurs additional CPU overhead for fence rebuilds
	// This type does not do much internal checking, since it is internal only
	internal class ScratchBuffer : IDisposable
	{
		#region Fields
		public readonly uint Index; // The index in the pool
		public readonly Vk.CommandBuffer Buffer;
		private readonly Vk.Fence _fence;
		#endregion // Fields

		public ScratchBuffer(uint idx, Vk.CommandBuffer buf, Vk.Fence fence)
		{
			Index = idx;
			Buffer = buf;
			_fence = fence;
		}
		~ScratchBuffer()
		{
			Core.Instance.GraphicsDevice.ReleaseScratchBuffer(this);
		}

		public void Submit(Vk.Semaphore[] waits = null, Vk.PipelineStageFlags[] stages = null, Vk.Semaphore[] signals = null)
		{
			_fence.Reset();
			Core.Instance.GraphicsDevice.Queues.Graphics.Submit(
				submits: new [] { new Vk.SubmitInfo {
					CommandBuffers = new [] { Buffer },
					WaitSemaphores = waits,
					WaitDestinationStageMask = stages,
					SignalSemaphores = signals
				}},
				_fence
			);
		}

		public void Wait() => _fence.Wait(UInt64.MaxValue);

		public void Dispose()
		{
			Core.Instance.GraphicsDevice.ReleaseScratchBuffer(this);
			GC.SuppressFinalize(this);
		}
	}
}
