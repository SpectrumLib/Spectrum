using Spectrum.Graphics;
using System;

namespace Spectrum
{
	/// <summary>
	/// Contains the sum of all active objects pertaining to logic, rendering, and state. The primary way to implement
	/// custom application logic and rendering. Instances of this class control the active state of the application, 
	/// and only one scene can be active at once. When coupled with <see cref="SceneManager"/>, powerful functionality 
	/// for application lifecycle and scene transitions are available.
	/// </summary>
	public abstract class AppScene : IDisposable
	{
		#region Fields
		/// <summary>
		/// The name of this scene, used for debugging purposes and identification.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// If this scene is the active scene in the application.
		/// </summary>
		public bool IsActive { get; private set; } = false;

		/// <summary>
		/// A reference to the application.
		/// </summary>
		public SpectrumApp Application => SpectrumApp.Instance;
		/// <summary>
		/// A reference to the current graphics device.
		/// </summary>
		public GraphicsDevice GraphicsDevice => SpectrumApp.Instance.GraphicsDevice;

		/// <summary>
		/// If this scene has been disposed.
		/// </summary>
		protected bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		/// <summary>
		/// Initializes the base functionality of the scene. Scenes cannot be constructed directly, and must be
		/// created with the <see cref="SceneManager.CreateScene{T}"/> functions.
		/// </summary>
		/// <param name="name">The name of the scene.</param>
		protected AppScene(string name)
		{
			// Ensure we are creating this through SceneManager, and set to false to prevent this scene from making more scenes
			lock (SceneManager.CreateLock)
			{
				if (!SceneManager.IsCreatingScene)
					throw new InvalidOperationException("AppScene instances must be created through the SceneManager");
				SceneManager.IsCreatingScene = false;
			}

			if (String.IsNullOrWhiteSpace(name))
				throw new ArgumentException("The scene name cannot be null or empty", nameof(name));

			Name = name;
		}
		~AppScene()
		{
			dispose(false);
		}

		#region Internal Scene Lifecycle
		internal void Initialize()
		{
			OnInitialize();
		}

		internal void Enable()
		{
			OnEnable();
		}

		internal void Start()
		{
			IsActive = true;
			OnStart();
		}

		internal void Disable()
		{
			OnDisable();
		}

		internal void Remove()
		{
			OnRemove();
			IsActive = false;
		}

		internal void DoPreUpdate()
		{
			PreUpdate();
		}

		internal void DoUpdate()
		{
			Update();
		}

		internal void DoPostUpdate()
		{
			PostUpdate();
		}

		internal void DoPreRender()
		{
			PreRender();
		}

		internal void DoRender()
		{
			Render();
		}

		internal void DoPostRender()
		{
			PostRender();
		}
		#endregion // Internal Scene Lifecycle

		#region User Functions
		/// <summary>
		/// Called immediately after the scene is constructed, to perform further lightweight initialization. Do not
		/// load and resources or content in this function, as it will cause the application to lag or hang. 
		/// </summary>
		protected virtual void OnInitialize() { }

		/// <summary>
		/// Called when the scene is initially queued to become the new active scene. Note that there may be more
		/// rendered frames between when this function is called and when it becomes active, if there is a scene
		/// transition. Asynchronous resource/content loading can be started in this function.
		/// </summary>
		protected virtual void OnEnable() { }

		/// <summary>
		/// Called when the old active scene is destroyed, and this scene becomes the new active scene. Most of the
		/// resource/content loading for the scene should be done here.
		/// </summary>
		protected virtual void OnStart() { }

		/// <summary>
		/// Called when this scene is queued to be removed as the active scene. Note that there may be more
		/// rendered frames between when this function is called and when it gets removed, if there is a scene
		/// transition.
		/// </summary>
		protected virtual void OnDisable() { }

		/// <summary>
		/// Called when this scene is removed as the active scene. Resource/content unloading should be done here.
		/// </summary>
		protected virtual void OnRemove() { }

		/// <summary>
		/// Called when this scene is disposed. Should be used to perform any last-minute cleanup, and is the last
		/// function called on this scene while it is alive. If the application exits unexpectedly, this is the only
		/// function that will be called.
		/// </summary>
		/// <param name="disposing">
		/// `true` if the scene was disposed manually, `false` otherwise (garbage collected).
		/// </param>
		protected virtual void OnDispose(bool disposing) { }

		/// <summary>
		/// Called before the main update functionality. Can be used to prepare objects or logic to handle the main
		/// update code.
		/// </summary>
		protected virtual void PreUpdate() { }

		/// <summary>
		/// Called to perform the main update functionality in a frame.
		/// </summary>
		protected virtual void Update() { }

		/// <summary>
		/// Called after the main update functionality. Can be used to check update results, or perform other
		/// functionality that is dependent on the main update being finished.
		/// </summary>
		protected virtual void PostUpdate() { }

		/// <summary>
		/// Called before the main render functionality. Can be used to prepare objects, logic, or render state to
		/// handle the main render code.
		/// </summary>
		protected virtual void PreRender() { }

		/// <summary>
		/// Called to perform the main render functionality in a frame.
		/// </summary>
		protected virtual void Render() { }

		/// <summary>
		/// Called after the main render functionality. Can be used for final frame cleanup, or to implement/control
		/// postprocessing effects.
		/// </summary>
		protected virtual void PostRender() { }
		#endregion // User Functions

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				OnDispose(disposing);

				SceneManager.RemoveScene(this);
			}

			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
