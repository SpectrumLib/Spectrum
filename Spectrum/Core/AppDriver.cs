﻿using System;
using static Spectrum.InternalLog;

namespace Spectrum
{
	// Controls the runtime of the application, event polling, frame dispatching, and platform related opertaions.
	internal class AppDriver : IDisposable
	{
		#region Fields
		public readonly SpectrumApp Application = null;

		public AppWindow Window { get; private set; } = null;

		private bool _isDisposed = false;
		#endregion // Fields

		public AppDriver(SpectrumApp app)
		{
			Application = app;

			// Report/Load the unmanaged libraries
			loadNativeLibraries();

			// Initialize GLFW3
			if (Glfw.Init() != Glfw.TRUE)
			{
				LFATAL("Failed to initialize GLFW3.");
				throw new Exception("Unable to initialize the GLFW3 library, check log for error");
			}
			else
			{
				LINFO($"Loaded glfw3 function pointers (took {Glfw.LoadTime.TotalMilliseconds:.00} ms).");
			}

			// Create the window (but keep it hidden)
			Window = new AppWindow(app);
		}
		~AppDriver()
		{
			dispose(false);
		}

		// Performs the runtime initialization
		public void Initialize()
		{
			
		}

		public void MainLoop()
		{
			while (true)
			{
				Time.Frame();
				Glfw.PollEvents(); // Also raises the input events
				Application.DoFrame();

				if (Glfw.WindowShouldClose(Window.Handle))
					break;
				if (Application.IsExiting)
					break;
			}
		}

		private void loadNativeLibraries()
		{
			foreach (var lib in NativeLoader.AvailableResources)
				LDEBUG($"Available native library: {lib}.");

			try
			{
				NativeLoader.LoadUnmanagedLibrary("glfw3", "glfw3.dll");
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
				Window.Dispose();

				Glfw.Terminate();

				NativeLoader.UnloadLibraries();
				_isDisposed = true;
			}
		}
		#endregion // IDisposable
	}
}
