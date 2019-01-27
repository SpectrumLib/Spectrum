using System;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Fully defines the attachments, shaders, states, and output of a rendering pipeline, with the ability to split
	/// rendering up into multiple subpasses. Rendering cannot happen without a valid RenderPass instance bound.
	/// </summary>
	public sealed class RenderPass : IDisposable
	{
		#region Fields
		/// <summary>
		/// The name of the render pass, used for debugging and identification.
		/// </summary>
		public readonly string Name;

		private bool _isDisposed = false;
		#endregion // Fields

		internal RenderPass(string name)
		{
			Name = name;
		}
		~RenderPass()
		{
			dispose(false);
		}

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!_isDisposed)
			{

			}
			_isDisposed = true;
		}
		#endregion // IDisposable
	}
}
