/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using Spectrum.Graphics;
using System;

namespace Spectrum
{
	/// <summary>
	/// Represents the collection of all objects and states required for application updates and rendering. Scenes are
	/// designed to allow separatation and specialization of global application state by subclassing from a central
	/// type. Scenes should be used to implement the application logic and rendering, instead of putting code into the
	/// <see cref="Spectrum.Core"/> class.
	/// </summary>
	public abstract class Scene : IDisposable
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
		/// A reference to the current <see cref="Core.GraphicsDevice"/>.
		/// </summary>
		public GraphicsDevice GraphicsDevice => Core.Instance.GraphicsDevice;
		/// <summary>
		/// The renderer managing the graphics for this scene.
		/// </summary>
		public readonly SceneRenderer Renderer;

		/// <summary>
		/// If this scene has been disposed.
		/// </summary>
		protected bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		/// <summary>
		/// Initializes the base Scene functionality.
		/// </summary>
		/// <param name="name">The name of the scene, cannot be null or empty.</param>
		protected Scene(string name)
		{
			if (String.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Scene name cannot be null or empty.", nameof(name));
			Name = name;

			Renderer = new SceneRenderer(this);
		}
		~Scene()
		{
			dispose(false);
		}

		#region User Functions
		/// <summary>
		/// Called when a new scene is queued to become the active scene, and this scene is either the newely queued
		/// scene, or the currently active scene that will be replaced. This function is synchronous on the main thread,
		/// and should not contain code that will cause the app to hang.
		/// </summary>
		/// <param name="newScene">
		/// <c>true</c> if this scene is the new scene, <c>false</c> if this is the scene being replaced.
		/// </param>
		protected virtual void OnQueued(bool newScene) { }

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
		/// Called when this scene is disposed. Should be used to perform any last-minute cleanup, and is the last
		/// function called on this scene while it is alive. If the application exits unexpectedly, this is the only
		/// function that will be called.
		/// </summary>
		/// <param name="disposing">
		/// <c>true</c> if the scene was disposed manually, <c>false</c> otherwise (garbage collected).
		/// </param>
		protected virtual void OnDispose(bool disposing) { }

		/// <summary>
		/// Called when the backbuffer size changes. The Scene render target will already be rebuilt by the time this
		/// function is called.
		/// </summary>
		/// <param name="oldSize">The old rendertarget size.</param>
		/// <param name="newSize">The new rendertarget size.</param>
		protected virtual void OnBackbufferResize(Extent oldSize, Extent newSize) { }

		#region Core Loop
		/// <summary>
		/// Called at the beginning of the frame, after <see cref="Core.BeginFrame"/>, but before any of the update
		/// code.
		/// </summary>
		protected virtual void BeginFrame() { }
		/// <summary>
		/// Called to perform the main update functionality in a frame. Called after <see cref="Core.Update"/>.
		/// </summary>
		protected virtual void Update() { }
		/// <summary>
		/// Called between the update and render functionality. Called after <see cref="Core.MidFrame"/>.
		/// </summary>
		protected virtual void MidFrame() { }
		/// <summary>
		/// Called to perform the main render functionality in a frame. Called after <see cref="Core.Render"/>.
		/// </summary>
		protected virtual void Render() { }
		/// <summary>
		/// Called after the rendering functionality, and after <see cref="Core.EndFrame"/>.
		/// </summary>
		protected virtual void EndFrame() { }
		#endregion // Core Loop
		#endregion // User Functions

		#region User Function Calling
		internal void DoOnQueued(bool newScene) => OnQueued(newScene);
		internal void DoStart()
		{
			// TODO: Use backbuffer size
			Renderer.Rebuild(Core.Instance.Window.Size.Width, Core.Instance.Window.Size.Height);
			OnStart();
		}
		internal void DoRemove() => OnRemove();
		internal void DoBeginFrame()
		{
			Renderer.Reset();
			BeginFrame();
		}
		internal void DoUpdate() => Update();
		internal void DoMidFrame() => MidFrame();
		internal void DoRender() => Render();
		internal void DoEndFrame() => EndFrame();
		#endregion // User Function Calling

		internal void BackbufferResize(Extent newSize)
		{
			if (Renderer.BackbufferSize == newSize)
				return;

			var oldSize = Renderer.BackbufferSize;
			Renderer.Rebuild(newSize.Width, newSize.Height);

			OnBackbufferResize(oldSize, newSize);
		}

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

				Renderer.Dispose();
			}

			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
