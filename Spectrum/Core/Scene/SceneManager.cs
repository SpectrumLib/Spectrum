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
		#endregion // Fields

		#region Frame Functions
		internal static void PreUpdate()
		{

		}

		internal static void Update()
		{

		}

		internal static void PostUpdate()
		{

		}

		internal static void PreRender()
		{

		}

		internal static void Render()
		{

		}

		internal static void PostRender()
		{

		}
		#endregion // Frame Functions

		#region Scene Creation
		/// <summary>
		/// Creates a new scene of the given type using the no-args constructor, initializes it, and begins tracking
		/// its lifecycle.
		/// </summary>
		/// <typeparam name="T">The scene type to create.</typeparam>
		/// <returns>The new scene instance.</returns>
		public static T CreateScene<T>()
			where T : AppScene, new()
		{
			lock (CreateLock) { IsCreatingScene = true; }

			try
			{
				T scene = new T();
				scene.Initialize();

				lock (s_scenes) { s_scenes.Add(scene); }
				return scene;
			}
			finally
			{
				lock (CreateLock) { IsCreatingScene = false; }
			}
		}

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
			catch (MethodAccessException e)
			{
				throw new InvalidOperationException($"The scene type '{sceneType.Name}' does not have a publically " +
					$"accessible constructor that matches the given arguments", e);
			}
			catch (MemberAccessException e)
			{
				throw new InvalidOperationException($"Cannot create instance of abstract scene type '{sceneType.Name}'", e);
			}
			finally
			{
				lock (CreateLock) { IsCreatingScene = false; }
			}
		}
		#endregion // Scene Creation

		internal static void Shutdown()
		{
			// TODO: deal with active state

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
