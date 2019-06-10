using System;
using Spectrum.Content;
using Spectrum.Graphics;

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
		/// The manager for the content items specific to this scene.
		/// </summary>
		public readonly ContentManager Content;

		/// <summary>
		/// The renderer responsible for this scene.
		/// </summary>
		public readonly SceneRenderer Renderer;

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
		/// <param name="contentPath">
		/// The path to the content pack to source the scene content items from. Defaults to the default content pack
		/// in the data/ folder.
		/// </param>
		protected AppScene(string name, string contentPath = "data/Content.cpak")
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
			if (String.IsNullOrWhiteSpace(contentPath))
				throw new ArgumentNullException(nameof(contentPath));

			Name = name;
			Content = ContentManager.OpenPackFile(contentPath);
			Renderer = new SceneRenderer(this);
		}
		~AppScene()
		{
			dispose(false);
		}

		#region Internal Scene Lifecycle
		internal void Initialize()
		{
			Renderer.Rebuild((uint)Application.Window.Size.X, (uint)Application.Window.Size.Y);

			OnInitialize();
		}

		internal void QueueChange(bool hasTransition)
		{
			OnQueueChange(hasTransition);
		}

		internal void Start()
		{
			IsActive = true;
			OnStart();
		}

		internal void Remove()
		{
			OnRemove();
			Content.Dispose();
			Renderer.Dispose();
			IsActive = false;
		}

		internal void BackbufferResize(uint nw, uint nh)
		{
			if (Renderer.BackbufferSize.X == nw && Renderer.BackbufferSize.Y == nh)
				return; // Size didnt change

			var oldSize = Renderer.BackbufferSize;
			Renderer.Rebuild(nw, nh);

			OnBackbufferResize((uint)oldSize.X, (uint)oldSize.Y, nw, nh);
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
		/// Called when a new scene is queued to become the active scene, and this scene is either the newely queued
		/// scene, or the currently active scene that will be replaced.
		/// </summary>
		/// <remarks>
		/// The scenes should check <see cref="AppScene.IsActive"/> to decide if they are the newely queued scene, or the
		/// active scene that is being replaced. Note that there can be many frames between this call and the actual change,
		/// if there is a scene transition occuring. New scenes should not load resources/content on the main thread in this
		/// function, and active scenes should not start unloading resources/content as it may be rendered for multiple
		/// frames before actually being disposed.
		/// </remarks>
		/// <param name="hasTransition">If there is a scene transition running.</param>
		protected virtual void OnQueueChange(bool hasTransition) { }

		/// <summary>
		/// Called when the old active scene is destroyed, and this scene becomes the new active scene. Most of the
		/// resource/content loading for the scene should be done here.
		/// </summary>
		protected virtual void OnStart() { }

		/// <summary>
		/// Called when this scene is removed as the active scene. Resource/content unloading should be done here.
		/// </summary>
		protected virtual void OnRemove() { }

		/// <summary>
		/// Called when the window size changes, and the render targets must be rebuilt to the new size. The
		/// <see cref="SceneRenderer"/> instance for this scene will be automatically rebuilt, this function is only
		/// to update user managed graphics resources.
		/// </summary>
		/// <param name="oldWidth">The old width of the window/backbuffer.</param>
		/// <param name="oldHeight">The old height of the window/backbuffer.</param>
		/// <param name="newWidth">The new width of the window/backbuffer.</param>
		/// <param name="newHeight">The new height of the window/backbuffer.</param>
		protected virtual void OnBackbufferResize(uint oldWidth, uint oldHeight, uint newWidth, uint newHeight) { }

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
