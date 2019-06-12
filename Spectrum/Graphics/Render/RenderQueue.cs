using System;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Manages the batching and submission of render commands to the GPU. This is the primary type used to submit
	/// rendering commands.
	/// </summary>
	public class RenderQueue : IDisposable
	{
		// The number of command buffers to keep active at once before re-use
		private const uint BUFFER_COUNT = 10;
		private static readonly Vk.CommandBufferBeginInfo ONE_TIME_SUBMIT = new Vk.CommandBufferBeginInfo(
			Vk.CommandBufferUsages.OneTimeSubmit
		);

		#region Fields
		/// <summary>
		/// Gets if <see cref="Begin(Pipeline)"/> has been called, and the queue is currently recording draw commands.
		/// </summary>
		public bool IsRecording => _currPipeline != null;
		/// <summary>
		/// The graphics device that the queue submits to.
		/// </summary>
		public GraphicsDevice Device => SpectrumApp.Instance.GraphicsDevice;

		/// <summary>
		/// The number of <see cref="Submit"/> calls made to this queue in the current frame.
		/// </summary>
		public uint SubmitCount { get; private set; }

		// The command buffer and sync objects used to render in this instance
		private Vk.CommandPool _cmdPool;

		// The queue items and circular buffer implementation objects
		private RenderQueueItem[] _queueItems;
		private uint _queueIndex => SubmitCount % BUFFER_COUNT;
		private uint _lastIndex => (SubmitCount - 1) % BUFFER_COUNT;
		private RenderQueueItem _currentItem => _queueItems[_queueIndex];
		private RenderQueueItem _lastItem => (SubmitCount > 0) ? _queueItems[_lastIndex] : null;

		// Cached items in use by the current recording process
		private Pipeline _currPipeline = null;
		private uint _currDrawCount = 0;

		private bool _isDisposed = false;
		#endregion // Fields

		internal RenderQueue()
		{
			// Allocate the pool
			_cmdPool = Device.VkDevice.CreateCommandPool(new Vk.CommandPoolCreateInfo(
				Device.Queues.Graphics.FamilyIndex, Vk.CommandPoolCreateFlags.ResetCommandBuffer | Vk.CommandPoolCreateFlags.Transient
			));

			// Allocate the queue items
			var bufs = _cmdPool.AllocateBuffers(new Vk.CommandBufferAllocateInfo(Vk.CommandBufferLevel.Primary, (int)BUFFER_COUNT));
			_queueItems = new RenderQueueItem[BUFFER_COUNT];
			for (uint i = 0; i < BUFFER_COUNT; ++i)
			{
				_queueItems[i] = new RenderQueueItem {
					Buffer = bufs[i],
					Semaphore = Device.VkDevice.CreateSemaphore(),
					Fence = Device.VkDevice.CreateFence()
				};
			}
			SubmitCount = 0;
		}
		~RenderQueue()
		{
			dispose(false);
		}

		// Called at the beginning of a frame to reset the queue back to an unused state
		internal void Reset()
		{
			SubmitCount = 0;
		}

		#region Begin/End
		/// <summary>
		/// Prepares the queue to build a new set of rendering commands.
		/// </summary>
		/// <param name="pipeline">The pipeline that describes the rendering state to use for the draw commands.</param>
		/// <param name="viewport">The render viewport to use, or <c>null</c> to use to entire render target.</param>
		/// <param name="scissor">The render scissor to use, or <c>null</c> to use the entire render target.</param>
		public void Begin(Pipeline pipeline, Viewport? viewport = null, Scissor? scissor = null)
		{
			if (IsRecording)
				throw new InvalidOperationException("Cannot call RenderQueue.Begin() if the queue is already recording commands");
			_currPipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));

			// Wait for the current item to be available
			_currentItem.WaitAvailable();

			// Begin recording, setup the pipeline and render pass
			var buf = _currentItem.Buffer;
			buf.Begin(ONE_TIME_SUBMIT);
			buf.CmdBeginRenderPass(new Vk.RenderPassBeginInfo(pipeline.VkFramebuffer, pipeline.VkRenderPass, pipeline.DefaultScissor), 
				Vk.SubpassContents.Inline);
			buf.CmdBindPipeline(Vk.PipelineBindPoint.Graphics, pipeline.VkPipeline);

			// Because these states are dynamic, we need to specify them explicity
			_currentItem.Viewport = viewport.HasValue ? viewport.Value.ToVulkanNative() : pipeline.DefaultViewport;
			_currentItem.Scissor = scissor.HasValue ? scissor.Value.ToVulkanNative() : pipeline.DefaultScissor;
			buf.CmdSetViewport(_currentItem.Viewport);
			buf.CmdSetScissor(_currentItem.Scissor);

			// Update the tracking objects
			_currDrawCount = 0;
		}

		/// <summary>
		/// Finalizes the queued rendering commands and submits them to the GPU for processing.
		/// </summary>
		public void Submit()
		{
			if (!IsRecording)
				throw new InvalidOperationException("Cannot call RenderQueue.Submit() if the queue is not currently recording commands");

			// End the renderpass and buffer
			var buf = _currentItem.Buffer;
			buf.CmdEndRenderPass();
			buf.End();

			// Submit (with test to see if anything was actually drawn)
			if (_currDrawCount != 0)
			{
				_currentItem.Submit(Device.Queues.Graphics);
				++SubmitCount; 
			}

			_currPipeline = null;
		}
		#endregion // Begin/End

		private void waitAll()
		{
			if (SubmitCount == 0)
				return;
			foreach (var item in _queueItems)
				item.WaitAvailable();
		}

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!_isDisposed && disposing)
			{
				waitAll();

				_cmdPool.Dispose();
				foreach (var item in _queueItems)
				{
					item.Semaphore.Dispose();
					item.Fence.Dispose();
				}
			}
			_isDisposed = true;
		}
		#endregion // IDisposable

		// Contains information about a render queue item
		private class RenderQueueItem
		{
			private static readonly Vk.PipelineStages[] ALL_GRAPHICS = { Vk.PipelineStages.AllGraphics };

			// Vulkan Objects
			public Vk.CommandBuffer Buffer;
			public Vk.Semaphore Semaphore;
			public Vk.Fence Fence;

			// If the item has been submitted in this frame
			public bool IsSubmitted { get; private set; } = false;
			// If the item had to be waited on the last time WaitAvailable was called
			public bool Waited { get; private set; } = false;
			// The total number of times that the item has been submitted in its lifetime
			public uint SubmitCount { get; private set; } = 0;

			// State information
			public Vk.Viewport Viewport;
			public Vk.Rect2D Scissor;

			// Waits for the item fence to signal, checks if there is a valid fence to wait on
			public void WaitAvailable()
			{
				if (!IsSubmitted)
				{
					Waited = false;
					return;
				}
				Waited = Fence.GetStatus() != Vk.Result.Success;
				if (Waited) Fence.Wait();
				Fence.Reset();
				IsSubmitted = false;
			}

			// Submits the buffer to the pool, marks this item as in use
			public void Submit(Vk.Queue queue)
			{
				if (IsSubmitted)
					throw new InvalidOperationException("Cannot submit a queue item that is currently processing");
				queue.Submit(new Vk.SubmitInfo(waitSemaphores: (SubmitCount > 0) ? new[] { Semaphore } : null, waitDstStageMask: (SubmitCount > 0) ? ALL_GRAPHICS : null,
					commandBuffers: new[] { Buffer }, signalSemaphores: new[] { Semaphore }), Fence);
				IsSubmitted = true;
				++SubmitCount;
			}
		}
	}
}
