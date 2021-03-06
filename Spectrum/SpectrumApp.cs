using System;
using Spectrum.Audio;
using Spectrum.Content;
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

		/// <summary>
		/// The content manager for global content (content that exists outside of individual <see cref="AppScene"/>
		/// instances, and should persist past active AppScene changes). This manager is not initialized until
		/// immediately before <see cref="LoadContent"/> is called, and will only be initialized if
		/// <see cref="AppParameters.GlobalContentPath"/> is not null.
		/// </summary>
		public ContentManager GContent { get; private set; } = null;
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
			// Initialize the driver (this creates the window)
			Driver.Initialize();

			// Create and assemble all of the graphics components
			GraphicsDevice = new GraphicsDevice(this);
			GraphicsDevice.InitializeResources();

			doInitialize();

			if (AppParameters.GlobalContentPath != null)
				GContent = ContentManager.OpenPackFile(AppParameters.GlobalContentPath);
			doLoadContent();

			GC.Collect(); // Probably not a bad idea after potentially heavy initialization

			doStart();
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
		/// Override in the base class to perform custom initialization. Content cannot be loaded in this function. 
		/// User code does not need to call <c>base.Initialize()</c>.
		/// </summary>
		protected virtual void Initialize() { }

		// Performs internal content loading, then user content loading
		private void doLoadContent()
		{
			LoadContent();
		}

		/// <summary>
		/// Override in the base class to perform loading of global content. User code does not need to call
		/// <c>base.LoadContent()</c>.
		/// </summary>
		protected virtual void LoadContent() { }

		// Performs the startup procedure
		private void doStart()
		{
			Start();
		}

		/// <summary>
		/// Override in the base class to perform final startup procedures. This function is called immediately before
		/// the first frame starts. This is where you should set your initial <see cref="AppScene"/>. User code does
		/// not need to call <c>base.Start()</c>.
		/// </summary>
		protected virtual void Start() { }
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
			SceneManager.PreUpdate();
			CoroutineManager.Tick();
			Update();
			SceneManager.Update();
			PostUpdate();
			SceneManager.PostUpdate();
		}

		// Performs the render logic for a single frame
		private void doRender()
		{
			GraphicsDevice.BeginFrame();

			PreRender();
			SceneManager.PreRender();
			Render();
			SceneManager.Render();
			PostRender();
			SceneManager.PostRender();

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
		/// Called once per frame to perform logic after the main update logic. Override in base class to implement 
		/// custom PostUpdate logic. User code does not need to call <c>base.PostUpdate()</c>.
		/// </summary>
		protected virtual void PostUpdate() { }

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
		/// <summary>
		/// Called once per frame to perform logic after the main render logic. Override in base class to implement
		/// custom PreRender logic. User code does not need to call <c>base.PostRender()</c>.
		/// </summary>
		protected virtual void PostRender() { }
		#endregion // Main Loop

		#region Event Callbacks
		// Private event handlers
		internal void DoFocusChange(bool focused)
		{
			OnFocusChange(focused);
		}
		internal void DoMinimize(bool minimized)
		{
			if (minimized) OnMinimize();
			else OnRestore();
		}
		internal void DoBackbufferSizeChange(uint nw, uint nh)
		{
			OnBackbufferSizeChange(nw, nh);
			SceneManager.ActiveScene?.BackbufferResize(nw, nh);
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
				// Clean up all of the scenes, including the active one
				SceneManager.Shutdown();

				OnDisposing(disposing);

				SongThread.Stop();
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
}
