using System;

namespace Spectrum
{
	/// <summary>
	/// Represents a window that hosts the application.
	/// </summary>
	public class AppWindow : IDisposable
	{
		/// <summary>
		/// The default window size.
		/// </summary>
		public static readonly Point DEFAULT_SIZE = new Point(1280, 720);

		#region Fields
		internal readonly SpectrumApp Application;

		internal readonly IntPtr Handle;

		private bool _isDisposed = false;
		#endregion // Fields

		internal AppWindow(SpectrumApp app)
		{
			Application = app;

			// Prepare the window hints
			Glfw.WindowHint(Glfw.RESIZABLE, Glfw.FALSE);
			//Glfw.WindowHint(Glfw.VISIBLE, Glfw.FALSE);
			Glfw.WindowHint(Glfw.DECORATED, Glfw.TRUE);
			Glfw.WindowHint(Glfw.CLIENT_API, Glfw.NO_API);

			// Open the hidden window
			Handle = Glfw.CreateWindow(DEFAULT_SIZE.X, DEFAULT_SIZE.Y, "Spectrum");
		}
		~AppWindow()
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
				Glfw.DestroyWindow(Handle);
			}
			_isDisposed = true;
		}
		#endregion // IDisposable
	}
}
