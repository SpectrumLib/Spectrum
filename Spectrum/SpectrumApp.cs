using System;
using Spectrum.Graphics;
using static Spectrum.InternalLog;

namespace Spectrum
{
	/// <summary>
	/// Main class that is extended to control the startup, shutdown, and main loop of the entire application. Only one
	/// instance of this class may exist at once.
	/// </summary>
	public abstract class SpectrumApp : IDisposable
	{
		/// <summary>
		/// The current application instance.
		/// </summary>
		public static SpectrumApp Instance { get; private set; } = null;

		#region Fields
		/// <summary>
		/// The application parametrs that were used to create the application.
		/// </summary>
		public readonly AppParameters AppParameters;
		
		// The application driver instance
		internal readonly AppDriver Driver;
		/// <summary>
		/// The window for this application. You can set some window parameters before it is opened.
		/// </summary>
		public AppWindow Window => Driver.Window;

		/// <summary>
		/// The wrapper around the physical device being used to render by this application.
		/// </summary>
		public GraphicsDevice GraphicsDevice { get; private set; } = null;

		/// <summary>
		/// Gets if the application is set to exit at the end of the current update loop.
		/// </summary>
		public bool IsExiting { get; private set; } = false;

		/// <summary>
		/// Gets if the application instance is disposed.
		/// </summary>
		public bool IsDisposed { get; private set; } = false;

		/// <summary>
		/// The target framerate of the application, in frames per second. If able, the system will limit the
		/// application to run as close to this speed as possible. A value of null will unlock the framerate and
		/// will allow it to run as fast as possible. Defaults to null.
		/// </summary>
		public float? TargetFPS = null;

		#region Events
		/// <summary>
		/// Event raised when the application window gains and loses focus.
		/// </summary>
		public event ApplicationFocusEvent FocusChanged;
		/// <summary>
		/// Event raised when the application window is minimized or restored from minimization.
		/// </summary>
		public event ApplicationMinimizeEvent Minimized;
		#endregion // Events
		#endregion // Fields

		/// <summary>
		/// Starts the application and performs platform and library initialization using the passed parameters.
		/// </summary>
		/// <param name="appParams">The parameters to create the application with.</param>
		protected SpectrumApp(AppParameters appParams)
		{
			if (Instance != null)
				throw new InvalidOperationException("Unable to create more than once Application instance at once");
			Instance = this;

			// Validate the app parameters
			AppParameters = appParams;
			AppParameters.Validate();

			// Open the logging as soon as possible
			Logger.Initialize(AppParameters);
			LDEBUG("Application startup.");

			// Create the driver
			Driver = new AppDriver(this);
		}
		~SpectrumApp()
		{
			dispose(false);
		}

		/// <summary>
		/// Starts the application initialization procedures, then moves on to the main loop. This call will not return
		/// until the application exits its main loop.
		/// </summary>
		public void Run()
		{
			// Initialize the driver
			Driver.Initialize();

			// Create and assemble all of the graphics components
			GraphicsDevice = new GraphicsDevice(this);
			GraphicsDevice.InitializeResources();

			doInitialize();

			GC.Collect(); // Probably not a bad idea after potentially heavy initialization

			Driver.MainLoop();
		}

		/// <summary>
		/// Tells the application to exit at the end of the current update loop. Suppresses the final render loop of
		/// the frame it was called in.
		/// </summary>
		public void Exit()
		{
			IsExiting = true;
		}

		#region Initialization
		// Performs the internal initialization, then the user initialization
		private void doInitialize()
		{
			Initialize();
		}

		/// <summary>
		/// Override in the base class to perform custom initialization. User code does not need to call 
		/// <c>base.Initialize()</c>.
		/// </summary>
		protected virtual void Initialize() { }
		#endregion // Initialization

		#region Main Loop
		// Performs a single frame
		internal void DoFrame()
		{
			doUpdate();
			if (!IsExiting)
				doRender();
		}

		// Performs the update logic for a single frame
		private void doUpdate()
		{
			PreUpdate();
			CoroutineManager.Tick();
			Update();
		}

		// Performs the render logic for a single frame
		private void doRender()
		{
			GraphicsDevice.BeginFrame();

			PreRender();
			Render();

			GraphicsDevice.EndFrame();
		}

		/// <summary>
		/// Called once per frame to perform logic before the main update logic. Override in base class to implement
		/// custom PreUpdate logic. User code does not need to call <c>base.PreUpdate()</c>.
		/// </summary>
		protected virtual void PreUpdate() { }
		/// <summary>
		/// Called once per frame to perform the main update logic. Override in base class to implement custom 
		/// Update logic. User code does not need to call <c>base.Update()</c>.
		/// </summary>
		protected virtual void Update() { }

		/// <summary>
		/// Called once per frame to perform logic before the main render logic. Override in base class to implement
		/// custom PreRender logic. User code does not need to call <c>base.PreRender()</c>.
		/// </summary>
		protected virtual void PreRender() { }
		/// <summary>
		/// Called once per frame to perform the main render logic. Override in base class to implement custom 
		/// Render logic. User code does not need to call <c>base.Render()</c>.
		/// </summary>
		protected virtual void Render() { }
		#endregion // Main Loop

		internal static void FocusChangeCallback(bool focused) => Instance.FocusChanged?.Invoke(focused);

		internal static void MinimizeCallback(bool minimized) => Instance.Minimized?.Invoke(minimized);

		#region IDisposable
		/// <summary>
		/// Disposes the application at the end of its execution. Performs a cleanup of all library components.
		/// Application is left unusable after this call.
		/// </summary>
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				OnDisposing(disposing);

				GraphicsDevice.Dispose();
				CoroutineManager.Cleanup();
				
				Driver.Dispose();

				// Keep the logging available for as long as possible
				LDEBUG("Application disposal.");
				Logger.Shutdown();
			}

			IsDisposed = true;
			Instance = null;
		}

		/// <summary>
		/// Called when the application instance is being disposed, and before any library components are disposed. 
		/// User code should override this to perform disposal of any resources not managed by the library. This
		/// function will be called either from an implicit or explicit call to <see cref="Dispose"/>, or from
		/// the application instance being finalized, but it is guarenteed to only ever be called once.
		/// </summary>
		/// <param name="disposing">If the function is called from <see cref="Dispose"/>.</param>
		protected virtual void OnDisposing(bool disposing) { }
		#endregion // IDisposable
	}

	/// <summary>
	/// Callback for application focus change event.
	/// </summary>
	/// <param name="focused"><c>true</c> if the window has gained focus, <c>false</c> if it has lost focus.</param>
	public delegate void ApplicationFocusEvent(bool focused);

	/// <summary>
	/// Callback for application minimization event.
	/// </summary>
	/// <param name="resotred"><c>true</c> if the window has been minimized, <c>false</c> if the window has been restored.</param>
	public delegate void ApplicationMinimizeEvent(bool minimized);
}
