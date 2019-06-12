using System;
using Spectrum.Graphics;
using Vk = VulkanCore;

namespace Spectrum
{
	/// <summary>
	/// Manages the logic and obejcts required to issue rendering commands in an <see cref="AppScene"/> instance.
	/// </summary>
	public sealed class SceneRenderer : RenderQueue
	{
		#region Fields
		/// <summary>
		/// The scene that owns this renderer.
		/// </summary>
		public readonly AppScene Scene;
		/// <summary>
		/// A reference to the graphics device being used by this renderer.
		/// </summary>
		public new GraphicsDevice Device => SpectrumApp.Instance.GraphicsDevice;

		/// <summary>
		/// The size of the render target for this renderer.
		/// </summary>
		public Point BackbufferSize => ColorTarget?.Size ?? Point.Zero;

		private Color _clearColor = Color.Black;
		/// <summary>
		/// The color to clear the backbuffer to each frame.
		/// </summary>
		public Color ClearColor
		{
			get => _clearColor;
			set
			{
				if (_clearColor != value)
				{
					_clearColor = value;
					Rebuild((uint)BackbufferSize.X, (uint)BackbufferSize.Y); // This will only re-record the clear command, which is cheap 
				}
			}
		}

		/// <summary>
		/// The default color render target for the scene renderer. This is what is displayed to the screen at the end
		/// of every frame. It is automatically resized when the window size changes.
		/// </summary>
		public RenderTarget ColorTarget { get; private set; } = null;
		/// <summary>
		/// The default depth/stencil render target for the scene renderer. This holds the depth/stencil information
		/// for the scene geometry. It is automatically resized when the window size changes.
		/// </summary>
		public RenderTarget DepthTarget { get; private set; } = null;

		// Target clear objects
		private readonly Vk.CommandPool _clearPool;
		private readonly Vk.CommandBuffer _clearCmd;
		private readonly Vk.Fence _clearFence;

		private bool _isDisposed = false;
		#endregion // Fields

		internal SceneRenderer(AppScene scene)
		{
			Scene = scene;

			_clearPool = Device.CreateGraphicsCommandPool(true, false);
			_clearCmd = _clearPool.AllocateBuffers(new Vk.CommandBufferAllocateInfo(Vk.CommandBufferLevel.Primary, 1))[0];
			_clearFence = Device.VkDevice.CreateFence();
		}
		~SceneRenderer()
		{
			dispose(false);
		}

		/// <summary>
		/// Clears the default color and depth targets for this renderer. Happens automatically at the beginning of
		/// each frame, but can be called manually if needed.
		/// </summary>
		public void Clear()
		{
			// Submit and wait for the clear commands
			// Using a combined pre-recorded command buffer is faster than individually calling Clear() on the targets
			_clearFence.Reset();
			Device.Queues.Graphics.Submit(new Vk.SubmitInfo(commandBuffers: new[] { _clearCmd }), _clearFence);
			_clearFence.Wait();
		}

		// Called at the beginning of the frame to reset components of the scene renderer
		internal new void Reset()
		{
			Clear();
			base.Reset();
		}

		// Rebuilds the render targets and commands
		internal void Rebuild(uint width, uint height)
		{
			// Rebuild the targets, if needed
			if (ColorTarget == null)
			{
				ColorTarget = new RenderTarget(width, height, TexelFormat.Color, $"{Scene.Name}_Color");
				DepthTarget = new RenderTarget(width, height, TexelFormat.Depth24Stencil8, $"{Scene.Name}_Depth");
			}
			else if (BackbufferSize.X != width || BackbufferSize.Y != height)
			{
				ColorTarget.Rebuild(width, height);
				DepthTarget.Rebuild(width, height);
			}

			// Record the clear command (this will always happen, either the RTs changed, or the color changed)
			{
				_clearCmd.Begin();

				// Transition the targets to transfer dst for clearing
				_clearCmd.CmdPipelineBarrier(Vk.PipelineStages.AllCommands, Vk.PipelineStages.AllCommands,
					imageMemoryBarriers: new[] { DepthTarget.ClearBarrier, ColorTarget.ClearBarrier });

				// Clear the color target
				var cclear = new Vk.ClearColorValue(_clearColor.RFloat, _clearColor.GFloat, _clearColor.BFloat, _clearColor.AFloat);
				_clearCmd.CmdClearColorImage(ColorTarget.VkImage, Vk.ImageLayout.TransferDstOptimal, cclear,
					new Vk.ImageSubresourceRange(ColorTarget.VkAspects, 0, 1, 0, 1));

				// Clear the depth target
				var dclear = new Vk.ClearDepthStencilValue(1, 0);
				_clearCmd.CmdClearDepthStencilImage(DepthTarget.VkImage, Vk.ImageLayout.TransferDstOptimal, dclear,
					new Vk.ImageSubresourceRange(DepthTarget.VkAspects, 0, 1, 0, 1));

				// Transition both images back to their attachment layouts
				_clearCmd.CmdPipelineBarrier(Vk.PipelineStages.AllCommands, Vk.PipelineStages.AllCommands,
					imageMemoryBarriers: new[] { DepthTarget.AttachBarrier, ColorTarget.AttachBarrier });

				_clearCmd.End();
			}
		}

		#region IDisposable
		public new void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (disposing && !_isDisposed)
			{
				Device.Queues.Graphics.WaitIdle();
				_clearFence.Dispose();
				_clearPool.Dispose();
				ColorTarget?.Dispose();
				DepthTarget?.Dispose();
				base.Dispose();
			}
			_isDisposed = true;
		}
		#endregion // IDisposable
	}
}
