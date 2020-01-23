/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Spectrum
{
	// GLFW 3.3 functions and callbacks, and member delegates, taken from the glfw header
	internal partial class Glfw3
	{
		private static readonly int TYPE_HEADER_LENGTH = typeof(Delegates.glfwInit).Name.LastIndexOf('.') + 1;

		#region Delegate Fields
		private readonly Delegates.glfwInit _glfwInit;
		private readonly Delegates.glfwTerminate _glfwTerminate;
		private readonly Delegates.glfwGetVersion _glfwGetVersion;
		private readonly Delegates.glfwSetErrorCallback _glfwSetErrorCallback;
		private readonly Delegates.glfwWindowHint _glfwWindowHint;
		private readonly Delegates.glfwCreateWindow _glfwCreateWindow;
		private readonly Delegates.glfwDestroyWindow _glfwDestroyWindow;
		private readonly Delegates.glfwWindowShouldClose _glfwWindowShouldClose;
		private readonly Delegates.glfwPollEvents _glfwPollEvents;
		private readonly Delegates.glfwShowWindow _glfwShowWindow;
		private readonly Delegates.glfwVulkanSupported _glfwVulkanSupported;
		private readonly Delegates.glfwGetWindowAttrib _glfwGetWindowAttrib;
		private readonly Delegates.glfwSetWindowAttrib _glfwSetWindowAttrib;
		private readonly Delegates.glfwGetWindowSize _glfwGetWindowSize;
		private readonly Delegates.glfwSetWindowSize _glfwSetWindowSize;
		private readonly Delegates.glfwGetWindowPos _glfwGetWindowPos;
		private readonly Delegates.glfwSetWindowPos _glfwSetWindowPos;
		private readonly Delegates.glfwGetPrimaryMonitor _glfwGetPrimaryMonitor;
		private readonly Delegates.glfwGetMonitors _glfwGetMonitors;
		private readonly Delegates.glfwGetMonitorPos _glfwGetMonitorPos;
		private readonly Delegates.glfwGetVideoModes _glfwGetVideoModes;
		private readonly Delegates.glfwGetVideoMode _glfwGetVideoMode;
		private readonly Delegates.glfwSetWindowTitle _glfwSetWindowTitle;
		private readonly Delegates.glfwSetMouseButtonCallback _glfwSetMouseButtonCallback;
		private readonly Delegates.glfwSetScrollCallback _glfwSetScrollCallback;
		private readonly Delegates.glfwSetKeyCallback _glfwSetKeyCallback;
		private readonly Delegates.glfwGetCursorPos _glfwGetCursorPos;
		private readonly Delegates.glfwSetInputMode _glfwSetInputMode;
		private readonly Delegates.glfwSetCursorEnterCallback _glfwSetCursorEnterCallback;
		private readonly Delegates.glfwSetWindowPosCallback _glfwSetWindowPosCallback;
		private readonly Delegates.glfwSetWindowSizeCallback _glfwSetWindowSizeCallback;
		private readonly Delegates.glfwSetWindowFocusCallback _glfwSetWindowFocusCallback;
		private readonly Delegates.glfwSetWindowIconifyCallback _glfwSetWindowIconifyCallback;
		private readonly Delegates.glfwGetPhysicalDevicePresentationSupport _glfwGetPhysicalDevicePresentationSupport;
		private readonly Delegates.glfwCreateWindowSurface _glfwCreateWindowSurface;
		#endregion // Delegate Fields

		private T loadFunc<T>()
			where T : Delegate
			=> NativeUtils.LoadFunction<T>(_handle, typeof(T).Name.Substring(TYPE_HEADER_LENGTH));

		#region Public Delegate Types
		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWerrorfun(int error, string desc);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWmousebuttonfun(IntPtr window, int button, int action, int mods);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWscrollfun(IntPtr window, double xoffset, double yoffset);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWkeyfun(IntPtr window, int key, int scancode, int action, int mods);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void Glfwcursorenterfun(IntPtr window, int entered);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWwindowposfun(IntPtr window, int xpos, int ypos);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWwindowsizefun(IntPtr window, int width, int height);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWwindowfocusfun(IntPtr window, int focused);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWwindowiconifyfun(IntPtr window, int iconified);
		#endregion // Public Delegate Types

		#region API Delegate Types
		private static class Delegates
		{
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwInit();
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwTerminate();
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwGetVersion(out int maj, out int min, out int rev);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetErrorCallback(GLFWerrorfun cbfun);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwWindowHint(int hint, int value);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwCreateWindow(int width, int height, IntPtr title, IntPtr monitor, IntPtr share);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwDestroyWindow(IntPtr window);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwWindowShouldClose(IntPtr window);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwPollEvents();
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwShowWindow(IntPtr window);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwVulkanSupported();
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwGetWindowAttrib(IntPtr window, int attrib);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowAttrib(IntPtr window, int attrib, int value);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwGetWindowSize(IntPtr window, out int width, out int height);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowSize(IntPtr window, int width, int height);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwGetWindowPos(IntPtr window, out int x, out int y);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowPos(IntPtr window, int w, int y);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetPrimaryMonitor();
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetMonitors(out int count);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwGetMonitorPos(IntPtr monitor, out int x, out int y);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetVideoModes(IntPtr monitor, out int count);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr glfwGetVideoMode(IntPtr monitor);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowTitle(IntPtr window, IntPtr title);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWmousebuttonfun glfwSetMouseButtonCallback(IntPtr window, GLFWmousebuttonfun mouse_button_callback);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWscrollfun glfwSetScrollCallback(IntPtr window, GLFWscrollfun scroll_callback);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate GLFWkeyfun glfwSetKeyCallback(IntPtr window, GLFWkeyfun key_callback);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwGetCursorPos(IntPtr window, out double xpos, out double ypos);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetInputMode(IntPtr window, int mode, int value);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetCursorEnterCallback(IntPtr window, Glfwcursorenterfun cursor_enter_callback);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowPosCallback(IntPtr window, GLFWwindowposfun window_pos_callback);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowSizeCallback(IntPtr window, GLFWwindowsizefun window_size_callback);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowFocusCallback(IntPtr window, GLFWwindowfocusfun focus_callback);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void glfwSetWindowIconifyCallback(IntPtr window, GLFWwindowiconifyfun iconify_callback);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwGetPhysicalDevicePresentationSupport(IntPtr instance, IntPtr device, uint family);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int glfwCreateWindowSurface(IntPtr instance, IntPtr window, IntPtr alloc, out IntPtr surface);
		}
		#endregion // API Delegate Types
	}
}
