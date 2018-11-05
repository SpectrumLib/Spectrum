using System;
using System.Collections.Generic;

namespace Spectrum.Input
{
	/// <summary>
	/// Represents and manages user input from the keyboard. Keyboard input can be both event and polling based.
	/// </summary>
	public static class Keyboard
	{
		/// <summary>
		/// The default quantity for the <see cref="HoldTime"/> field.
		/// </summary>
		public const float DEFAULT_HOLD_TIME = 0.5f;
		/// <summary>
		/// The default quantity for the <see cref="TapTime"/> field.
		/// </summary>
		public const float DEFAULT_TAP_TIME = 0.2f;

		#region Fields
		// Track states and times
		private static readonly bool[] s_lastKeys = new bool[KeyUtils.MAX_KEY_INDEX + 1];
		private static readonly bool[] s_currKeys = new bool[KeyUtils.MAX_KEY_INDEX + 1];
		private static readonly float[] s_lastPress = new float[KeyUtils.MAX_KEY_INDEX + 1];
		private static readonly float[] s_lastRelease = new float[KeyUtils.MAX_KEY_INDEX + 1];
		private static readonly float[] s_lastTap = new float[KeyUtils.MAX_KEY_INDEX + 1];
		private static readonly List<Keys> s_pressed = new List<Keys>(32);

		// Holds the glfw events until all are registered
		private static readonly List<KeyEventData> s_events = new List<KeyEventData>(32);

		#region Input Settings
		/// <summary>
		/// The amount of time (in seconds) that a key must be held down for to start generating hold events.
		/// </summary>
		public static float HoldTime = DEFAULT_HOLD_TIME;

		/// <summary>
		/// The maximum amount of time (in seconds) between a press and release event to be considered a tap event.
		/// </summary>
		public static float TapTime = DEFAULT_TAP_TIME;

		/// <summary>
		/// Gets or sets if <see cref="KeyEventType.Tapped"/> events are generated. Defaults to true.
		/// </summary>
		public static bool TapEventsEnabled = true;
		#endregion // Input Settings

		/// <summary>
		/// The mask of modifier keys (shift, control, alt) currently pressed down.
		/// </summary>
		public static ModKeyMask ModifierMask { get; private set; } = new ModKeyMask(0x00);

		#region Events
		/// <summary>
		/// Event that is raised every time a key is pressed.
		/// </summary>
		public static event KeyEvent KeyPressed;
		/// <summary>
		/// Event that is raised every time a key is released.
		/// </summary>
		public static event KeyEvent KeyReleased;
		/// <summary>
		/// Events that is raised every time a key is released within a certain time after being pressed.
		/// </summary>
		public static event KeyEvent KeyTapped;
		/// <summary>
		/// Event that is raised while a key is being held down.
		/// </summary>
		public static event KeyEvent KeyHeld;
		/// <summary>
		/// Event used to subscribe or unsubscribe from all input events.
		/// </summary>
		public static event KeyEvent AllKeyEvents
		{
			add { KeyPressed += value; KeyReleased += value; KeyTapped += value; KeyHeld += value; }
			remove { KeyPressed -= value; KeyReleased -= value; KeyTapped -= value; KeyHeld -= value; }
		}
		#endregion // Events
		#endregion // Fields

		internal static void NewFrame()
		{
			Array.Copy(s_currKeys, s_lastKeys, s_currKeys.Length);
		}

		internal static void FireEvents()
		{
			// Create the hold events
			foreach (var keys in s_pressed)
			{
				float diff = Time.Elapsed - s_lastPress[(int)keys];
				if (diff >= HoldTime)
					s_events.Add(new KeyEventData(KeyEventType.Held, keys, ModifierMask, diff));
			}

			// Fire off the events
			foreach (var evt in s_events)
			{
				switch (evt.Type)
				{
					case KeyEventType.Pressed: KeyPressed?.Invoke(in evt); break;
					case KeyEventType.Released: KeyReleased?.Invoke(in evt); break;
					case KeyEventType.Tapped: KeyTapped?.Invoke(in evt); break;
					case KeyEventType.Held: KeyHeld?.Invoke(in evt); break;
				}
			}

			s_events.Clear();
		}

		#region Polling
		/// <summary>
		/// Gets if the key is currently pressed.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public static bool IsKeyDown(Keys key) => s_currKeys[(int)key];
		/// <summary>
		/// Gets if the key is currently released.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public static bool IsKeyUp(Keys key) => !s_currKeys[(int)key];
		/// <summary>
		/// Gets if the key was pressed in the previous frame.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public static bool IsKeyPreviouslyDown(Keys key) => s_lastKeys[(int)key];
		/// <summary>
		/// Gets if the key was released in the previous frame.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public static bool IsKeyPreviouslyUp(Keys key) => !s_lastKeys[(int)key];
		/// <summary>
		/// Gets if the key was just pressed in this frame.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public static bool IsKeyPressed(Keys key) => s_currKeys[(int)key] && !s_lastKeys[(int)key];
		/// <summary>
		/// Gets if the key was just released in this frame.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public static bool IsKeyReleased(Keys key) => !s_currKeys[(int)key] && s_lastKeys[(int)key];
		/// <summary>
		/// Gets if the key is currently generating hold events.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public static bool IsKeyHeld(Keys key) => s_currKeys[(int)key] && ((Time.Elapsed - s_lastPress[(int)key]) >= HoldTime);

		/// <summary>
		/// Gets an array of the keys that are currently pressed down.
		/// </summary>
		public static Keys[] GetCurrentKeys() => s_pressed.ToArray();
		/// <summary>
		/// Enumerator for all of the keys that are currently pressed down.
		/// </summary>
		public static IEnumerator<Keys> EnumerateCurrentKeys()
		{
			foreach (var key in s_pressed)
				yield return key;
		}

		/// <summary>
		/// Gets the value of <see cref="Time.Elapsed"/> when the key was last pressed.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public static float GetLastPressTime(Keys key) => s_lastPress[(int)key];
		/// <summary>
		/// Gets the value of <see cref="Time.Elapsed"/> when the key was last released.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public static float GetLastReleaseTime(Keys key) => s_lastRelease[(int)key];
		/// <summary>
		/// Gets the value of <see cref="Time.Elapsed"/> when the key was last tapped.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public static float GetLastTapTime(Keys key) => s_lastTap[(int)key];
		#endregion // Polling

		#region GLFW Interop
		internal static void KeyCallback(IntPtr window, int key, int scancode, int action, int mods)
		{
			if (action == Glfw.REPEAT) return; // We generate our own repeat events

			Keys keys = KeyUtils.Translate(key);
			if (keys == Keys.Unknown) return; // Dont generate events for unsupported keys

			int index = (int)keys;
			if (action == Glfw.PRESS)
			{
				s_currKeys[index] = true;
				s_lastPress[index] = Time.Elapsed;
				s_pressed.Add(keys);
				s_events.Add(new KeyEventData(KeyEventType.Pressed, keys, ModifierMask, Time.Elapsed - s_lastRelease[index]));
			}
			else // Glfw.RELEASE
			{
				s_currKeys[index] = false;
				s_lastRelease[index] = Time.Elapsed;
				s_pressed.Remove(keys);
				float diff = Time.Elapsed - s_lastPress[index];
				s_events.Add(new KeyEventData(KeyEventType.Released, keys, ModifierMask, diff));

				if (TapEventsEnabled && diff <= TapTime)
				{
					s_events.Add(new KeyEventData(KeyEventType.Tapped, keys, ModifierMask, Time.Elapsed - s_lastTap[index]));
					s_lastTap[index] = Time.Elapsed;
				}
			}
		}
		#endregion // GLFW Interop

		static Keyboard()
		{
			for (int i = 0; i < KeyUtils.MAX_KEY_INDEX; ++i)
			{
				s_lastKeys[i] = s_currKeys[i] = false;
				s_lastPress[i] = s_lastRelease[i] = s_lastTap[i] = 0;
			}
		}
	}
}
