using System;

namespace Spectrum
{
	/// <summary>
	/// Represents a window that hosts the application. Note that Spectrum always uses windowed fullscreen, and any
	/// changes to a fullscreen window only change the backbuffer size. This allows easy alt-tabbing and context 
	/// switching, while still allowing for proper emulation of fullscreen windows of different resultions.
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
		/// Gets or sets the size of the window. If the window is fullscreen, the size cannot be changed.
		/// </summary>
		public Point Size
		{
			get => _cachedSize;
			set
			{
				if (IsFullscreen) return;
				if (value.X <= 0 || value.Y <= 0)
					throw new ArgumentOutOfRangeException("Size", $"The passed window size cannot be negative or zero ({value})");
				if (Handle == IntPtr.Zero) _cachedSize = value;
				else Glfw.SetWindowSize(Handle, value.X, value.Y);
			}
		}

		private Point _cachedPos = new Point(-1, -1);
		/// <summary>
		/// Gets or sets the top-left position of the window. If the window has not yet been created, setting this value
		/// to (-1, -1) will center the window on the screen when it is opened (this is default). If the window is
		/// fullscreen, the position cannot be changed.
		/// </summary>
		public Point Position
		{
			get => _cachedPos;
			set
			{
				if (IsFullscreen) return;
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

		private bool _decorated = true;
		/// <summary>
		/// Controls if the window has decorations, like the title bar and frame. If the window is fullscreen, this
		/// value is false, and cannot be changed.
		/// </summary>
		public bool Decorated
		{
			get
			{
				if (!IsShown) return _decorated;
				else if (IsFullscreen) return false;
				else return (Glfw.GetWindowAttrib(Handle, Glfw.DECORATED) == Glfw.TRUE);
			}
			set
			{
				if (!IsShown) _decorated = value;
				else if (IsFullscreen) return;
				else Glfw.SetWindowAttrib(Handle, Glfw.DECORATED, value ? Glfw.TRUE : Glfw.FALSE);
			}
		}

		private bool _resizeable = true;
		/// <summary>
		/// Controls if the window is resizeable through the native windowing system. Note that even if this is false,
		/// the window size can still be controlled through <see cref="Size"/>. If the window is fullscreen, this value
		/// is false, and cannot be changed.
		/// </summary>
		public bool Resizeable
		{
			get
			{
				if (!IsShown) return _resizeable;
				else if (IsFullscreen) return false;
				else return (Glfw.GetWindowAttrib(Handle, Glfw.RESIZABLE) == Glfw.TRUE);
			}
			set
			{
				if (!IsShown) _resizeable = value;
				else Glfw.SetWindowAttrib(Handle, Glfw.RESIZABLE, value ? Glfw.TRUE : Glfw.FALSE);
			}
		}

		// When switching to fullscreen, this saves the window bounds to restore them to switch back to windowed mode
		private Rect _savedWindowBounds = Rect.Empty;
		/// <summary>
		/// Gets if the window is fullscreen. Use <see cref="SetFullscreen"/> to change this.
		/// </summary>
		public bool IsFullscreen { get; private set; } = false;
		#endregion // Window Parameters

		#region Events
		/// <summary>
		/// Event raised when the window changes position.
		/// </summary>
		public event WindowPositionChangedEvent PositionChanged;
		/// <summary>
		/// Event raised when the window changes size.
		/// </summary>
		public event WindowSizeChangedEvent SizeChanged;
		/// <summary>
		/// Event raised when the window enters or leaves fullscreen mode.
		/// </summary>
		public event WindowStyleChangeEvent StyleChanged;
		#endregion // Events

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
			Glfw.WindowHint(Glfw.CLIENT_API, Glfw.NO_API);
			Glfw.WindowHint(Glfw.VISIBLE, Glfw.FALSE);

			// Open the hidden window
			Handle = Glfw.CreateWindow(_cachedSize.X, _cachedSize.Y, _title);

			// Set the input callbacks
			Glfw.SetMouseButtonCallback(Handle, (window, button, action, mods) => Input.Mouse.ButtonCallback(window, button, action, mods));
			Glfw.SetScrollCallback(Handle, (window, xoffset, yoffset) => Input.Mouse.ScrollCallback(window, xoffset, yoffset));
			Glfw.SetCursorEnterCallback(Handle, (window, entered) => Input.Mouse.CursorEnterCallback(window, entered));
			Glfw.SetKeyCallback(Handle, (window, key, scancode, action, mods) => Input.Keyboard.KeyCallback(window, key, scancode, action, mods));

			// Set the window callbacks
			Glfw.SetWindowPosCallback(Handle, (window, x, y) => PositionCallback(x, y));
			Glfw.SetWindowSizeCallback(Handle, (window, w, h) => SizeCallback(w, h));

			// Set the app callbacks
			Glfw.SetWindowFocusCallback(Handle, (window, focus) => SpectrumApp.FocusChangeCallback(focus == Glfw.TRUE));
			Glfw.SetWindowIconifyCallback(Handle, (window, icon) => SpectrumApp.MinimizeCallback(icon == Glfw.TRUE));
		}

		// Occurs right before the main loop starts
		internal void ShowWindow()
		{
			Glfw.ShowWindow(Handle);

			// Set the attributes after the window is shown (this prevents a few small bugs)
			if (IsFullscreen)
			{
				_savedWindowBounds = new Rect(_cachedPos, _cachedSize);

				Glfw.SetWindowAttrib(Handle, Glfw.DECORATED, Glfw.FALSE);
				Glfw.SetWindowAttrib(Handle, Glfw.RESIZABLE, Glfw.FALSE);
				Glfw.SetWindowAttrib(Handle, Glfw.FLOATING, Glfw.TRUE);
				IntPtr monitor = PrimaryMonitor;
				getMonitorRect(monitor, out Rect mbb);
				Glfw.SetWindowPos(Handle, mbb.X, mbb.Y);
				Glfw.SetWindowSize(Handle, mbb.Width, mbb.Height);
			}
			else
			{
				Glfw.SetWindowAttrib(Handle, Glfw.DECORATED, _decorated ? Glfw.TRUE : Glfw.FALSE);
				Glfw.SetWindowAttrib(Handle, Glfw.RESIZABLE, _resizeable ? Glfw.TRUE : Glfw.FALSE);
				Glfw.SetWindowSize(Handle, _cachedSize.X, _cachedSize.Y);
				if (_cachedPos.X == -1 && _cachedPos.Y == -1)
					centerWindow(PrimaryMonitor);
				else
					Glfw.SetWindowPos(Handle, _cachedPos.X, _cachedPos.Y);
			}
		}

		#region GLFW Interop
		private static void PositionCallback(int x, int y)
		{
			var window = SpectrumApp.Instance.Window;
			var old = window._cachedPos;
			window._cachedPos = new Point(x, y);
			if (old != window._cachedPos)
				window.PositionChanged?.Invoke(new WindowPositionEventData(old, window._cachedPos));
		}

		private static void SizeCallback(int w, int h)
		{
			var window = SpectrumApp.Instance.Window;
			var old = window._cachedSize;
			window._cachedSize = new Point(w, h);
			if (old != window._cachedSize)
				window.SizeChanged?.Invoke(new WindowSizeEventData(old, window._cachedSize));
		}
		#endregion // GLFW Interop

		#region Window Control
		private void centerWindow(IntPtr monitor)
		{
			if (IsFullscreen || monitor == IntPtr.Zero)
				return;

			getMonitorRect(monitor, out Rect mbb);
			Point hsize = Size / 2;
			Glfw.SetWindowPos(Handle, mbb.X + (mbb.Width / 2) - hsize.X, mbb.Y + (mbb.Height / 2) - hsize.Y);
		}

		/// <summary>
		/// Centers the window on the current monitor. Note that the window will not change it's own size if it is
		/// too big to fit in the monitor.
		/// </summary>
		public void CenterWindow() => centerWindow(Monitor);

		/// <summary>
		/// Sets the window to either be fullscreen or windowed mode. 
		/// </summary>
		/// <param name="fullscreen">`true` to set the window to fullscreen, `false` to set to windowed mode.</param>
		/// <param name="keepRes">
		/// `true` if the current window size should be the new fullscreen resolution, `false` to set the resolution
		/// to the native monitor resolution, defaults to true. NOT CURRENTLY IMPLEMENTED.
		/// </param>
		/// <returns>If the window style was changed, false means the requested style was already active.</returns>
		public bool SetFullscreen(bool fullscreen, bool keepRes = true)
		{
			if (fullscreen == IsFullscreen)
				return false;

			// Check if the window is currently shown, otherwise it just sets the initial condition for the window
			if (IsShown)
			{
				if (fullscreen)
				{
					// Save the window parameters
					_savedWindowBounds = new Rect(Position, Size);
					_resizeable = Resizeable;
					_decorated = Decorated;

					// Fullscreen floating window
					Glfw.SetWindowAttrib(Handle, Glfw.DECORATED, Glfw.FALSE);
					Glfw.SetWindowAttrib(Handle, Glfw.RESIZABLE, Glfw.FALSE);
					Glfw.SetWindowAttrib(Handle, Glfw.FLOATING, Glfw.TRUE);
					IntPtr monitor = Monitor;
					getMonitorRect(monitor, out Rect mbb);
					Glfw.SetWindowPos(Handle, mbb.X, mbb.Y);
					Glfw.SetWindowSize(Handle, mbb.Width, mbb.Height);
					// TODO: Update the backbuffer size using the keepRes argument
				}
				else
				{
					// Restore the window parameters
					Glfw.SetWindowPos(Handle, _savedWindowBounds.X, _savedWindowBounds.Y);
					Glfw.SetWindowSize(Handle, _savedWindowBounds.Width, _savedWindowBounds.Height);
					Glfw.SetWindowAttrib(Handle, Glfw.DECORATED, _decorated ? Glfw.TRUE : Glfw.FALSE);
					Glfw.SetWindowAttrib(Handle, Glfw.RESIZABLE, _resizeable ? Glfw.TRUE : Glfw.FALSE);
					Glfw.SetWindowAttrib(Handle, Glfw.FLOATING, Glfw.FALSE);
				}

				StyleChanged?.Invoke(new WindowStyleEventData(fullscreen));
			}

			IsFullscreen = fullscreen;
			return true;
		}
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
				// Cannot call Rect.Contains, as it uses a different coordinate system than the monitors do
				if ((center.X >= bb.X) && (center.X <= (bb.X + bb.Width)) && (center.Y >= bb.Y) && (center.Y <= (bb.Y + bb.Height)))
					return monitors[i];
			}

			// Will only get here if the center of the window has been moved off of all of the monitors
			// TODO: Switch to checking the corners to allow this to still be used
			//          -or-
			//       Change to finding the monitor with the highest overlap instead of a specific point
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
