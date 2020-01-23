/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Spectrum
{
	/// <summary>
	/// Utility functionality for getting information about, and working with, threading.
	/// </summary>
	public static class Threading
	{
		#region Fields
		/// <summary>
		/// The integer ID of the main application thread.
		/// </summary>
		public static readonly int MainThreadId;

		/// <summary>
		/// Gets if the current executing thread is the main application thread.
		/// </summary>
		public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == MainThreadId;

		// List of actions to execute on the next frame in the main thread
		private static readonly Queue<Action> _Actions = new Queue<Action>();
		#endregion // Fields

		/// <summary>
		/// Throws an exception if this function is executed on any thread other than the main application thread.
		/// </summary>
		/// <param name="callerName">The calling function name (filled in at compile time).</param>
		/// <param name="callerLine">The calling function line (filled in at compile time).</param>
		/// <exception cref="InvalidOperationException">
		///		The function is called on a thread that is not the main application thread.</exception>
		public static void EnsureMainThread(
			[CallerMemberName] string callerName = "",
			[CallerLineNumber] int callerLine = 0
		)
		{
			if (!IsMainThread)
			{
				var loc = $"'{callerName}'[line {callerLine}]";
				throw new InvalidOperationException($"Operation at {loc} not called on main thread.");
			}
		}

		/// <summary>
		/// Runs the action on the main application thread. Executes immediately if the current execution thread is the
		/// main application thread, otherwise blocks until the main thread can execute the action and return. Blocking
		/// actions are executed by the core loop immediately after coroutines are ticked.
		/// </summary>
		/// <param name="action">The action to execute on the main thread.</param>
		/// <returns>The amount of time the action had to wait before execution.</returns>
		public static TimeSpan BlockOnMainThread(Action action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			if (IsMainThread)
			{
				action();
				return TimeSpan.Zero;
			}

			var evt = new ManualResetEventSlim(false);
			Stopwatch sw = Stopwatch.StartNew();
			TimeSpan delay = TimeSpan.Zero;
			AddAction(() =>
			{
				delay = sw.Elapsed;
				action();
				evt.Set();
			});
			evt.Wait();
			return delay;
		}

		// Queues an action for execution on the main thread
		internal static void AddAction(Action action)
		{
			lock (_Actions)
				_Actions.Enqueue(action);
		}

		// Called from the main thread to run the pending actions
		internal static void RunActions()
		{
			if (!IsMainThread)
				throw new InvalidOperationException("Threading actions must be run on the main thread.");

			while (_Actions.Count > 0)
			{
				Action a = null;
				lock (_Actions)
					a = _Actions.Dequeue();
				a();
			}
		}

		static Threading()
		{
			MainThreadId = Thread.CurrentThread.ManagedThreadId;
		}
	}
}
