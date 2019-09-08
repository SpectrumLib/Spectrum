/*
 * GNU LGPLv3 License - Copyright (c) The Spectrum Team
 * This file is subject to the terms and conditions of the GNU LGPLv3 license, the text of which can be found in the
 * 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/LGPL-3.0>.
 */
using System;
using System.Diagnostics;
using System.Linq;

namespace Spectrum
{
	/// <summary>
	/// Provides access to and control of the application time, as well as timing statistics.
	/// </summary>
	public static class Time
	{
		#region Fields
		/// <summary>
		/// The time elapsed since the last frame, subject to the value of <see cref="Scale"/>.
		/// </summary>
		public static TimeSpan DeltaSpan { get; private set; } = TimeSpan.Zero;
		/// <summary>
		/// The unaltered wall-time elapsed since the last frame.
		/// </summary>
		public static TimeSpan UnscaledDeltaSpan { get; private set; } = TimeSpan.Zero;
		/// <summary>
		/// The total amount of wall-time elapsed since the start of the application.
		/// </summary>
		public static TimeSpan ElapsedSpan { get; private set; } = TimeSpan.Zero;
		/// <summary>
		/// The total amount of wall-time elapsed at the beginning of the last application frame.
		/// </summary>
		public static TimeSpan LastElapsedSpan { get; private set; } = TimeSpan.Zero;

		/// <summary>
		/// The value of <see cref="DeltaSpan"/> expressed as whole and fractional seconds.
		/// </summary>
		public static float Delta { get; private set; } = 0;
		/// <summary>
		/// The value of <see cref="UnscaledDelta"/> expressed as whole and fractional seconds.
		/// </summary>
		public static float UnscaledDelta { get; private set; } = 0;
		/// <summary>
		/// The value of <see cref="ElapsedSpan"/> expressed as whole and fractional seconds.
		/// </summary>
		public static float Elapsed { get; private set; } = 0;
		/// <summary>
		/// The value of <see cref="LastElapsedSpan"/> expressed as whole and fractional seconds.
		/// </summary>
		public static float LastElapsed { get; private set; } = 0;

		private static float _TimeScale = 1.0f; // The current time scale
		private static float? _NewTimeScale = null; // The requested time scale for the next frame, if there is one
		/// <summary>
		/// The current scaling factor for the global delta time. This can be set to any positive value, including zero,
		/// and is clamped automatically. Changes to the scaling do not apply until the next application frame.
		/// </summary>
		public static float Scale
		{
			get => _TimeScale;
			set => _NewTimeScale = value < 0 ? 0 : value;
		}
		/// <summary>
		/// Event that is raised at the beginning of the application frame for a change to <see cref="Scale"/>.
		/// </summary>
		public static event TimeScaleChangeCallback ScaleChanged;

		/// <summary>
		/// The resolution of the underlying system timer, in nanoseconds.
		/// </summary>
		public static readonly uint Resolution;
		/// <summary>
		/// If the underlying system timer is considered high resolution. Spectrum defines this as a resolution of 10
		/// microseconds or less (<see cref="Resolution"/> &lt;= 10_000).
		/// </summary>
		public static bool IsHighResolution => Resolution <= 10_000;

		/// <summary>
		/// The current frame number of the application - a value of 0 implies that the main loop has not started yet.
		/// </summary>
		public static ulong FrameCount { get; private set; } = 0;

		// The stopwatch used to track time.
		private static readonly Stopwatch _Timer = null;

		// Keeps a history of the deltas for the last n frames to calculate FPS with
		private const uint FPS_HISTORY_SIZE = 10;
		private static readonly float[] _FpsHistory = new float[FPS_HISTORY_SIZE];
		private static uint _CurrIndex = 0; // Used to make the fps history array a circular buffer

		/// <summary>
		/// The current FPS of the application, made by averaging the deltas of the last 10 frames.
		/// </summary>
		public static float FPS { get; private set; } = 0;
		/// <summary>
		/// The raw (un-averaged) fps of the most recent frame.
		/// </summary>
		public static float RawFPS { get; private set; } = 0;
		#endregion // Fields

		static Time()
		{
			Resolution = Math.Max((uint)(1e9 / Stopwatch.Frequency), 1);
			_Timer = Stopwatch.StartNew();
			Array.Clear(_FpsHistory, 0, (int)FPS_HISTORY_SIZE);
		}

		// Update the timing values
		internal static void Frame()
		{
			++FrameCount;

			// Check for a changing time scale
			if (_NewTimeScale.HasValue)
			{
				float old = _TimeScale;
				_TimeScale = _NewTimeScale.Value;
				ScaleChanged?.Invoke(old, _TimeScale);
				_NewTimeScale = null;
			}

			// Update the timing values
			LastElapsed = (float)(LastElapsedSpan = ElapsedSpan).TotalSeconds;
			Elapsed = (float)(ElapsedSpan = _Timer.Elapsed).TotalSeconds;
			UnscaledDelta = (float)(UnscaledDeltaSpan = ElapsedSpan - LastElapsedSpan).TotalSeconds;
			Delta = (float)(DeltaSpan = TimeSpan.FromTicks((long)(UnscaledDeltaSpan.Ticks * _TimeScale))).TotalSeconds;

			// Update the FPS history and re-calculate (small bump to prevent division by zero)
			RawFPS = _FpsHistory[_CurrIndex] = 1000f / ((float)UnscaledDeltaSpan.TotalMilliseconds + 0.01f);
			FPS = _FpsHistory.Sum() / Math.Min(FrameCount, FPS_HISTORY_SIZE);
			_CurrIndex = (_CurrIndex + 1) % FPS_HISTORY_SIZE;
		}

		/// <summary>
		/// Will return <c>true</c> only on the first frame that is at or greater than the given elapsed wall time.
		/// </summary>
		/// <param name="time">The elapsed wall time to check, in seconds.</param>
		/// <returns><c>true</c> only on the first frame at or past the given time.</returns>
		public static bool IsTime(float time) => (LastElapsed < time) && (Elapsed >= time);

		/// <summary>
		/// Returns <c>true</c> on every frame that is the first frame immediately past a multiple of the given time. 
		/// E.g. <c>IsTimeMultiple(1)</c> will return true on every first frame immediately past every whole second.
		/// </summary>
		/// <param name="time">The elapsed wall time to check multiples of.</param>
		/// <returns><c>true</c> on each first frame past any multiple of the given wall time.</returns>
		public static bool IsTimeMultiple(float time) => (Elapsed % time) < (LastElapsed % time);
	}

	/// <summary>
	/// Callback for signalling a change in the value of <see cref="Time.Scale"/>.
	/// </summary>
	/// <param name="oldScale">The old time scaling value.</param>
	/// <param name="newScale">The new time scaling value.</param>
	public delegate void TimeScaleChangeCallback(float oldScale, float newScale);
}
