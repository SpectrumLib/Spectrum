/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;

namespace Spectrum
{
	// Internal class that controls the lifetimes and ticking of coroutine instances
	internal static class CoroutineManager
	{
		private static readonly List<Coroutine> _Coroutines = new List<Coroutine>();
		private static readonly List<Coroutine> _ToAdd = new List<Coroutine>();
		private static bool _Ticking = false; // We cannot directly add or remove from _Coroutines when ticking

		public static void Tick()
		{
			_Ticking = true;

			float rdelta = Time.UnscaledDelta,
				  sdelta = Time.Delta;

			// Tick and update all coroutines
			_Coroutines.ForEach(cr =>
			{
				if (!cr.Running)
					return;

				// Update the wait objects
				bool tick = false;
				if (cr.WaitObj.Frames > 0)
				{
					cr.WaitObj.Frames -= 1;
					tick = cr.WaitObj.Frames == 0;
				}
				if (cr.WaitObj.Time > 0)
				{
					float ntime = cr.WaitObj.Time - (cr.UseUnscaledTime ? rdelta : sdelta);
					cr.WaitObj.Time = Math.Max(ntime, 0);
					tick = cr.WaitObj.Time <= 0;
				}
				if (cr.WaitObj.Coroutine != null && !cr.WaitObj.Coroutine.Running)
				{
					cr.WaitObj.Coroutine = null;
					tick = true;
				}

				// Tick and update based on return value
				if (tick)
				{
					++cr.TickCount;
					var ret = cr.Tick();

					if (ret == null)
						return;
					else if (ReferenceEquals(ret, Coroutine.END))
						cr.Running = false;
					else if (ret is Coroutine.WaitForSecondsImpl)
						cr.WaitObj.Time = ((Coroutine.WaitForSecondsImpl)ret).Time;
					else if (ret is Coroutine.WaitForFramesImpl)
						cr.WaitObj.Frames = ((Coroutine.WaitForFramesImpl)ret).Frames;
					else if (ret is Coroutine)
						cr.WaitObj.Coroutine = ret as Coroutine;
					else
						throw new InvalidOperationException($"Coroutine returned invalid object of type {ret.GetType()}.");
				}
			});

			// Remove coroutines that are finished
			_Coroutines.RemoveAll(cr =>
			{
				if (!cr.Running)
					cr.OnRemove();
				return !cr.Running;
			});

			// Add any coroutines that were added while we were ticking
			_ToAdd.ForEach(cr => _Coroutines.Add(cr));
			_ToAdd.Clear();

			_Ticking = false;
		}

		public static void AddCoroutine(Coroutine c)
		{
			c.Running = true;
			if (_Ticking)
				lock (_ToAdd) { _ToAdd.Add(c); }
			else
				_Coroutines.Add(c);
		}

		public static void Terminate()
		{
			_Coroutines.ForEach(cr => cr.OnRemove());
			_Coroutines.Clear();
			_ToAdd.Clear();
		}
	}
}
