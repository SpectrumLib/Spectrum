﻿using System;
using System.Collections.Generic;

namespace Spectrum
{
	// Internal class that controls the lifetimes and ticking of coroutine instances
	internal static class CoroutineManager
	{
		private static readonly List<Coroutine> s_coroutines = new List<Coroutine>();
		private static readonly List<Coroutine> s_toAdd = new List<Coroutine>();
		private static readonly List<Action> s_events = new List<Action>();
		private static readonly List<Action> s_addEvents = new List<Action>();
		private static bool s_ticking = false; // We cannot directly add or remove from s_coroutines when ticking

		public static void Tick()
		{
			s_ticking = true;

			float rdelta = Time.RealDelta, 
				  sdelta = Time.Delta;

			// Tick and update all coroutines
			s_coroutines.ForEach(cr =>
			{
				if (!cr.Running)
					return;

				// Update the wait objects
				if (cr.Wait.Time > 0)
				{
					float ntime = cr.Wait.Time - (cr.UseUnscaledTime ? rdelta : sdelta);
					cr.Wait.Time = Math.Max(ntime, 0);
				}
				if (!cr.Wait.Coroutine?.Running ?? true)
					cr.Wait.Coroutine = null;

				// Tick and update based on return value
				if (cr.Wait.Time <= 0 && cr.Wait.Coroutine == null)
				{
					++cr.TickCount;
					var ret = cr.Tick();

					if (ret == null)
						return;
					else if (ReferenceEquals(ret, Coroutine.END))
						cr.Running = false;
					else if (ret is Coroutine.WaitForSecondsImpl)
						cr.Wait.Time = ((Coroutine.WaitForSecondsImpl)ret).WaitTime;
					else if (ret is Coroutine)
						cr.Wait.Coroutine = ret as Coroutine;
					else { /* Type not understood, maybe make this an error later. */ }
				}
			});

			// Remove coroutines that are finished
			s_coroutines.RemoveAll(cr =>
			{
				if (!cr.Running)
					cr.OnRemove();
				return !cr.Running;
			});

			// Add any coroutines that were added while we were ticking
			s_toAdd.ForEach(cr => s_coroutines.Add(cr));
			s_toAdd.Clear();

			// Run all of the posted events
			lock (s_addEvents)
			{
				s_events.AddRange(s_addEvents);
				s_addEvents.Clear();
			}
			s_events.ForEach(a => a());
			s_events.Clear();

			s_ticking = false;
		}

		// Used to post an event to be run on the main thread at the next time the events are ticked
		// This can be used to post events from other threads that must be run on the main thread
		public static void PostEvent(Action a)
		{
			if (s_ticking)
				lock (s_addEvents) { s_addEvents.Add(a); }
			else
				s_events.Add(a);
		}

		public static void AddCoroutine(Coroutine c)
		{
			c.Running = true;
			if (s_ticking)
				lock (s_toAdd) { s_toAdd.Add(c); }
			else
				s_coroutines.Add(c);
		}

		public static void Cleanup()
		{
			s_coroutines.ForEach(cr => cr.OnRemove());
			s_coroutines.Clear();
			s_toAdd.Clear();
		}
	}
}
