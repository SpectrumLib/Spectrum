using System;
using static Spectrum.InternalLog;

namespace Spectrum
{
	// Controls the runtime of the application, event polling, frame dispatching, and platform related opertaions.
	internal class AppDriver : IDisposable
	{
		#region Fields
		public readonly SpectrumApp Application = null;

		private bool _isDisposed = false;
		#endregion // Fields

		public AppDriver(SpectrumApp app)
		{
			Application = app;

			// Report/Load the unmanaged libraries
			loadNativeLibraries();
		}
		~AppDriver()
		{
			dispose(false);
		}

		// Performs the runtime initialization
		public void Initialize()
		{
			
		}

		private void loadNativeLibraries()
		{
			foreach (var lib in NativeLoader.AvailableResources)
				LDEBUG($"Available native library: {lib}.");

			try
			{
				NativeLoader.LoadUnmanagedLibrary("glfw3", "glfw3.dll", false);
				LINFO($"Loaded native library for glfw3 (took {NativeLoader.LastLoadTime.TotalMilliseconds:.00} ms).");
			}
			catch (Exception e)
			{
				throw new Exception($"Unable to load native library glfw3, reason: {e.Message}");
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
			if (!_isDisposed)
			{
				NativeLoader.UnloadLibraries();
				_isDisposed = true;
			}
		}
		#endregion // IDisposable
	}
}
