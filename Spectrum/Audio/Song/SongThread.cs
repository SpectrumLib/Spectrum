/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.Threading;

namespace Spectrum.Audio
{
	internal static class SongThread
	{
		public const int SLEEP_TIMEOUT = 100; // 100 ms

		#region Fields
		// The thread object hosting the song thread
		private static Thread _SongThread = null;
		private static ManualResetEvent _SleepEvent = null;

		// Flag and lock for stopping the thread
		private static bool _ShouldStop = false;

		// Lists of songs in different states
		private static readonly List<Song> _ActiveSongs = new List<Song>(5);
		private static readonly List<Song> _ToAdd = new List<Song>(5);
		private static readonly List<Song> _ToRemove = new List<Song>(5);
		#endregion // Fields

		public static void AddSong(Song s)
		{
			lock (_ToAdd) { _ToAdd.Add(s); }
		}

		public static void RemoveSong(Song s)
		{
			lock (_ToRemove) { _ToRemove.Add(s); }
		}

		public static void Start()
		{
			if (_SongThread != null)
				return;

			_SleepEvent = new ManualResetEvent(false);
			_SongThread = new Thread(() =>
			{
				while (true)
				{
					if (_ShouldStop)
						break;

					// Perform the additions
					lock (_ToAdd)
					{
						if (_ToAdd.Count > 0)
						{
							_ActiveSongs.AddRange(_ToAdd);
							_ToAdd.Clear();
						}
					}

					// Update the active songs
					foreach (var asong in _ActiveSongs)
						asong.Update();

					// Perform the removals
					lock (_ToRemove)
					{
						if (_ToRemove.Count > 0)
						{
							foreach (var tr in _ToRemove)
								_ActiveSongs.Remove(tr);
							_ToRemove.Clear();
						}

						if (_ActiveSongs.Count == 0)
							break; // No more active songs, spin down thread to conserve resources
					}

					// Stop the thread from busy-spinning
					_SleepEvent.WaitOne(SLEEP_TIMEOUT);
					_SleepEvent.Reset();
				}

				_SongThread = null; // Clean up for the next thread launch
				_SleepEvent = null;
			});

			_ShouldStop = false;
			_SongThread.Start();
		}

		public static void Stop()
		{
			if (_SongThread == null)
				return;

			_ShouldStop = true;
			_SleepEvent.Set();
			_SongThread?.Join();
		}
	}
}
