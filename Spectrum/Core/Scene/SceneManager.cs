using System;
using System.Collections.Generic;
using System.Reflection;

namespace Spectrum
{
	/// <summary>
	/// Manages the lifecycle of <see cref="AppScene"/> instances, as well as queueing and executing scene transitions.
	/// </summary>
	public static class SceneManager
	{
		#region Fields
		// Tracks if a state has been created through the correct functions
		internal static bool IsCreatingScene = false;
		internal static readonly object CreateLock = new object();
		// Tracks if we are disposing the scenes, to prevent writing to an iterator
		private static bool s_isDisposing = false;
		// Holds all created app scenes for their entire lifecycle
		private static readonly List<AppScene> s_scenes = new List<AppScene>();

		/// <summary>
		/// The currently active scene.
		/// </summary>
		public static AppScene ActiveScene { get; private set; } = null;
		private static AppScene s_queuedScene = null;
		/// <summary>
		/// If there is a scene change currently queued.
		/// </summary>
		public static bool SceneChanging { get; private set; } = false;
		#endregion // Fields

		#region Frame Functions
		internal static void PreUpdate()
		{
			if (SceneChanging) // TODO: Check the transition is complete first as well, once implemented
				DoSceneChange();

			ActiveScene?.DoPreUpdate();
		}

		internal static void Update()
		{
			ActiveScene?.DoUpdate();
		}

		internal static void PostUpdate()
		{
			ActiveScene?.DoPostUpdate();
		}

		internal static void PreRender()
		{
			ActiveScene?.DoPreRender();
		}

		internal static void Render()
		{
			ActiveScene?.DoRender();
		}

		internal static void PostRender()
		{
			ActiveScene?.DoPostRender();
		}
		#endregion // Frame Functions

		#region Scene Changing
		/// <summary>
		/// Queues a change of the active scene to the passed scene. This will notify the current active scene of the
		/// pending change. The actual change will not start until the next application frame.
		/// </summary>
		/// <param name="newScene">The new scene to change to, can be null if no scene should be shown.</param>
		public static void ChangeScene(AppScene newScene /* TODO: Scene Transitions */)
		{
			if (newScene != null && ReferenceEquals(newScene, ActiveScene))
				throw new ArgumentException("Cannot change the active scene to the same scene instance");
			if (s_queuedScene != null)
				throw new InvalidOperationException("Cannot queue a scene change if there is already a change in progress");

			s_queuedScene = newScene;
			SceneChanging = true;

			// Notify
			s_queuedScene?.QueueChange(false);
			ActiveScene?.QueueChange(false);
		}

		// Called to perform the actual transition between active scenes
		private static void DoSceneChange()
		{
			// Remove the current scene, and GC collect as there may be many objects released in the disposal
			ActiveScene?.Remove();
			ActiveScene?.Dispose();
			if (ActiveScene != null)
				GC.Collect();

			// Load the new scene, and GC collect to remove the many temporary objects created during loading
			ActiveScene = s_queuedScene;
			ActiveScene?.Start();
			s_queuedScene = null;
			if (ActiveScene != null)
				GC.Collect();

			SceneChanging = false;
		}
		#endregion // Scene Changing

		#region Scene Creation
		/// <summary>
		/// Creates a new scene with the given arguments, initializes it, and begins tracking its lifecycle.
		/// </summary>
		/// <typeparam name="T">The scene type to create.</typeparam>
		/// <param name="args">The arguments to pass to the scene constructor.</param>
		/// <returns>The new scene instance.</returns>
		public static T CreateScene<T>(params object[] args)
			where T : AppScene
		{
			lock (CreateLock) { IsCreatingScene = true; }

			Type sceneType = typeof(T);
			T scene = null;

			if (sceneType.IsAbstract)
				throw new InvalidOperationException($"Cannot create instance of abstract scene type '{sceneType.Name}'");

			try
			{
				scene = Activator.CreateInstance(sceneType, args) as T;
				if (scene == null)
					throw new InvalidOperationException($"The scene was created but could not be cast to the valid type");
				scene.Initialize();

				lock (s_scenes) { s_scenes.Add(scene); }
				return scene;
			}
			catch (TargetInvocationException e)
			{
				throw new InvalidOperationException($"The scene constructor threw an exception: {e.Message}", e);
			}
			catch (MemberAccessException e)
			{
				throw new InvalidOperationException($"The scene type '{sceneType.Name}' does not have a publically " +
					$"accessible constructor that matches the given arguments", e);
			}
			finally
			{
				lock (CreateLock) { IsCreatingScene = false; }
			}
		}
		#endregion // Scene Creation

		internal static void Shutdown()
		{
			// Deal with active scene
			ActiveScene?.QueueChange(false);
			ActiveScene?.Remove();
			ActiveScene?.Dispose();
			ActiveScene = null;

			// Might be a queued scene
			s_queuedScene?.Dispose();
			s_queuedScene = null;

			// Deal with other cached states
			s_isDisposing = true;
			s_scenes.ForEach(scene => scene.Dispose());
			s_scenes.Clear();
			s_isDisposing = false;
		}

		internal static void RemoveScene(AppScene scene)
		{
			if (!s_isDisposing)
			{
				lock (s_scenes) { s_scenes.Remove(scene); }
			}
		}
	}
}
