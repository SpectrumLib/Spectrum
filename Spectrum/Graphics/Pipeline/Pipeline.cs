using System;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Represents the total parameter set of a rendering pipeline.
	/// </summary>
	public sealed class Pipeline
	{
		#region Fields

		private bool _isDisposed = false;
		#endregion // Fields

		~Pipeline()
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
