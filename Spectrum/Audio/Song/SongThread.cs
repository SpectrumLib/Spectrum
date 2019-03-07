using System;
using System.Collections.Generic;
using System.Threading;

namespace Spectrum.Audio
{
	// Background thread code which manages song streaming
	internal static class SongThread
	{
		#region Fields
		// The thread object hosting the song thread
		private static Thread s_songThread = null;

		// Flag and lock for stopping the thread
		private static readonly object s_stopLock = new object();
		private static bool s_shouldStop = false;

		// Lists of songs in different states
		private static readonly List<Song> s_activeSongs = new List<Song>(5);
		private static readonly List<Song> s_toAdd = new List<Song>(5);
		private static readonly List<Song> s_toRemove = new List<Song>(5);
		#endregion // Fields

		public static void AddSong(Song s)
		{
			lock (s_toAdd) { s_toAdd.Add(s); }
		}

		public static void RemoveSong(Song s)
		{
			lock (s_toRemove) { s_toRemove.Add(s); }
		}

		public static void Start()
		{
			if (s_songThread != null)
				return;

			s_songThread = new Thread(() =>
			{
				while (true)
				{
					lock (s_stopLock)
					{
						if (s_shouldStop)
							break;
					}

					// Perform the additions
					lock (s_toAdd)
					{
						if (s_toAdd.Count > 0)
						{
							s_activeSongs.AddRange(s_toAdd);
							s_toAdd.Clear();
						}
					}

					// Update the active songs
					foreach (var asong in s_activeSongs)
						asong.Update();

					// Perform the removals
					lock (s_toRemove)
					{
						if (s_toRemove.Count > 0)
						{
							foreach (var tr in s_toRemove)
								s_activeSongs.Remove(tr);
							s_toRemove.Clear();
						}

						if (s_activeSongs.Count == 0)
							break; // No more active songs, spin down thread to conserve resources
					}

					// Stop the thread from busy-spinning
					Thread.Sleep(100);
				}

				s_songThread = null; // Clean up for the next thread launch
			});

			s_shouldStop = false;
			s_songThread.Start();
		}

		public static void Stop()
		{
			if (s_songThread == null)
				return;

			lock (s_stopLock) { s_shouldStop = true; }
			s_songThread.Join();
		}
	}
}
