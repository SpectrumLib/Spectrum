using System;
using static Spectrum.InternalLog;
using Spectrum.Input;
using System.Linq;

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
			if (!Glfw.Init())
			{
				LFATAL("Failed to initialize GLFW3.");
				throw new Exception("Unable to initialize the GLFW3 library, check log for error");
			}
			else
			{
				LINFO($"Loaded glfw3 function pointers (took {Glfw.LoadTime.TotalMilliseconds:.00} ms).");
			}

			// Check for the vulkan runtime
			if (!Glfw.VulkanSupported())
			{
				LFATAL("Vulkan runtime not found.");
				throw new Exception("Vulkan runtime not located on this system, please ensure your graphics drivers are up to date");
			}

			// Initialize the audio engine
			Audio.AudioEngine.Initialize();

			// Create the window (zero initialization for glfw done here)
			Window = new AppWindow(app);
		}
		~AppDriver()
		{
			dispose(false);
		}

		// Performs the runtime initialization
		public void Initialize()
		{
			Window.CreateWindow();
		}

		public void MainLoop()
		{
			Window.ShowWindow();

			while (true)
			{
				// Update the time
				Time.Frame();

				// If needed, limit the framerate
				if (Application.TargetFPS.HasValue)
				{
					//TimestepUtils.WaitFor(1000f / Application.TargetFPS.Value, (float)Time.RealDeltaTime.TotalMilliseconds);
				}

				// Input events
				Keyboard.NewFrame();
				Mouse.NewFrame();
				Glfw.PollEvents(); // Raises input and window events
				Keyboard.FireEvents();
				Mouse.FireEvents();

				// Update the subsystems
				Audio.AudioEngine.Update();

				// Perform the update and render portions of the game loop
				Application.DoFrame();

				// Check for exit conditions
				if (Glfw.WindowShouldClose(Window.Handle))
					break;
				if (Application.IsExiting)
					break;
			}
		}

		private void loadNativeLibraries()
		{
			LDEBUG($"Available native libraries: {String.Join(", ", NativeLoader.AvailableResources.Select(n => n.Substring(16)))}");

			NativeLoader.Logger = LWARN;

			try
			{
				NativeLoader.LoadUnmanagedLibrary("glfw3", "glfw3.dll");
				LINFO($"Loaded native library for glfw3 (took {NativeLoader.LastLoadTime.TotalMilliseconds:.00} ms).");
			}
			catch (Exception e)
			{
				throw new Exception($"Unable to load native library glfw3, reason: {e.Message}");
			}

			try
			{
				NativeLoader.LoadUnmanagedLibrary("oal", "soft_oal.dll");
				LINFO($"Loaded native library for openal (took {NativeLoader.LastLoadTime.TotalMilliseconds:.00} ms).");
			}
			catch (Exception e)
			{
				throw new Exception($"Unable to load native library oal, reason: {e.Message}");
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

				Audio.AudioEngine.Shutdown();

				Glfw.Terminate();

				NativeLoader.UnloadLibraries();
				_isDisposed = true;
			}
		}
		#endregion // IDisposable
	}
}
