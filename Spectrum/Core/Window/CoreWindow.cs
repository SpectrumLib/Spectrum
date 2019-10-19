/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Linq;

namespace Spectrum
{
	/// <summary>
	/// Implements operations and queries about the core window that hosts the application graphics and input.
	/// Fullscreen is implemented using windowed fullscreen, instead of true fullscreen, and changing the fullscreen
	/// size simply changes the backbuffer size.
	/// </summary>
	public class CoreWindow : IDisposable
	{
		/// <summary>
		/// The default window size.
		/// </summary>
		public static readonly Extent DEFAULT_SIZE = new Extent(1280, 720);

		#region Fields
		// The glfw library instance in use by this window.
		internal readonly Glfw3 Glfw;
		// The handle of the glfw window
		internal IntPtr Handle { get; private set; } = IntPtr.Zero;

		// Gets the primary monitor for the system.
		internal IntPtr PrimaryMonitor => Glfw.GetPrimaryMonitor();
		// Gets the current monitor based on the window's center-point
		internal IntPtr Monitor => getCurrentMonitor(out _);

		// Gets if the monitor has been shown yet
		internal bool IsShown => (Handle != IntPtr.Zero) && (Glfw.GetWindowAttrib(Handle, Glfw3.VISIBLE) == Glfw3.TRUE);

		// Gets if the window should close from an operating system event
		internal bool ShouldClose => (Handle != IntPtr.Zero) && Glfw.WindowShouldClose(Handle);

		#region Window Parameters
		// Window parameters come with cached parameters, which are used to store user changes to the window before the
		//    window is opened. After the window has been opened, the parameters should be set and queried directly
		//    from and to the existing window. The cached values are also used to save information when switching
		//    between windowed and fullscreen modes.

		/// <summary>
		/// Gets or sets the size of the window. If the window is fullscreen, the size cannot be changed.
		/// </summary>
		public Extent Size
		{
			get => _cachedSize;
			set
			{
				if (IsFullscreen) return;
				if (Handle == IntPtr.Zero) _cachedSize = value;
				else Glfw.SetWindowSize(Handle, (int)value.Width, (int)value.Height);
			}
		}
		private Extent _cachedSize = DEFAULT_SIZE;

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
		private Point _cachedPos = new Point(-1, -1);

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
		private string _title = "Spectrum";

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
				else return (Glfw.GetWindowAttrib(Handle, Glfw3.DECORATED) == Glfw3.TRUE);
			}
			set
			{
				if (!IsShown) _decorated = value;
				else if (IsFullscreen) return;
				else Glfw.SetWindowAttrib(Handle, Glfw3.DECORATED, value ? Glfw3.TRUE : Glfw3.FALSE);
			}
		}
		private bool _decorated = true;

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
				else return (Glfw.GetWindowAttrib(Handle, Glfw3.RESIZABLE) == Glfw3.TRUE);
			}
			set
			{
				if (!IsShown) _resizeable = value;
				else Glfw.SetWindowAttrib(Handle, Glfw3.RESIZABLE, value ? Glfw3.TRUE : Glfw3.FALSE);
			}
		}
		private bool _resizeable = true;

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
		#endregion // Fields

		internal CoreWindow()
		{
			Glfw = new Glfw3();
			_title = Core.Instance.Name;
		}

		#region Window Lifetime
		// This occurs after user code is called, to allow the user to set the initial window parameters
		internal void CreateWindow()
		{
			if (!Glfw.Init())
			{
				var err = Glfw.LastError;
				throw new Exception($"Unable to initialize GLFW, error (0x{err.code:X}): {err.desc}.");
			}
			if (!Glfw.VulkanSupported())
				throw new Exception("Vulkan runtime not found on the system.");

			// Prepare the window hints
			Glfw.WindowHint(Glfw3.CLIENT_API, Glfw3.NO_API);
			Glfw.WindowHint(Glfw3.VISIBLE, Glfw3.FALSE);

			// Open the hidden window
			Handle = Glfw.CreateWindow((int)_cachedSize.Width, (int)_cachedSize.Height, _title);
			if (Handle == IntPtr.Zero)
			{
				var err = Glfw.LastError;
				throw new Exception($"Unable to create window, error (0x{err.code:X}): {err.desc}.");
			}

			// Set the input callbacks
			Glfw.SetMouseButtonCallback(Handle, (window, button, action, mods) => Input.Mouse.ButtonCallback(window, button, action, mods));
			Glfw.SetScrollCallback(Handle, (window, xoffset, yoffset) => Input.Mouse.ScrollCallback(window, xoffset, yoffset));
			Glfw.SetCursorEnterCallback(Handle, (window, entered) => Input.Mouse.CursorEnterCallback(window, entered));
			Glfw.SetKeyCallback(Handle, (window, key, scancode, action, mods) => Input.Keyboard.KeyCallback(window, key, scancode, action, mods));

			// Set the window callbacks
			Glfw.SetWindowPosCallback(Handle, (window, x, y) => PositionCallback(x, y));
			Glfw.SetWindowSizeCallback(Handle, (window, w, h) => SizeCallback(w, h));

			// Set the app callbacks
			Glfw.SetWindowFocusCallback(Handle, (window, focus) => Core.Instance.DoFocusChange(focus == Glfw3.TRUE));
			Glfw.SetWindowIconifyCallback(Handle, (window, icon) => Core.Instance.DoMinimize(icon == Glfw3.TRUE));
		}

		// Occurs right before the main loop starts
		internal void ShowWindow()
		{
			Glfw.ShowWindow(Handle);

			// Set the attributes after the window is shown (this prevents a few small bugs)
			if (IsFullscreen)
			{
				_savedWindowBounds = new Rect(_cachedPos, _cachedSize);

				Glfw.SetWindowAttrib(Handle, Glfw3.DECORATED, Glfw3.FALSE);
				Glfw.SetWindowAttrib(Handle, Glfw3.RESIZABLE, Glfw3.FALSE);
				Glfw.SetWindowAttrib(Handle, Glfw3.FLOATING, Glfw3.TRUE);
				IntPtr monitor = PrimaryMonitor;
				getMonitorRect(monitor, out Rect mbb);
				Glfw.SetWindowPos(Handle, mbb.X, mbb.Y);
				Glfw.SetWindowSize(Handle, (int)mbb.Width, (int)mbb.Height);
			}
			else
			{
				Glfw.SetWindowAttrib(Handle, Glfw3.DECORATED, _decorated ? Glfw3.TRUE : Glfw3.FALSE);
				Glfw.SetWindowAttrib(Handle, Glfw3.RESIZABLE, _resizeable ? Glfw3.TRUE : Glfw3.FALSE);
				Glfw.SetWindowSize(Handle, (int)_cachedSize.Width, (int)_cachedSize.Height);
				if (_cachedPos.X == -1 && _cachedPos.Y == -1)
					centerWindow(PrimaryMonitor);
				else
					Glfw.SetWindowPos(Handle, _cachedPos.X, _cachedPos.Y);
			}
		}

		// Prepares the input classes for a new frame, and reads pending OS messages to the window
		internal void PumpEvents()
		{
			Input.Mouse.NewFrame();
			Input.Keyboard.NewFrame();
			Glfw.PollEvents();
			Input.Mouse.FireEvents();
			Input.Keyboard.FireEvents();
		}
		#endregion // Window Lifetime

		#region GLFW Interop
		private static void PositionCallback(int x, int y)
		{
			var window = Core.Instance.Window;
			var old = window._cachedPos;
			window._cachedPos = new Point(x, y);
			if (old != window._cachedPos)
				window.PositionChanged?.Invoke(new WindowPositionEventData(old, window._cachedPos));
		}

		private static void SizeCallback(int w, int h)
		{
			var window = Core.Instance.Window;
			var old = window._cachedSize;
			window._cachedSize = new Extent((uint)w, (uint)h);
			if (old != window._cachedSize)
			{
				Core.Instance.DoBackbufferSizeChange((uint)w, (uint)h);
				window.SizeChanged?.Invoke(new WindowSizeEventData(old, window._cachedSize));
			}
		}
		#endregion // GLFW Interop

		#region Window Control
		private void centerWindow(IntPtr monitor)
		{
			if (IsFullscreen || monitor == IntPtr.Zero)
				return;

			getMonitorRect(monitor, out Rect mbb);
			Extent hsize = Size / 2;
			Glfw.SetWindowPos(Handle, (int)(mbb.X + (mbb.Width / 2) - hsize.Width), (int)(mbb.Y + (mbb.Height / 2) - hsize.Height));
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
					Glfw.SetWindowAttrib(Handle, Glfw3.DECORATED, Glfw3.FALSE);
					Glfw.SetWindowAttrib(Handle, Glfw3.RESIZABLE, Glfw3.FALSE);
					Glfw.SetWindowAttrib(Handle, Glfw3.FLOATING, Glfw3.TRUE);
					IntPtr monitor = Monitor;
					getMonitorRect(monitor, out Rect mbb);
					Glfw.SetWindowPos(Handle, mbb.X, mbb.Y);
					Glfw.SetWindowSize(Handle, (int)mbb.Width, (int)mbb.Height);
				}
				else
				{
					// Restore the window parameters
					Glfw.SetWindowPos(Handle, _savedWindowBounds.X, _savedWindowBounds.Y);
					Glfw.SetWindowSize(Handle, (int)_savedWindowBounds.Width, (int)_savedWindowBounds.Height);
					Glfw.SetWindowAttrib(Handle, Glfw3.DECORATED, _decorated ? Glfw3.TRUE : Glfw3.FALSE);
					Glfw.SetWindowAttrib(Handle, Glfw3.RESIZABLE, _resizeable ? Glfw3.TRUE : Glfw3.FALSE);
					Glfw.SetWindowAttrib(Handle, Glfw3.FLOATING, Glfw3.FALSE);
				}

				StyleChanged?.Invoke(new WindowStyleEventData(fullscreen));
			}

			IsFullscreen = fullscreen;
			return true;
		}
		#endregion // Window Control

		#region Monitor Functions
		// This calcates the window's current monitor using the center-point of the window. This is needed because
		//   GLFW does not have functionaly for doing this right out of the box. It returns the GLFW handle to the
		//   current monitor, as well as the bounding box describing the monitor's position and size.
		// Cannot use Rect class because of different coordinate systems
		private IntPtr getCurrentMonitor(out Rect bb)
		{
			// Return IntPtr.Zero if the window is not yet shown
			if (!IsShown)
			{
				bb = Rect.Empty;
				return IntPtr.Zero;
			}

			// Get the monitor rects
			var monitors = Glfw.GetMonitors();
			var rmon = monitors.Select(m => { getMonitorRect(m, out var mr); return mr; }).ToArray();

			// Check for the monitor that has the window center point
			var c = Position + (Point)(Size / 2);
			for (int i = 0; i < rmon.Length; ++i)
			{
				if (rmon[i].Contains(c))
				{
					bb = rmon[i];
					return monitors[i];
				}
			}

			// Will only get here if the center of the window has been moved off of all of the monitors
			// Simply find the monitor that has the most overlap
			int maxi = -1;
			uint maxa = 0;
			var srect = new Rect(Position, Size);
			for (int i = 0; i < rmon.Length; ++i)
			{
				Rect.Intersect(srect, rmon[i], out var inter);
				if (inter.Area > maxa)
				{
					maxi = i;
					maxa = inter.Area;
				}
			}

			bb = (maxi != -1) ? rmon[maxi] : Rect.Empty;
			return (maxi != -1) ? monitors[maxi] : IntPtr.Zero;
		}

		private void getMonitorRect(IntPtr monitor, out Rect bb)
		{
			Glfw3.VidMode mode = Glfw.GetVideoMode(monitor);
			Glfw.GetMonitorPos(monitor, out bb.X, out bb.Y);
			bb.Width = (uint)mode.Width;
			bb.Height = (uint)mode.Height;
		}
		#endregion // Monitor Functions

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (Handle != IntPtr.Zero)
			{
				Glfw.DestroyWindow(Handle);
				Handle = IntPtr.Zero;
			}
			if (disposing)
			{
				Glfw.Terminate();
				Glfw.Dispose();
			}
		}
		#endregion // IDisposable
	}
}
