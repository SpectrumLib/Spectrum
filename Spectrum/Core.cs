/*
 * GNU LGPLv3 License - Copyright (c) The Spectrum Team
 * This file is subject to the terms and conditions of the GNU LGPLv3 license, the text of which can be found in the
 * 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/LGPL-3.0>.
 */
using System;

namespace Spectrum
{
	/// <summary>
	/// The central type that manages the runtime and core objects of a Spectrum application. At minimum, a Spectrum
	/// application requires an active instance of a child of this type to be runnable. Only one instance can exist
	/// at a time.
	/// </summary>
	public abstract class Core : IDisposable
	{
		/// <summary>
		/// A reference to the active singleton instance of this type, if one exists. This reference can be used to
		/// safely get the instances of other core types in the current application.
		/// </summary>
		public static Core Instance { get; private set; } = null;

		#region Fields
		/// <summary>
		/// The name of the application.
		/// </summary>
		public string Name => Params.Name;
		/// <summary>
		/// The version of the application.
		/// </summary>
		public Version Version => Params.Version;

		/// <summary>
		/// Gets if the application is currently flaged to exit at the end of the next frame update loop.
		/// </summary>
		public bool IsExiting { get; private set; } = false;

		// The parameters that the application was initialized with.
		internal readonly CoreParams Params;

		private bool _isDisposed = false;
		#endregion // Fields

		/// <summary>
		/// Performs construction and initialization of most non-graphics library systems.
		/// </summary>
		/// <param name="cpar">The parameters to construct the application with.</param>
		/// <exception cref="InvalidCoreParameterException">The passed parameters contained an invalid value.</exception>
		protected Core(CoreParams cpar)
		{
			if (Instance != null)
				throw new InvalidOperationException("Cannot create more than one instance of a Core type at one time.");
			Instance = this;

			Params = cpar ?? throw new ArgumentNullException(nameof(cpar), "Cannot pass null parameters to Core.");
			Params.Validate();

			Logger.Initialize(Params);
		}
		~Core()
		{
			dispose(false);
		}

		/// <summary>
		/// Starts the remaining application initialization procedures, then moves on to the main application loop.
		/// This function will not return until the application exits its main loop.
		/// </summary>
		public void Run()
		{
			// User code initialization
			Initialize();

			// Load global content
			LoadContent();

			// Start and run the main loop
			Start();
			mainLoop();
		}

		// Runs the main application loop
		private void mainLoop()
		{
			while (!IsExiting)
			{
				// Update the time
				Time.Frame();

				// Begin the frame
				BeginFrame();

				// Update the frame
				CoroutineManager.Tick();
				Update();

				// Check for early exit
				if (IsExiting)
					break;

				// Midframe
				MidFrame();

				// Render the frame
				Render();

				// End the frame
				EndFrame();
			}
		}

		/// <summary>
		/// Flags the application that it should exit. Supresses MidFrame(), the render logic, and EndFrame() in the
		/// last frame after it is called.
		/// </summary>
		public void Exit() => IsExiting = true;

		#region Initialization
		/// <summary>
		/// Allows the application to perform final initialization steps after the library has been fully initialized.
		/// This is called before the global content manager is created, and global content loaded.
		/// </summary>
		public virtual void Initialize() { }
		/// <summary>
		/// Allows the application to load global content before the main loop starts, such as for splash or initial
		/// loading screens.
		/// </summary>
		public virtual void LoadContent() { }
		/// <summary>
		/// Called immediately before the main loop is entered. The application should use this function to set the
		/// initial scene and perform final state setup.
		/// </summary>
		public virtual void Start() { }
		#endregion // Initialization

		#region Core Loop
		/// <summary>
		/// First application-space function called at the beginning of a new frame. It is called after the library
		/// updates its internal states in the new frame, but before any of the Update loop code is called.
		/// </summary>
		protected virtual void BeginFrame() { }
		/// <summary>
		/// Called once per frame to implement the main update logic. Called before the Update function in the active 
		/// scene.
		/// </summary>
		protected virtual void Update() { }
		/// <summary>
		/// Called between the Update and Render functionality of the current frame.
		/// </summary>
		protected virtual void MidFrame() { }
		/// <summary>
		/// Called once per frame to implement the main render logic. Called before the Render function in the active 
		/// scene.
		/// </summary>
		protected virtual void Render() { }
		/// <summary>
		/// Called after the render functionality of the current frame is complete.
		/// </summary>
		protected virtual void EndFrame() { }
		#endregion // Core Loop

		#region Dispose
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				CoroutineManager.Terminate();

				Logger.Terminate(); // Should go last to allow logging to the last moment
			}

			Instance = null;
			_isDisposed = true;
		}
		#endregion // Dispose
	}
}
