/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;

namespace Spectrum
{
	internal partial class Glfw3 : IDisposable
	{
		#region Fields
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

		private IntPtr _handle = IntPtr.Zero;
		#endregion // Fields

		// Loads the library and function pointers
		public Glfw3()
		{
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

			// Set the error callback
			_glfwSetErrorCallback((code, desc) => {
				_lastErrorCode = code;
				_lastErrorDesc = desc;
			});
		}
		~Glfw3()
		{
			Dispose();
		}

		#region API Functions
		public bool Init() => (_glfwInit() == Glfw3.TRUE);
		public void Terminate() => _glfwTerminate();
		#endregion // API Functions

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
	}
}
