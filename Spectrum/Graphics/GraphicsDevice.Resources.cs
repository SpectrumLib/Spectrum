/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.Threading;
using Vk = SharpVk;

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
		// Acquires a scratch buffer from the pool of buffers. Scratch buffers should be used only for one-off operations, 
		//   such as initial layout transitions, or one time buffer copies. All scratch buffers are primary level only, 
		//   and access to them is synchronized by the library.
		internal ScratchBuffer GetScratchCommandBuffer()
		{
			var tid = Thread.CurrentThread.ManagedThreadId;
			if (_threadGraphicsObjects.TryGetValue(tid, out var tgo))
			{
				var idx = tgo.NextScratchBuffer();
				return new ScratchBuffer(idx, tgo);
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

		// Creates a long-lived (non-scratch) primary command buffer for use on the calling thread only
		internal Vk.CommandBuffer CreatePrimaryCommandBuffer()
		{
			var tid = Thread.CurrentThread.ManagedThreadId;
			if (_threadGraphicsObjects.TryGetValue(tid, out var tgo))
				return VkDevice.AllocateCommandBuffer(tgo.CommandPool, Vk.CommandBufferLevel.Primary);
			else
				throw new InvalidOperationException("Attempted to allocate command buffer on non-graphics thread.");
		}

		// Creates a long-lived (non-scratch) secondary command buffer for use on the calling thread only
		internal Vk.CommandBuffer CreateSecondaryCommandBuffer()
		{
			var tid = Thread.CurrentThread.ManagedThreadId;
			if (_threadGraphicsObjects.TryGetValue(tid, out var tgo))
				return VkDevice.AllocateCommandBuffer(tgo.CommandPool, Vk.CommandBufferLevel.Secondary);
			else
				throw new InvalidOperationException("Attempted to allocate command buffer on non-graphics thread.");
		}

		// Called to free a command buffer made with CreatePrimary...() or CreateSecondary...()
		internal void FreeCommandBuffer(Vk.CommandBuffer cb)
		{
			var tid = Thread.CurrentThread.ManagedThreadId;
			if (_threadGraphicsObjects.TryGetValue(tid, out var tgo))
				tgo.CommandPool.FreeCommandBuffers(cb);
			else
				throw new InvalidOperationException("Attempted to free command buffer on non-graphics thread.");
		}
		#endregion // Command Buffers

		#region Transfer Buffers
		// Acquires a transfer buffer from the pool of buffers
		internal TransferBuffer GetTransferBuffer()
		{
			var tid = Thread.CurrentThread.ManagedThreadId;
			if (_threadGraphicsObjects.TryGetValue(tid, out var tgo))
				return new TransferBuffer(tgo.NextTransferBuffer(), tgo);
			else
				throw new InvalidOperationException("Attempted to acquire transfer buffer on non-graphics thread.");
		}

		internal void ReleaseTransferBuffer(TransferBuffer tb)
		{
			var tid = Thread.CurrentThread.ManagedThreadId;
			if (_threadGraphicsObjects.TryGetValue(tid, out var tgo))
				tgo.ReleaseTransferBuffer(tb.Index);
			else
				throw new InvalidOperationException("Attempted to release transfer buffer on non-graphics thread.");
		}
		#endregion // Transfer Buffers
	}
}
