using System;
using System.Diagnostics;
using System.Linq;

namespace Spectrum
{
	/// <summary>
	/// Allows access to and control of the application time. Provides both unaltered wall time and scaled time.
	/// </summary>
	public static class Time
	{
		#region Fields
		/// <summary>
		/// The time elapsed since the last frame, subject to the value of <see cref="Scale"/>.
		/// </summary>
		public static TimeSpan DeltaTime { get; private set; } = TimeSpan.Zero;
		/// <summary>
		/// The unaltered wall-time elapsed since the last frame.
		/// </summary>
		public static TimeSpan RealDeltaTime { get; private set; } = TimeSpan.Zero;
		/// <summary>
		/// The total amount of wall-time elapsed since the start of the application.
		/// </summary>
		public static TimeSpan ElapsedTime { get; private set; } = TimeSpan.Zero;
		/// <summary>
		/// The total amount of wall-time elapsed in the last frame since the start of the application.
		/// </summary>
		public static TimeSpan LastElapsedTime { get; private set; } = TimeSpan.Zero;

		// The current time scale
		private static float s_timeScale = 1.0f;
		// The requested time scale for the next frame, if there is one
		private static float? s_newTimeScale = null;
		/// <summary>
		/// Gets or set the current scaling factor for the <see cref="DeltaTime"/> field. This can be used to implement
		/// slow or fast motion, or pausing if the value is set to zero. Value cannot be less than zero, and will be
		/// clamped automatically. This does not affect the values of <see cref="RealDeltaTime"/> or 
		/// <see cref="ElapsedTime"/>. Changes to this value will not take effect until the next frame.
		/// </summary>
		public static float Scale
		{
			get { return s_timeScale; }
			set { s_newTimeScale = value < 0 ? 0 : value; }
		}
		/// <summary>
		/// Callback for when the value of <see cref="Scale"/> changes. This will be called at the beginning of the
		/// first frame after the time scale changes.
		/// </summary>
		public static event TimeScaleChangedCallback TimeScaleChanged;

		/// <summary>
		/// The value of <see cref="DeltaTime"/> expressed as whole and fractional seconds.
		/// </summary>
		public static float Delta => (float)DeltaTime.TotalSeconds;
		/// <summary>
		/// The value of <see cref="RealDeltaTime"/> expressed as whole and fractional seconds.
		/// </summary>
		public static float RealDelta => (float)RealDeltaTime.TotalSeconds;
		/// <summary>
		/// The value of <see cref="ElapsedTime"/> expressed as whole and fractional seconds.
		/// </summary>
		public static float Elapsed => (float)ElapsedTime.TotalSeconds;
		/// <summary>
		/// The value of <see cref="LastElapsedTime"/> expressed as whole and fractional seconds.
		/// </summary>
		public static float LastElapsed => (float)LastElapsedTime.TotalSeconds;

		/// <summary>
		/// The smallest time difference measureable by the underlying timer, in nanoseconds.
		/// </summary>
		public static readonly uint Resolution;
		/// <summary>
		/// Gets if the underlying timer used is considered high resolution. Spectrum defines "high resolution"
		/// as having the smallest measureable time difference as &lt;=0.01ms (10 us), i.e. the value of
		/// <see cref="Resolution"/> is &lt;= 10,000.
		/// </summary>
		public static readonly bool IsHighResolution;

		/// <summary>
		/// The current frame number of the application, a value of 0 implies that the main loop has not started yet.
		/// </summary>
		public static ulong FrameCount { get; private set; } = 0;

		// The stopwatch used to track time.
		private static Stopwatch s_timer = null;
		// The time at the last frame
		private static TimeSpan s_lastTime = TimeSpan.Zero;

		// Keeps a history of the deltas for the last n frames to calculate FPS with
		private const uint FPS_HISTORY_SIZE = 15;
		private static readonly float[] s_fpsHistory = new float[FPS_HISTORY_SIZE];
		// The current frame in the s_fpsHistory array to implement a circular buffer
		private static uint s_currIndex = 0;
		private static uint s_nextIndex => (s_currIndex + 1) % FPS_HISTORY_SIZE;

		/// <summary>
		/// The current FPS of the application, made by averaging the deltas of the last 15 frames.
		/// </summary>
		public static float FPS { get; private set; } = 0;
		/// <summary>
		/// The raw (un-averaged) fps of the last frame.
		/// </summary>
		public static float RawFPS { get; private set; } = 0;
		#endregion // Fields

		static Time()
		{
			Resolution = Math.Max((uint)(1e9 / Stopwatch.Frequency), 1);
			IsHighResolution = Resolution <= 10_000;
			s_timer = Stopwatch.StartNew();
			Array.Clear(s_fpsHistory, 0, (int)FPS_HISTORY_SIZE);
		}

		internal static void Frame()
		{
			++FrameCount;

			// Check for a changing time scale
			if (s_newTimeScale.HasValue)
			{
				float old = s_timeScale;
				s_timeScale = s_newTimeScale.Value;
				TimeScaleChanged?.Invoke(old, s_timeScale);
				s_newTimeScale = null;
			}

			// Update the timing values
			LastElapsedTime = ElapsedTime;
			ElapsedTime = s_timer.Elapsed;
			RealDeltaTime = ElapsedTime - s_lastTime;
			DeltaTime = TimeSpan.FromTicks((long)(RealDeltaTime.Ticks * s_timeScale));
			s_lastTime = ElapsedTime;

			// Update the FPS history and re-calculate (small bump to prevent division by zero)
			RawFPS = s_fpsHistory[s_currIndex] = 1000f / ((float)RealDeltaTime.TotalMilliseconds + 0.01f);
			float last = s_fpsHistory[s_nextIndex];
			if (FrameCount > FPS_HISTORY_SIZE)
				FPS = s_fpsHistory.Average();
			else
				FPS = s_fpsHistory.Sum() / FrameCount;
			s_currIndex = s_nextIndex;
		}

		/// <summary>
		/// Will return true only on the first frame that is at or greater than the given elapsed wall time.
		/// </summary>
		/// <param name="time">The elapsed wall time to check, in seconds.</param>
		/// <returns>True on the first frame at or past the given time.</returns>
		public static bool IsTime(float time) => (LastElapsed < time) && (Elapsed >= time);

		/// <summary>
		/// Returns true on every frame that is the first frame immediately past a multiple of the given time. E.g.
		/// <c>IsTimeMultiple(1)</c> will return true on every first frame immediately past every whole second.
		/// </summary>
		/// <param name="time">The elapsed wall time to check multiples of.</param>
		/// <returns>True on each first frame past any multiple of the given wall time.</returns>
		public static bool IsTimeMultiple(float time) => (Elapsed % time) < (LastElapsed % time);

		/// <summary>
		/// Returns true if the current <see cref="FrameCount"/> is equal to the given frame number.
		/// </summary>
		/// <param name="frame">The frame number to check for.</param>
		/// <returns>If the current frame count is equal to the given frame number.</returns>
		public static bool IsFrame(ulong frame) => FrameCount == frame;
	}

	/// <summary>
	/// Callback for handling a timescale change in the <see cref="Time"/> class.
	/// </summary>
	/// <param name="oldScale">The old timescale.</param>
	/// <param name="newScale">The new timescale.</param>
	public delegate void TimeScaleChangedCallback(float oldScale, float newScale);
}
