using System;
using Spectrum.Graphics;

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

		private bool _isDisposed = false;
		#endregion // Fields

		internal SceneRenderer(AppScene scene)
		{
			Scene = scene;	
		}
		~SceneRenderer()
		{
			dispose(false);
		}

		internal void Rebuild(uint width, uint height)
		{
			// Dispose the old ones
			ColorTarget?.Dispose();
			DepthTarget?.Dispose();

			// Create new targets
			ColorTarget = new RenderTarget(width, height, TexelFormat.Color, $"{Scene.Name}_Color");
			DepthTarget = new RenderTarget(width, height, TexelFormat.Depth24Stencil8, $"{Scene.Name}_Depth");
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
				ColorTarget?.Dispose();
				DepthTarget?.Dispose();
			}
			_isDisposed = true;
		}
		#endregion // IDisposable
	}
}
