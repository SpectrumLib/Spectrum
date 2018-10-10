using System;

namespace Spectrum
{
	/// <summary>
	/// Main class that is extended to control the startup, shutdown, and main loop of the entire application. Only one
	/// instance of this class may exist at once.
	/// </summary>
	public abstract class Application : IDisposable
	{
		/// <summary>
		/// The current application instance.
		/// </summary>
		public static Application Instance { get; private set; } = null;

		#region Fields
		/// <summary>
		/// Gets if the application instance is disposed.
		/// </summary>
		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		/// <summary>
		/// Starts the application and performs platform and library initialization using the passed parameters.
		/// </summary>
		/// <param name="appParams">The parameters to create the application with.</param>
		protected Application(AppParameters appParams)
		{
			if (Instance != null)
				throw new InvalidOperationException("Unable to create more than once Application instance at once.");
			Instance = this;
		}
		~Application()
		{
			dispose(false);
		}

		#region IDisposable
		/// <summary>
		/// Disposes the application at the end of its execution. Performs a cleanup of all library components.
		/// Application is left unusable after this call.
		/// </summary>
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				OnDisposing(disposing);
			}

			IsDisposed = true;
			Instance = null;
		}

		/// <summary>
		/// Called when the application instance is being disposed, and before any library components are disposed. 
		/// User code should override this to perform disposal of any resources not managed by the library. This
		/// function will be called either from an implicit or explicit call to <see cref="Dispose"/>, or from
		/// the application instance being finalized, but it is guarenteed to only ever be called once.
		/// </summary>
		/// <param name="disposing">If the function is called from <see cref="Dispose"/>.</param>
		protected virtual void OnDisposing(bool disposing) { }
		#endregion // IDisposable
	}
}
