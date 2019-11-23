/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	// Contains a reference to a pooled scratch command buffer for the current thread
	// The buffer can be disposed without waiting for the commands to finish, synchronization is done internally
	// Code using these objects **SHOULD NOT** submit the buffer manually
	// Additionally, acquiring a scratch buffer, but not submitting it, incurs additional CPU overhead for fence rebuilds
	// This type does not do much internal checking, since it is internal only
	internal sealed class ScratchBuffer : IDisposable
	{
		#region Fields
		public readonly uint Index; // The index in the pool
		public readonly Vk.CommandBuffer Buffer;
		private readonly Vk.Fence _fence;
		#endregion // Fields

		public ScratchBuffer(uint idx, ThreadGraphicsObjects tgo)
		{
			Index = idx;
			Buffer = tgo.ScratchPool[idx].Buffer;
			_fence = tgo.ScratchPool[idx].Fence;
		}

		public void Submit(Vk.Semaphore[] waits = null, Vk.PipelineStageFlags[] stages = null, Vk.Semaphore[] signals = null)
		{
			_fence.Reset();
			Core.Instance.GraphicsDevice.Queues.Graphics.Submit(
				submits: new[] { new Vk.SubmitInfo {
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
		}
	}
}
