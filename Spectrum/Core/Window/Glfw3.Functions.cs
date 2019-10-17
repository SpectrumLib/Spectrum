/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
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
		#endregion // Delegate Fields

		private T loadFunc<T>()
			where T : Delegate
			=> NativeUtils.LoadFunction<T>(_handle, typeof(T).Name.Substring(TYPE_HEADER_LENGTH));

		#region Public Delegate Types
		[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
		public delegate void GLFWerrorfun(int error, string desc);
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
		}
		#endregion // API Delegate Types
	}
}
