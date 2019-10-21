/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using Spectrum.Graphics;
using System;
using Vk = SharpVk;

namespace Spectrum
{
	/// <summary>
	/// Manages the objects, states, and logic for issuing rendering commands in a <see cref="Scene"/> instance.
	/// </summary>
	public sealed class SceneRenderer : IDisposable
	{
		#region Fields
		/// <summary>
		/// The scene that owns this renderer.
		/// </summary>
		public readonly Scene Scene;
		/// <summary>
		/// A reference to the graphics device being used by this renderer.
		/// </summary>
		public GraphicsDevice Device => Core.Instance.GraphicsDevice;

		/// <summary>
		/// The size of the render target for this renderer.
		/// </summary>
		public Extent BackbufferSize => ColorTarget?.Size ?? Extent.Zero;

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
					Rebuild(BackbufferSize.Width, BackbufferSize.Height); // This will only re-record the clear command, which is cheap 
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

		internal SceneRenderer(Scene scene)
		{
			Scene = scene;

			_clearPool = Device.VkDevice.CreateCommandPool(Device.Queues.FamilyIndex, Vk.CommandPoolCreateFlags.ResetCommandBuffer);
			_clearCmd = Device.VkDevice.AllocateCommandBuffer(_clearPool, Vk.CommandBufferLevel.Primary);
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
			_clearFence.Reset();
			Device.Queues.Graphics.Submit(submits: new[] { new Vk.SubmitInfo {
				CommandBuffers = new[] { _clearCmd }
			}}, _clearFence);
			_clearFence.Wait(UInt64.MaxValue);
		}

		// Called at the beginning of the frame to reset components of the scene renderer
		internal void Reset()
		{
			Clear();
			//base.Reset();
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
			else if (BackbufferSize.Width != width || BackbufferSize.Height != height)
			{
				ColorTarget.Rebuild(width, height);
				DepthTarget.Rebuild(width, height);
			}

			// Record the clear command (this will always happen, either the RTs changed, or the color changed)
			{
				_clearCmd.Begin();

				// Transition the targets to transfer dst for clearing
				_clearCmd.PipelineBarrier(
					sourceStageMask: Vk.PipelineStageFlags.AllGraphics,
					destinationStageMask: Vk.PipelineStageFlags.AllGraphics,
					memoryBarriers: null,
					bufferMemoryBarriers: null,
					imageMemoryBarriers: new [] { DepthTarget.ClearBarrier, ColorTarget.ClearBarrier }
				);

				// Clear the color target
				var cclear = new Vk.ClearColorValue(_clearColor.RFloat, _clearColor.GFloat, _clearColor.BFloat, _clearColor.AFloat);
				_clearCmd.ClearColorImage(
					image: ColorTarget.VkImage,
					imageLayout: Vk.ImageLayout.TransferDestinationOptimal,
					color: cclear,
					ranges: new [] { new Vk.ImageSubresourceRange(ColorTarget.VkAspects, 0, 1, 0, 1) }
				);

				// Clear the depth target
				var dclear = new Vk.ClearDepthStencilValue(1, 0);
				_clearCmd.ClearDepthStencilImage(
					image: DepthTarget.VkImage,
					imageLayout: Vk.ImageLayout.TransferDestinationOptimal,
					depthStencil: dclear,
					ranges: new [] { new Vk.ImageSubresourceRange(DepthTarget.VkAspects, 0, 1, 0, 1) }
				);

				// Transition both images back to their attachment layouts
				_clearCmd.PipelineBarrier(
					sourceStageMask: Vk.PipelineStageFlags.AllGraphics,
					destinationStageMask: Vk.PipelineStageFlags.AllGraphics,
					memoryBarriers: null,
					bufferMemoryBarriers: null,
					imageMemoryBarriers: new [] { DepthTarget.AttachBarrier, ColorTarget.AttachBarrier }
				);

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
				//base.Dispose();
			}
			_isDisposed = true;
		}
		#endregion // IDisposable
	}
}
