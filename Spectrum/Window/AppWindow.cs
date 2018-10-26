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

		internal IntPtr Handle { get; private set; } = IntPtr.Zero;

		// Gets the primary monitor for the system.
		internal IntPtr PrimaryMonitor => Glfw.GetPrimaryMonitor();

		#region Window Parameters
		// Window parameters come with cached parameters, which are used to store user changes to the window before the
		//    window is opened. After the window has been opened, the parameters should be set and queried directly
		//    from and to the existing window. The cached values are also used to save information when switching
		//    between windowed and fullscreen modes.

		private Point _cachedSize = DEFAULT_SIZE;
		/// <summary>
		/// Gets or sets the client area of the window (the area with rendering, ignoring the decoration).
		/// </summary>
		public Point Size
		{
			get
			{
				if (Handle == IntPtr.Zero) return _cachedSize;
				else { Point p; Glfw.GetWindowSize(Handle, out p.X, out p.Y); return p; }
			}
			set
			{
				if (value.X <= 0 || value.Y <= 0)
					throw new ArgumentOutOfRangeException("Size", $"The passed window size cannot be negative or zero ({value})");
				if (Handle == IntPtr.Zero) _cachedSize = value;
				else Glfw.SetWindowSize(Handle, value.X, value.Y);
			}
		}

		private Point _cachedPos = Point.Zero;
		/// <summary>
		/// Gets or sets the top-left position of the window. If the window has not yet been created, setting this value
		/// to (0, 0) will center the window on the screen when it is opened (this is default).
		/// </summary>
		public Point Position
		{
			get
			{
				if (Handle == IntPtr.Zero) return _cachedPos;
				else { Point p; Glfw.GetWindowPos(Handle, out p.X, out p.Y); return p; }
			}
			set
			{
				if (Handle == IntPtr.Zero) _cachedPos = value;
				else Glfw.SetWindowPos(Handle, value.X, value.Y);
			}
		}
		#endregion // Window Parameters

		private bool _isDisposed = false;
		#endregion // Fields

		internal AppWindow(SpectrumApp app)
		{
			Application = app;
		}
		~AppWindow()
		{
			dispose(false);
		}

		// This occurs after user code is called, to allow the user to set the initial window parameters
		internal void CreateWindow()
		{
			// Prepare the window hints
			Glfw.WindowHint(Glfw.RESIZABLE, Glfw.FALSE);
			Glfw.WindowHint(Glfw.VISIBLE, Glfw.FALSE);
			Glfw.WindowHint(Glfw.DECORATED, Glfw.TRUE);
			Glfw.WindowHint(Glfw.CLIENT_API, Glfw.NO_API);

			// Open the hidden window and position it
			Handle = Glfw.CreateWindow(_cachedSize.X, _cachedSize.Y, "Spectrum");
			// TODO: Position window after creation
		}

		internal void ShowWindow()
		{
			Glfw.ShowWindow(Handle);
		}

		#region Window Control
		#endregion // Window Control

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
