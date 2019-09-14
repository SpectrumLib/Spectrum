using System;
using System.Collections;

namespace Spectrum
{
	/// <summary>
	/// Base class for implementing functionality that is delayed or repeated in a discretized manner. Coroutines 
	/// are updated/ticked immediately before the Update portion of the game loop.
	/// </summary>
	public abstract class Coroutine
	{
		/// <summary>
		/// Object returned from <see cref="Tick"/> to signal the coroutine to stop.
		/// </summary>
		protected internal static readonly object END = new object();

		#region Impl. Types
		/// <summary>
		/// Callback type for functions passed to time-based timers. Return value allows timers to repeat.
		/// </summary>
		/// <param name="time">The delay time for the timer that generated the callback.</param>
		/// <returns><c>true</c> to continue the timer, <c>false</c> to stop.</returns>
		public delegate bool TimeCallback(float time);

		/// <summary>
		/// Callback type for functions passed to frame-based timers. Return value allows timers to repeat.
		/// </summary>
		/// <param name="frames">The frame count for the timer delay.</param>
		/// <returns><c>true</c> to continue the timer, <c>false</c> to stop.</returns>
		public delegate bool FrameCallback(uint frames);

		// Manages the references to data used by coroutine waiting
		internal struct WaitObjects
		{
			public float Time;
			public Coroutine Coroutine;
			public uint Frames;
		}

		// Internal implementation of the return value for time-waiting functions
		internal struct WaitForSecondsImpl
		{
			public float Time;
		}

		// Internal implementation of the return value for frame-waiting functions
		internal struct WaitForFramesImpl
		{
			public uint Frames;
		}
		#endregion // Impl. Types

		#region Fields
		/// <summary>
		/// Gets the number of times that this coroutine has been ticked.
		/// </summary>
		public uint TickCount { get; internal set; } = 0;
		/// <summary>
		/// Gets if the coroutine should use unscaled wall time for timing operations. Defaults to false.
		/// This only has an effect in frames where <see cref="Time.Scale"/> is not 1.
		/// </summary>
		public virtual bool UseUnscaledTime => false;
		/// <summary>
		/// Gets if this corouting is currently actively ticking.
		/// </summary>
		public bool Running { get; internal set; } = false;

		// Manages the objects that this coroutine can wait on
		internal WaitObjects WaitObj = new WaitObjects { Time = 0, Coroutine = null, Frames = 0 };
		#endregion // Fields

		#region Inst. Functions
		/// <summary>
		/// Called by code external to the coroutine to force it to stop ticking.
		/// </summary>
		public void Stop()
		{
			if (Running) OnStop();
			Running = false;
		}

		/// <summary>
		/// Called to handle the coroutine being stopped by external code (when <see cref="Stop"/> is called).
		/// </summary>
		protected virtual void OnStop() { }

		/// <summary>
		/// Called when the coroutine is done ticking and is removed from the list of active coroutines. Any resource
		/// cleanup should be done here.
		/// </summary>
		protected internal virtual void OnRemove() { }

		/// <summary>
		/// Called in every iteration of the main application loop to continue to coroutine logic. The object that it
		/// returns controls how the coroutine continues in its execution.
		/// </summary>
		/// <returns>
		/// This function can return different objects based on how it wants to continue execution:
		/// <list type="bullet">
		///		<item>
		///			<term>`null`</term>
		///			<description>Return null to continue ticking in the next update as normal.</description>
		///		</item>
		///		<item>
		///			<term><see cref="END"/></term>
		///			<description>This signifies that the coroutine is done, and will no longer be active.</description>
		///		</item>
		///		<item>	
		///			<term><see cref="WaitTime(float)"/> or <see cref="WaitTime(TimeSpan)"/></term>
		///			<description>Either of these will halt the execution of the coroutine for the specified amount of time.</description>
		///		</item>
		///		<item>
		///			<term>A <see cref="Coroutine"/> instance</term>
		///			<description>The execution of this coroutine will halt until the returned coroutine is finished.</description>
		///		</item>
		/// </list>
		/// </returns>
		protected internal abstract object Tick();
		#endregion // Inst. Functions

		#region Wait Methods
		/// <summary>
		/// Returns an object that causes a coroutine to halt its execution for the given amount of time.
		/// </summary>
		/// <param name="time">The number of seconds to halt execution for.</param>
		/// <returns>The object internally representing a coroutine halt.</returns>
		public static object WaitTime(float time) => new WaitForSecondsImpl { Time = time };

		/// <summary>
		/// Returns an object that causes a coroutine to halt its execution for the given amount of time.
		/// </summary>
		/// <param name="time">The amount of time to halt execution for.</param>
		/// <returns>The object internally representing a coroutine halt.</returns>
		public static object WaitTime(TimeSpan time) => new WaitForSecondsImpl { Time = (float)time.TotalSeconds };

		/// <summary>
		/// Returns an object that causes the coroutine to halt its execution for the given number of app frames.
		/// </summary>
		/// <param name="frames">The number of frames to wait for.</param>
		/// <returns>The object internally representing a coroutine halt.</returns>
		public static object WaitFrames(uint frames) => new WaitForFramesImpl { Frames = frames };
		#endregion // Wait Methods

		#region Scheduling
		/// <summary>
		/// Sets the passed coroutine as active and starts ticking it.
		/// </summary>
		/// <param name="coroutine">The coroutine to start. Cannot already be started.</param>
		/// <returns>The started coroutine.</returns>
		public static Coroutine Start(Coroutine coroutine)
		{
			if (coroutine == null)
				throw new ArgumentNullException(nameof(coroutine));
			if (coroutine.Running)
				throw new InvalidOperationException("Cannot start a coroutine that is already running");
			CoroutineManager.AddCoroutine(coroutine);
			return coroutine;
		}

		/// <summary>
		/// Creates a coroutine using an IEnumerator as a tickable object.
		/// </summary>
		/// <param name="enumerator">The enumerator to tick. See <see cref="Tick"/> for return type information.</param>
		/// <param name="unscaled">If the ticker should ignore time scaling.</param>
		/// <returns>The started Coroutine.</returns>
		public static Coroutine Start(IEnumerator enumerator, bool unscaled = false)
		{
			if (enumerator == null)
				throw new ArgumentNullException(nameof(enumerator));
			var cr = new EnumeratorCoroutine(enumerator, unscaled);
			CoroutineManager.AddCoroutine(cr);
			return cr;
		}
		
		/// <summary>
		/// Creates a timer, which executes the passed action after a certain amount of time has passed.
		/// </summary>
		/// <param name="delay">The amount of time to delay before execution.</param>
		/// <param name="action">The action to execute after the delay.</param>
		/// <param name="repeat">If the action should be repeated, <c>false</c> to execute only once.</param>
		/// <param name="unscaled">If the timer should ignore time scaling.</param>
		/// <returns>The started Coroutine.</returns>
		public static Coroutine Schedule(float delay, Action action, bool repeat = false, bool unscaled = false)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));
			if (delay < 0)
				throw new ArgumentOutOfRangeException(nameof(delay), "Cannot specify a negative delay for timers");
			var cr = new TimerCoroutine(delay, repeat, unscaled, (delay) => { action(); return true; });
			CoroutineManager.AddCoroutine(cr);
			return cr;
		}

		/// <summary>
		/// Creates a timer, which executes the passed action after a certain amount of time has passed.
		/// </summary>
		/// <param name="delay">The amount of time to delay before execution.</param>
		/// <param name="action">The action to execute after the delay.</param>
		/// <param name="repeat">If the action should be repeated, <c>false</c> to execute only once.</param>
		/// <param name="unscaled">If the timer should ignore time scaling.</param>
		/// <returns>The started Coroutine.</returns>
		public static Coroutine Schedule(TimeSpan delay, Action action, bool repeat = false, bool unscaled = false) =>
			Schedule((float)delay.TotalSeconds, action, repeat, unscaled);

		/// <summary>
		/// Creates a timer, which executes the passed action after a certain amount of time has passed.
		/// </summary>
		/// <param name="delay">The amount of time to delay before execution.</param>
		/// <param name="action">The action to execute after the delay.</param>
		/// <param name="repeat">If the action should be repeated, <c>false</c> to execute only once.</param>
		/// <param name="unscaled">If the timer should ignore time scaling.</param>
		/// <returns>The started Coroutine.</returns>
		public static Coroutine Schedule(float delay, TimeCallback action, bool repeat = false, bool unscaled = false)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));
			if (delay < 0)
				throw new ArgumentOutOfRangeException(nameof(delay), "Cannot specify a negative delay for timers");
			var cr = new TimerCoroutine(delay, repeat, unscaled, action);
			CoroutineManager.AddCoroutine(cr);
			return cr;
		}

		/// <summary>
		/// Creates a timer, which executes the passed action after a certain amount of time has passed.
		/// </summary>
		/// <param name="delay">The amount of time to delay before execution.</param>
		/// <param name="action">The action to execute after the delay.</param>
		/// <param name="repeat">If the action should be repeated, <c>false</c> to execute only once.</param>
		/// <param name="unscaled">If the timer should ignore time scaling.</param>
		/// <returns>The started Coroutine.</returns>
		public static Coroutine Schedule(TimeSpan delay, TimeCallback action, bool repeat = false, bool unscaled = false) =>
			Schedule((float)delay.TotalSeconds, action, repeat, unscaled);

		/// <summary>
		/// Creates a timer, which executes the passed action after a certain number of frames have passed.
		/// </summary>
		/// <param name="frames">The number of frames to delay before execution.</param>
		/// <param name="action">The action to execute after the delay.</param>
		/// <param name="repeat">If the action should be repeated, <c>false</c> to execute only once.</param>
		/// <returns>The started Coroutine.</returns>
		public static Coroutine ScheduleFrames(uint frames, Action action, bool repeat = false)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));
			var cr = new FrameCoroutine(frames, repeat, (delay) => { action(); return true; });
			CoroutineManager.AddCoroutine(cr);
			return cr;
		}

		/// <summary>
		/// Creates a timer, which executes the passed action after a certain number of frames have passed.
		/// </summary>
		/// <param name="frames">The number of frames to delay before execution.</param>
		/// <param name="action">The action to execute after the delay.</param>
		/// <param name="repeat">If the action should be repeated, <c>false</c> to execute only once.</param>
		/// <returns>The started Coroutine.</returns>
		public static Coroutine ScheduleFrames(uint frames, FrameCallback action, bool repeat = false)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));
			var cr = new FrameCoroutine(frames, repeat, action);
			CoroutineManager.AddCoroutine(cr);
			return cr;
		}
		#endregion // Scheduling
	}

	// Class that internally controls ticking an IEnumerator instance
	internal class EnumeratorCoroutine : Coroutine
	{
		private readonly IEnumerator _ticker;
		private readonly bool _unscaled;
		public override bool UseUnscaledTime => _unscaled;

		public EnumeratorCoroutine(IEnumerator ticker, bool unscaled)
		{
			_ticker = ticker;
			_unscaled = unscaled;
		}

		protected internal override object Tick() => _ticker.MoveNext() ? _ticker.Current : END;
	}

	// Class that internally implements a time-based timer
	internal class TimerCoroutine : EnumeratorCoroutine
	{
		public readonly TimeCallback Action;
		public readonly float Delay;
		public readonly bool Repeat;

		public TimerCoroutine(float delay, bool repeat, bool unscaled, TimeCallback action) :
			base(timer_func(delay, repeat, action), unscaled)
		{
			Action = action;
			Delay = delay;
			Repeat = repeat;
		}

		private static IEnumerator timer_func(float delay, bool repeat, TimeCallback action)
		{
			do
			{
				yield return WaitTime(delay);
			}
			while (action(delay) && repeat);
		}
	}

	// Class that internally implements a frame-based timer
	internal class FrameCoroutine : EnumeratorCoroutine
	{
		public readonly FrameCallback Action;
		public readonly uint Delay;
		public readonly bool Repeat;

		public FrameCoroutine(uint delay, bool repeat, FrameCallback action) :
			base(timer_func(delay, repeat, action), true)
		{
			Action = action;
			Delay = delay;
			Repeat = repeat;
		}

		private static IEnumerator timer_func(uint delay, bool repeat, FrameCallback action)
		{
			do
			{
				yield return WaitFrames(delay);
			}
			while (action(delay) && repeat);
		}
	}
}
