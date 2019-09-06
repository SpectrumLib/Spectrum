using System;

namespace Spectrum
{
	/// <summary>
	/// The central type that manages the runtime and core objects of a Spectrum application. At minimum, a Spectrum
	/// application requires an active instance of a child of this type to be runnable. Only one instance can exist
	/// at any one time.
	/// </summary>
	public abstract class Core : IDisposable
	{
		/// <summary>
		/// A reference to the active singleton instance of this type, if one exists. This reference can be used to
		/// safely get the instances of other core types in the current application.
		/// </summary>
		public static Core Instance { get; private set; } = null;

		#region Fields
		private bool _isDisposed = false;
		#endregion // Fields

		protected Core()
		{
			if (Instance != null)
				throw new InvalidOperationException("Cannot create more than one instance of a Core type at one time.");
			Instance = this;
		}
		~Core()
		{
			dispose(false);
		}

		#region Dispose
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

			Instance = null;
			_isDisposed = true;
		}
		#endregion // Dispose
	}
}
