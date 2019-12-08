/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using Spectrum.Content;
using Spectrum.Graphics;
using static Spectrum.InternalLog;

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
		/// The main window for the application.
		/// </summary>
		public CoreWindow Window { get; private set; } = null;

		/// <summary>
		/// The controller for the physical graphics device used by the application window.
		/// </summary>
		public GraphicsDevice GraphicsDevice { get; private set; } = null;

		/// <summary>
		/// Manager for global content, which persists outside of <see cref="Scene"/> lifetimes. This object will not
		/// be available until <see cref="LoadContent"/> is called.
		/// </summary>
		public ContentManager GContent { get; private set; } = null;

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
			IINFO($"Application initialized at {DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")}.");

			// Create (but dont open) the window
			Window = new CoreWindow();
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
			// Initialization code
			Audio.AudioEngine.Initialize();
			Window.CreateWindow();
			GraphicsDevice = new GraphicsDevice();
			Initialize();
		
			// Load global content
			if (Params.LoadGlobalContent)
			{
				GContent = ContentManager.OpenPackFile(Params.GlobalContentPath);
				LoadContent();
			}

			// Start and run the main loop
			Start();
			mainLoop();

			// End code and pre-disposal
			End();
		}

		// Runs the main application loop
		private void mainLoop()
		{
			Window.ShowWindow(); // Show immediately before the loop starts

			while (!IsExiting)
			{
				// Update the time, prepare frame-based classes for a new frame
				Time.Frame();
				Window.PumpEvents(); // Also updates the input classes

				// Begin the frame
				BeginFrame();
				SceneManager.BeginFrame();

				// Update the frame
				CoroutineManager.Tick();
				Threading.RunActions();
				Audio.AudioEngine.Update();
				Update();
				SceneManager.Update();

				// Check for early exit, or window self-close
				if (IsExiting || Window.ShouldClose)
					break;

				// Midframe
				MidFrame();
				SceneManager.MidFrame();

				// Render the frame
				GraphicsDevice.BeginFrame();
				Render();
				SceneManager.Render();

				// End the frame
				EndFrame();
				SceneManager.EndFrame();
				GraphicsDevice.EndFrame();

				// Allow final post frame logic
				PostFrame();
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
		/// loading screens. This will not be called if <see cref="CoreParams.LoadGlobalContent"/> is <c>false</c> for
		/// <see cref="Params"/>.
		/// </summary>
		public virtual void LoadContent() { }
		/// <summary>
		/// Called immediately before the main loop is entered. The application should use this function to set the
		/// initial scene and perform final state setup.
		/// </summary>
		public virtual void Start() { }
		/// <summary>
		/// Called immediately after the main loop exits, and before application disposable begins.
		/// </summary>
		public virtual void End() { }
		/// <summary>
		/// Called when the application core object is being disposed.
		/// </summary>
		/// <param name="disposing"><c>true</c> if <see cref="Dispose"/> was called manually.</param>
		public virtual void OnDisposing(bool disposing) { }
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
		/// <summary>
		/// Called after <see cref="Scene.EndFrame"/> to perform the very final application-space logic in a frame,
		/// that may depend on the final frame logic in the active scene.
		/// </summary>
		protected virtual void PostFrame() { }
		#endregion // Core Loop

		#region Event Callbacks
		// Private event handlers
		internal void DoFocusChange(bool focused) => OnFocusChange(focused);
		internal void DoMinimize(bool minimized)
		{
			if (minimized) OnMinimize();
			else OnRestore();
		}
		internal void DoBackbufferSizeChange(uint nw, uint nh)
		{
			OnBackbufferSizeChange(nw, nh);
			SceneManager.ActiveScene?.BackbufferResize(new Extent(nw, nh));
		}

		/// <summary>
		/// Called when the application window loses or gains focus.
		/// </summary>
		/// <param name="focused">If the window is now focused. <c>false</c> implies the window lost focus.</param>
		protected virtual void OnFocusChange(bool focused) { }
		/// <summary>
		/// Called when the application window is minimized to the OS system bar.
		/// </summary>
		protected virtual void OnMinimize() { }
		/// <summary>
		/// Called when the application window is restored from the OS system bar.
		/// </summary>
		protected virtual void OnRestore() { }
		/// <summary>
		/// Called when the application window or backbuffer size changes.
		/// </summary>
		/// <param name="newWidth">The new width of the window/backbuffer.</param>
		/// <param name="newHeight">The new height of the window/backbuffer.</param>
		protected virtual void OnBackbufferSizeChange(uint newWidth, uint newHeight) { }
		#endregion // Event Callbacks

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
				// Clean up the updating objects
				CoroutineManager.Terminate();
				SceneManager.Terminate();

				// Call the user dispose function
				OnDisposing(disposing);

				// Clean up the content and hardware objects
				GContent?.Dispose();
				GraphicsDevice.Dispose();

				// Clean up the window
				Window.Dispose();

				// Clean up the audio engine
				Audio.AudioEngine.Terminate();

				IINFO($"Application terminated at {DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")}.");
				Logger.Terminate(); // Should go last to allow logging to the last moment
			}

			// Always free native libraries
			Native.NativeLoader.FreeAll();

			Instance = null;
			_isDisposed = true;
		}
		#endregion // Dispose
	}
}
