/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;

namespace Spectrum
{
	/// <summary>
	/// Manages the lifecycle of the active <see cref="Scene"/> instance, as well as queueing and executing 
	/// scene transitions.
	/// </summary>
	public static class SceneManager
	{
		#region Fields
		/// <summary>
		/// The currently active scene.
		/// </summary>
		public static Scene ActiveScene { get; private set; } = null;
		private static Scene _QueuedScene = null;
		/// <summary>
		/// If there is a scene change currently queued.
		/// </summary>
		public static bool IsSceneChanging { get; private set; } = false;
		#endregion // Fields

		#region Frame Functions
		internal static void BeginFrame()
		{
			if (IsSceneChanging)
				DoSceneChange();

			ActiveScene?.DoBeginFrame();
		}

		internal static void Update() => ActiveScene?.DoUpdate();

		internal static void MidFrame() => ActiveScene?.DoMidFrame();

		internal static void Render() => ActiveScene?.DoRender();

		internal static void EndFrame() => ActiveScene?.DoEndFrame();
		#endregion // Frame Functions

		#region Scene Changing
		/// <summary>
		/// Queues a change of the active scene to the passed scene. This will notify the current active scene of the
		/// pending change. The actual change will not start until the next application frame.
		/// </summary>
		/// <param name="newScene">The new scene to change to, can be null if no scene should be shown.</param>
		/// <exception cref="ArgumentException">The new scene is the currently active scene.</exception>
		/// <exception cref="InvalidOperationException">A scene change is already in progress.</exception>
		public static void QueueScene(Scene newScene)
		{
			if (newScene != null && ReferenceEquals(newScene, ActiveScene))
				throw new ArgumentException("Cannot change the active scene to the same scene instance.");
			if (_QueuedScene != null)
				throw new InvalidOperationException("Cannot queue a scene change if there is already a change in progress.");

			_QueuedScene = newScene;
			IsSceneChanging = true;

			// Notify
			_QueuedScene?.DoOnQueued(true);
			ActiveScene?.DoOnQueued(false);
		}

		// Called to perform the actual transition between active scenes
		private static void DoSceneChange()
		{
			// Remove the current scene, and GC collect as there may be many objects released in the disposal
			ActiveScene?.DoRemove();
			ActiveScene?.Dispose();
			if (ActiveScene != null)
				GC.Collect();

			// Load the new scene, and GC collect to remove the many temporary objects created during loading
			ActiveScene = _QueuedScene;
			ActiveScene?.DoStart();
			_QueuedScene = null;
			if (ActiveScene != null)
				GC.Collect();

			IsSceneChanging = false;
		}
		#endregion // Scene Changing

		internal static void Terminate()
		{
			// Deal with active scene
			ActiveScene?.DoOnQueued(false);
			ActiveScene?.DoRemove();
			ActiveScene?.Dispose();
			ActiveScene = null;

			// Might be a queued scene
			_QueuedScene?.Dispose();
			_QueuedScene = null;
		}
	}
}
