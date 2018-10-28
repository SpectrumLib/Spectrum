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
		internal static IntPtr PrimaryMonitor => Glfw.GetPrimaryMonitor();
		// Gets the current monitor based on the window's center-point
		internal IntPtr Monitor => getCurrentMonitor(out Rect bb);

		// Gets if the monitor has been shown yet
		internal bool IsShown => (Handle != IntPtr.Zero) && (Glfw.GetWindowAttrib(Handle, Glfw.VISIBLE) == Glfw.TRUE);

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

		private Point _cachedPos = new Point(-1, -1);
		/// <summary>
		/// Gets or sets the top-left position of the window. If the window has not yet been created, setting this value
		/// to (-1, -1) will center the window on the screen when it is opened (this is default).
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

		private string _title = "Spectrum";
		/// <summary>
		/// Get or sets the title of the window, and supports UTF-8 characters. Note the title will not update until
		/// the next frame.
		/// </summary>
		public string Title
		{
			get => _title;
			set
			{
				_title = value;
				if (Handle != IntPtr.Zero) Glfw.SetWindowTitle(Handle, value);
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

			// Open the hidden window
			Handle = Glfw.CreateWindow(_cachedSize.X, _cachedSize.Y, _title);
		}

		internal void ShowWindow()
		{
			if (_cachedPos.X == -1 && _cachedPos.Y == -1)
				centerWindow(PrimaryMonitor);
			else
				Glfw.SetWindowPos(Handle, _cachedPos.X, _cachedPos.Y);

			Glfw.ShowWindow(Handle);
		}

		#region Window Control
		private void centerWindow(IntPtr monitor)
		{
			getMonitorRect(monitor, out Rect mbb);
			Point hsize = Size / 2;
			Glfw.SetWindowPos(Handle, mbb.X + (mbb.Width / 2) - hsize.X, mbb.Y + (mbb.Height / 2) - hsize.Y);
		}

		/// <summary>
		/// Centers the window on the current monitor. Note that the window will not change it's own size if it is
		/// too big to fit in the monitor.
		/// </summary>
		public void CenterWindow() => centerWindow(Monitor);
		#endregion // Window Control

		// This calcates the window's current monitor using the center-point of the window. This is needed because
		//   GLFW does not have functionaly for doing this right out of the box. It returns the GLFW handle to the
		//   current monitor, as well as the bounding box describing the monitor's position and size.
		private IntPtr getCurrentMonitor(out Rect bb)
		{
			if (!IsShown)
			{
				getMonitorRect(PrimaryMonitor, out bb);
				return PrimaryMonitor;
			}

			IntPtr[] monitors = Glfw.GetMonitors();
			Point center = Position + (Size / 2);
			for (int i = 0; i < monitors.Length; ++i)
			{
				getMonitorRect(monitors[i], out bb);
				if (bb.Contains(center))
					return monitors[i];
			}

			// Should never get here unless the window has somehow ended up far away in screen-space
			bb = Rect.Empty;
			return IntPtr.Zero;
		}

		private void getMonitorRect(IntPtr monitor, out Rect bb)
		{
			Glfw.VidMode mode = Glfw.GetVideoMode(monitor);
			Glfw.GetMonitorPos(monitor, out bb.X, out bb.Y);
			bb.Width = mode.Width;
			bb.Height = mode.Height;
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
