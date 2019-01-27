using System;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Fully defines the attachments, shaders, states, and output of a rendering pipeline, with the ability to split
	/// rendering up into multiple subpasses. Rendering cannot happen without a valid RenderPass instance bound.
	/// <para>
	/// RenderPass construction needs to happen in a very specific order, using the <c>Add...()</c> functions to
	/// build up a full description of the RenderPass, and then calling <see cref="Build"/>. Creating a render pass
	/// is very verbose and time consuming, but the tradeoff is a fully defined rendering state that can be strongly
	/// optimized by the driver when it is created, and quickly and efficiently bound at runtime with minimal
	/// backend work.
	/// </para>
	/// </summary>
	public sealed class RenderPass : IDisposable
	{
		#region Fields
		/// <summary>
		/// If the instance has been built using <see cref="Build"/> and is ready for use in rendering.
		/// </summary>
		public bool Prepared { get; private set; } = false;

		private bool _isDisposed = false;
		#endregion // Fields

		public RenderPass()
		{

		}
		~RenderPass()
		{
			dispose(false);
		}

		/// <summary>
		/// Fully constructs the renderpass, all of its subpasses, allocates texture resources as necessary, and
		/// binds pipelines, shaders, and attachments.
		/// </summary>
		public void Build()
		{
			if (Prepared)
				return;

			Prepared = true;
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
