/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Spectrum
{
	internal partial class Glfw3 : IDisposable
	{
		#region Fields
		private IntPtr _handle = IntPtr.Zero;

		private int _lastErrorCode = Glfw3.NO_ERROR;
		private string _lastErrorDesc = String.Empty;
		// A tuple of the last error code and description, which clears the error values
		public (int code, string desc) LastError
		{
			get {
				var last = (_lastErrorCode, _lastErrorDesc);
				_lastErrorCode = Glfw3.NO_ERROR;
				_lastErrorDesc = String.Empty;
				return last;
			}
		}

		// Gets if there is an error
		public bool HasError => _lastErrorCode != Glfw3.NO_ERROR;

		// Gets time it took to load the library and functions
		public readonly TimeSpan LoadTime;
		#endregion // Fields

		// Loads the library and function pointers
		public Glfw3()
		{
			Stopwatch timer = Stopwatch.StartNew();

			_handle = Native.NativeLoader.LoadLibrary("glfw3", "libglfw.so.3");

			// Check the version
			_glfwGetVersion = loadFunc<Delegates.glfwGetVersion>();
			_glfwGetVersion(out var vmaj, out var vmin, out var vrev);
			if (vmaj < 3 || vmin < 3)
				throw new PlatformNotSupportedException($"Spectrum requires GLFW 3.3 or greater ({vmaj}.{vmin}.{vrev} found).");

			// Load the functions
			_glfwInit = loadFunc<Delegates.glfwInit>();
			_glfwTerminate = loadFunc<Delegates.glfwTerminate>();
			_glfwSetErrorCallback = loadFunc<Delegates.glfwSetErrorCallback>();
			_glfwWindowHint = loadFunc<Delegates.glfwWindowHint>();
			_glfwCreateWindow = loadFunc<Delegates.glfwCreateWindow>();
			_glfwDestroyWindow = loadFunc<Delegates.glfwDestroyWindow>();
			_glfwWindowShouldClose = loadFunc<Delegates.glfwWindowShouldClose>();
			_glfwPollEvents = loadFunc<Delegates.glfwPollEvents>();
			_glfwShowWindow = loadFunc<Delegates.glfwShowWindow>();
			_glfwVulkanSupported = loadFunc<Delegates.glfwVulkanSupported>();
			_glfwGetWindowAttrib = loadFunc<Delegates.glfwGetWindowAttrib>();
			_glfwSetWindowAttrib = loadFunc<Delegates.glfwSetWindowAttrib>();
			_glfwGetWindowSize = loadFunc<Delegates.glfwGetWindowSize>();
			_glfwSetWindowSize = loadFunc<Delegates.glfwSetWindowSize>();
			_glfwGetWindowPos = loadFunc<Delegates.glfwGetWindowPos>();
			_glfwSetWindowPos = loadFunc<Delegates.glfwSetWindowPos>();
			_glfwGetPrimaryMonitor = loadFunc<Delegates.glfwGetPrimaryMonitor>();
			_glfwGetMonitors = loadFunc<Delegates.glfwGetMonitors>();
			_glfwGetMonitorPos = loadFunc<Delegates.glfwGetMonitorPos>();
			_glfwGetVideoModes = loadFunc<Delegates.glfwGetVideoModes>();
			_glfwGetVideoMode = loadFunc<Delegates.glfwGetVideoMode>();
			_glfwSetWindowTitle = loadFunc<Delegates.glfwSetWindowTitle>();
			_glfwSetMouseButtonCallback = loadFunc<Delegates.glfwSetMouseButtonCallback>();
			_glfwSetScrollCallback = loadFunc<Delegates.glfwSetScrollCallback>();
			_glfwSetKeyCallback = loadFunc<Delegates.glfwSetKeyCallback>();
			_glfwGetCursorPos = loadFunc<Delegates.glfwGetCursorPos>();
			_glfwSetInputMode = loadFunc<Delegates.glfwSetInputMode>();
			_glfwSetCursorEnterCallback = loadFunc<Delegates.glfwSetCursorEnterCallback>();
			_glfwSetWindowPosCallback = loadFunc<Delegates.glfwSetWindowPosCallback>();
			_glfwSetWindowSizeCallback = loadFunc<Delegates.glfwSetWindowSizeCallback>();
			_glfwSetWindowFocusCallback = loadFunc<Delegates.glfwSetWindowFocusCallback>();
			_glfwSetWindowIconifyCallback = loadFunc<Delegates.glfwSetWindowIconifyCallback>();
			_glfwGetPhysicalDevicePresentationSupport = loadFunc<Delegates.glfwGetPhysicalDevicePresentationSupport>();
			_glfwCreateWindowSurface = loadFunc<Delegates.glfwCreateWindowSurface>();

			// Set the error callback
			_glfwSetErrorCallback((code, desc) => {
				_lastErrorCode = code;
				_lastErrorDesc = desc;
			});

			LoadTime = timer.Elapsed;
		}
		~Glfw3()
		{
			Dispose();
		}

		#region Passthrough API Functions
		public bool Init() => (_glfwInit() == Glfw3.TRUE);
		public void Terminate() => _glfwTerminate();
		public void WindowHint(int hint, int value) => _glfwWindowHint(hint, value);
		public void DestroyWindow(IntPtr window) => _glfwDestroyWindow(window);
		public bool WindowShouldClose(IntPtr window) => (_glfwWindowShouldClose(window) == Glfw3.TRUE);
		public void PollEvents() => _glfwPollEvents();
		public void ShowWindow(IntPtr window) => _glfwShowWindow(window);
		public bool VulkanSupported() => (_glfwVulkanSupported() == Glfw3.TRUE);
		public int GetWindowAttrib(IntPtr window, int attrib) => _glfwGetWindowAttrib(window, attrib);
		public void SetWindowAttrib(IntPtr window, int attrib, int value) => _glfwSetWindowAttrib(window, attrib, value);
		public void GetWindowSize(IntPtr window, out int w, out int h) => _glfwGetWindowSize(window, out w, out h);
		public void SetWindowSize(IntPtr window, int w, int h) => _glfwSetWindowSize(window, w, h);
		public void GetWindowPos(IntPtr window, out int x, out int y) => _glfwGetWindowPos(window, out x, out y);
		public void SetWindowPos(IntPtr window, int x, int y) => _glfwSetWindowPos(window, x, y);
		public IntPtr GetPrimaryMonitor() => _glfwGetPrimaryMonitor();
		public void GetMonitorPos(IntPtr monitor, out int x, out int y) => _glfwGetMonitorPos(monitor, out x, out y);
		public void SetMouseButtonCallback(IntPtr window, GLFWmousebuttonfun mouse_button_callback)
			=> _glfwSetMouseButtonCallback(window, mouse_button_callback);
		public void SetScrollCallback(IntPtr window, GLFWscrollfun scroll_callback)
			=> _glfwSetScrollCallback(window, scroll_callback);
		public void SetKeyCallback(IntPtr window, GLFWkeyfun key_callback) => _glfwSetKeyCallback(window, key_callback);
		public void GetCursorPos(IntPtr window, out double x, out double y) => _glfwGetCursorPos(window, out x, out y);
		public void SetInputMode(IntPtr window, int mode, int value) => _glfwSetInputMode(window, mode, value);
		public void SetCursorEnterCallback(IntPtr window, Glfwcursorenterfun func) => _glfwSetCursorEnterCallback(window, func);
		public void SetWindowPosCallback(IntPtr window, GLFWwindowposfun func) => _glfwSetWindowPosCallback(window, func);
		public void SetWindowSizeCallback(IntPtr window, GLFWwindowsizefun func) => _glfwSetWindowSizeCallback(window, func);
		public void SetWindowFocusCallback(IntPtr window, GLFWwindowfocusfun func) => _glfwSetWindowFocusCallback(window, func);
		public void SetWindowIconifyCallback(IntPtr window, GLFWwindowiconifyfun func) => _glfwSetWindowIconifyCallback(window, func);
		// TODO: Re-add when vulkan is linked in
		//public bool GetPhysicalDevicePresentationSupport(Vk.Instance inst, Vk.PhysicalDevice dev, uint fam)
		//	=> (_glfwGetPhysicalDevicePresentationSupport(inst.Handle, dev.Handle, fam) == 1);
		#endregion // Passthrough API Functions

		#region API Function Wrappers
		public unsafe IntPtr CreateWindow(int width, int height, string title)
		{
			byte[] tstr = Encoding.UTF8.GetBytes(title + '\0');

			fixed (byte* tptr = tstr)
			{
				return _glfwCreateWindow(width, height, (IntPtr)tptr, IntPtr.Zero, IntPtr.Zero);
			}
		}

		public IntPtr[] GetMonitors()
		{
			IntPtr mptr = _glfwGetMonitors(out int count);

			IntPtr[] mons = new IntPtr[count];
			for (int i = 0; i < count; ++i, mptr += IntPtr.Size)
				mons[i] = Marshal.ReadIntPtr(mptr);
			return mons;
		}

		public VidMode[] GetVideoModes(IntPtr monitor)
		{
			IntPtr mptr = _glfwGetVideoModes(monitor, out int count);

			VidMode[] modes = new VidMode[count];
			for (int i = 0; i < count; ++i, mptr += (6 * sizeof(int)))
				modes[i] = Marshal.PtrToStructure<VidMode>(mptr);
			return modes;
		}

		public VidMode GetVideoMode(IntPtr monitor)
		{
			IntPtr mptr = _glfwGetVideoMode(monitor);
			return Marshal.PtrToStructure<VidMode>(mptr);
		}

		public unsafe void SetWindowTitle(IntPtr window, string title)
		{
			byte[] tstr = Encoding.UTF8.GetBytes(title + '\0');
			fixed (byte* tptr = tstr)
			{
				_glfwSetWindowTitle(window, (IntPtr)tptr);
			}
		}

		// TODO: Re-add when vulkan is linked in
		//public static long CreateWindowSurface(Vk.Instance inst, IntPtr window)
		//{
		//	Vk.Result res = (Vk.Result)_glfwCreateWindowSurface(inst.Handle, window, IntPtr.Zero, out IntPtr surface);
		//	if (res != Vk.Result.Success)
		//		throw new Vk.VulkanException(res, "Could not create Vulkan surface from GLFW.");
		//	return surface.ToInt64();
		//}
		#endregion // API Function Wrappers

		#region IDisposable
		public void Dispose()
		{
			if (_handle != IntPtr.Zero)
			{
				Native.NativeLoader.FreeLibrary(_handle);
				_handle = IntPtr.Zero;
			}
		}
		#endregion // IDisposable

		#region Structs
		[StructLayout(LayoutKind.Explicit, Size=6*sizeof(int))]
		public struct VidMode
		{
			[FieldOffset(0)]
			public int Width;
			[FieldOffset(1	*sizeof(int))]
			public int Height;
			[FieldOffset(2*sizeof(int))]
			public int RedBits;
			[FieldOffset(3*sizeof(int))]
			public int GreenBits;
			[FieldOffset(4*sizeof(int))]
			public int BlueBits;
			[FieldOffset(5*sizeof(int))]
			public int RefreshRate;
		}
		#endregion // Structs
	}
}
