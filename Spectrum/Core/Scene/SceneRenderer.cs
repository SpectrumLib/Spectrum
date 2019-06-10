using System;
using Spectrum.Graphics;
using Vk = VulkanCore;

namespace Spectrum
{
	/// <summary>
	/// Manages the logic and obejcts required to issue rendering commands in an <see cref="AppScene"/> instance.
	/// </summary>
	public sealed class SceneRenderer : IDisposable
	{
		#region Fields
		/// <summary>
		/// The scene that owns this renderer.
		/// </summary>
		public readonly AppScene Scene;
		/// <summary>
		/// A reference to the graphics device being used by this renderer.
		/// </summary>
		public GraphicsDevice Device => SpectrumApp.Instance.GraphicsDevice;

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
		private Vk.ImageMemoryBarrier _colorClearBarrier;
		private Vk.ImageMemoryBarrier _colorAttachBarrier;
		private Vk.ImageMemoryBarrier _depthClearBarrier;
		private Vk.ImageMemoryBarrier _depthAttachBarrier;

		private bool _isDisposed = false;
		#endregion // Fields

		internal SceneRenderer(AppScene scene)
		{
			Scene = scene;

			_clearPool = Device.CreateGraphicsCommandPool(true, false);
			_clearCmd = _clearPool.AllocateBuffers(new Vk.CommandBufferAllocateInfo(Vk.CommandBufferLevel.Primary, 1))[0];
			_clearFence = Device.VkDevice.CreateFence();
			_colorClearBarrier = new Vk.ImageMemoryBarrier(
				null, new Vk.ImageSubresourceRange(Vk.ImageAspects.Color, 0, 1, 0, 1), Vk.Accesses.ColorAttachmentWrite,
				Vk.Accesses.TransferRead, Vk.ImageLayout.ColorAttachmentOptimal, Vk.ImageLayout.TransferDstOptimal
			);
			_colorAttachBarrier = new Vk.ImageMemoryBarrier(
				null, new Vk.ImageSubresourceRange(Vk.ImageAspects.Color, 0, 1, 0, 1), Vk.Accesses.TransferRead,
				Vk.Accesses.ColorAttachmentWrite, Vk.ImageLayout.TransferDstOptimal, Vk.ImageLayout.ColorAttachmentOptimal
			);
			_depthClearBarrier = new Vk.ImageMemoryBarrier(
				null, new Vk.ImageSubresourceRange(Vk.ImageAspects.Depth | Vk.ImageAspects.Stencil, 0, 1, 0, 1), Vk.Accesses.DepthStencilAttachmentWrite,
				Vk.Accesses.TransferWrite, Vk.ImageLayout.DepthStencilAttachmentOptimal, Vk.ImageLayout.TransferDstOptimal
			);
			_depthAttachBarrier = new Vk.ImageMemoryBarrier(
				null, new Vk.ImageSubresourceRange(Vk.ImageAspects.Depth | Vk.ImageAspects.Stencil, 0, 1, 0, 1), Vk.Accesses.TransferWrite,
				Vk.Accesses.DepthStencilAttachmentWrite, Vk.ImageLayout.TransferDstOptimal, Vk.ImageLayout.DepthStencilAttachmentOptimal
			);
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
			_clearFence.Reset();
			Device.Queues.Graphics.Submit(new Vk.SubmitInfo(commandBuffers: new[] { _clearCmd }), _clearFence);
			_clearFence.Wait();
		}

		internal void Rebuild(uint width, uint height)
		{
			// Rebuild the targets, if needed
			if (ColorTarget == null)
			{
				ColorTarget = new RenderTarget(width, height, TexelFormat.Color, $"{Scene.Name}_Color");
				DepthTarget = new RenderTarget(width, height, TexelFormat.Depth24Stencil8, $"{Scene.Name}_Depth");
				_colorClearBarrier.Image = _colorAttachBarrier.Image = ColorTarget.VkImage;
				_depthClearBarrier.Image = _depthAttachBarrier.Image = DepthTarget.VkImage;
			}
			else if (BackbufferSize.X != width || BackbufferSize.Y != height)
			{
				ColorTarget.Rebuild(width, height);
				DepthTarget.Rebuild(width, height);
				_colorClearBarrier.Image = _colorAttachBarrier.Image = ColorTarget.VkImage;
				_depthClearBarrier.Image = _depthAttachBarrier.Image = DepthTarget.VkImage;
			}

			// Record the clear command (this will always happen, either the RTs changed, or the color changed)
			{
				_clearCmd.Begin();

				// Transition the targets to transfer dst for clearing
				_clearCmd.CmdPipelineBarrier(Vk.PipelineStages.AllCommands, Vk.PipelineStages.AllCommands,
					imageMemoryBarriers: new[] { _depthClearBarrier, _colorClearBarrier });

				// Clear the color target
				var cclear = new Vk.ClearColorValue(_clearColor.RFloat, _clearColor.GFloat, _clearColor.BFloat, _clearColor.AFloat);
				_clearCmd.CmdClearColorImage(ColorTarget.VkImage, Vk.ImageLayout.TransferDstOptimal, cclear,
					new Vk.ImageSubresourceRange(Vk.ImageAspects.Color, 0, 1, 0, 1));

				// Clear the depth target
				var dclear = new Vk.ClearDepthStencilValue(1, 0);
				_clearCmd.CmdClearDepthStencilImage(DepthTarget.VkImage, Vk.ImageLayout.TransferDstOptimal, dclear,
					new Vk.ImageSubresourceRange(Vk.ImageAspects.Depth | Vk.ImageAspects.Stencil, 0, 1, 0, 1));

				// Transition both images back to their attachment layouts
				_clearCmd.CmdPipelineBarrier(Vk.PipelineStages.AllCommands, Vk.PipelineStages.AllCommands,
					imageMemoryBarriers: new[] { _depthAttachBarrier, _colorAttachBarrier });

				_clearCmd.End();
			}
		}

		#region IDisposable
		public void Dispose()
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
			}
			_isDisposed = true;
		}
		#endregion // IDisposable
	}
}
