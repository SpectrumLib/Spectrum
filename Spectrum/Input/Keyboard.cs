/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
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
		private static readonly bool[] _LastKeys = new bool[KeyUtils.MAX_KEY_INDEX + 1];
		private static readonly bool[] _CurrKeys = new bool[KeyUtils.MAX_KEY_INDEX + 1];
		private static readonly float[] _LastPress = new float[KeyUtils.MAX_KEY_INDEX + 1];
		private static readonly float[] _LastRelease = new float[KeyUtils.MAX_KEY_INDEX + 1];
		private static readonly float[] _LastTap = new float[KeyUtils.MAX_KEY_INDEX + 1];
		private static readonly List<Keys> _Pressed = new List<Keys>(32);

		// Holds the glfw events until all are registered
		private static readonly List<(KeyEventType ty, Keys ky, float tm)> _Events = new List<(KeyEventType, Keys, float)>(32);

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
			Array.Copy(_CurrKeys, _LastKeys, _CurrKeys.Length);
		}

		internal static void FireEvents()
		{
			// Create the hold events
			foreach (var keys in _Pressed)
			{
				float diff = Time.Elapsed - _LastPress[(int)keys];
				if (diff >= HoldTime)
					_Events.Add((KeyEventType.Held, keys, diff));
			}

			// Fire off the events
			foreach (var evt in _Events)
			{
				switch (evt.ty)
				{
					case KeyEventType.Pressed: KeyPressed?.Invoke(new KeyEventData(evt.ty, evt.ky, ModifierMask, evt.tm)); break;
					case KeyEventType.Released: KeyReleased?.Invoke(new KeyEventData(evt.ty, evt.ky, ModifierMask, evt.tm)); break;
					case KeyEventType.Tapped: KeyTapped?.Invoke(new KeyEventData(evt.ty, evt.ky, ModifierMask, evt.tm)); break;
					case KeyEventType.Held: KeyHeld?.Invoke(new KeyEventData(evt.ty, evt.ky, ModifierMask, evt.tm)); break;
				}
			}

			_Events.Clear();
		}

		#region Polling
		/// <summary>
		/// Gets if the key is currently pressed.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public static bool IsKeyDown(Keys key) => _CurrKeys[(int)key];
		/// <summary>
		/// Gets if the key is currently released.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public static bool IsKeyUp(Keys key) => !_CurrKeys[(int)key];
		/// <summary>
		/// Gets if the key was pressed in the previous frame.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public static bool IsKeyPreviouslyDown(Keys key) => _LastKeys[(int)key];
		/// <summary>
		/// Gets if the key was released in the previous frame.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public static bool IsKeyPreviouslyUp(Keys key) => !_LastKeys[(int)key];
		/// <summary>
		/// Gets if the key was just pressed in this frame.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public static bool IsKeyPressed(Keys key) => _CurrKeys[(int)key] && !_LastKeys[(int)key];
		/// <summary>
		/// Gets if the key was just released in this frame.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public static bool IsKeyReleased(Keys key) => !_CurrKeys[(int)key] && _LastKeys[(int)key];
		/// <summary>
		/// Gets if the key is currently generating hold events.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public static bool IsKeyHeld(Keys key) => _CurrKeys[(int)key] && ((Time.Elapsed - _LastPress[(int)key]) >= HoldTime);

		/// <summary>
		/// Gets an array of the keys that are currently pressed down.
		/// </summary>
		public static Keys[] GetCurrentKeys() => _Pressed.ToArray();
		/// <summary>
		/// Enumerator for all of the keys that are currently pressed down.
		/// </summary>
		public static IEnumerator<Keys> EnumerateCurrentKeys()
		{
			foreach (var key in _Pressed)
				yield return key;
		}

		/// <summary>
		/// Gets the value of <see cref="Time.Elapsed"/> when the key was last pressed.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public static float GetLastPressTime(Keys key) => _LastPress[(int)key];
		/// <summary>
		/// Gets the value of <see cref="Time.Elapsed"/> when the key was last released.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public static float GetLastReleaseTime(Keys key) => _LastRelease[(int)key];
		/// <summary>
		/// Gets the value of <see cref="Time.Elapsed"/> when the key was last tapped.
		/// </summary>
		/// <param name="key">The key to check.</param>
		public static float GetLastTapTime(Keys key) => _LastTap[(int)key];
		#endregion // Polling

		#region GLFW Interop
		internal static void KeyCallback(IntPtr window, int key, int scancode, int action, int mods)
		{
			if (action == Glfw3.REPEAT) return; // We generate our own repeat events

			Keys keys = KeyUtils.Translate(key);
			if (keys == Keys.Unknown) return; // Dont generate events for unsupported keys

			int index = (int)keys;
			if (action == Glfw3.PRESS)
			{
				_CurrKeys[index] = true;
				_LastPress[index] = Time.Elapsed;
				_Pressed.Add(keys);
				if (keys.IsModKey())
					ModifierMask = ModifierMask.SetModifier(keys);
				
				_Events.Add((KeyEventType.Pressed, keys, Time.Elapsed - _LastRelease[index]));
			}
			else // Glfw.RELEASE
			{
				_CurrKeys[index] = false;
				_LastRelease[index] = Time.Elapsed;
				_Pressed.Remove(keys);
				float diff = Time.Elapsed - _LastPress[index];
				if (keys.IsModKey())
					ModifierMask = ModifierMask.ClearModifier(keys);

				_Events.Add((KeyEventType.Released, keys, diff));

				if (TapEventsEnabled && diff <= TapTime)
				{
					_Events.Add((KeyEventType.Tapped, keys, Time.Elapsed - _LastTap[index]));
					_LastTap[index] = Time.Elapsed;
				}
			}
		}
		#endregion // GLFW Interop

		static Keyboard()
		{
			for (int i = 0; i < KeyUtils.MAX_KEY_INDEX; ++i)
			{
				_LastKeys[i] = _CurrKeys[i] = false;
				_LastPress[i] = _LastRelease[i] = _LastTap[i] = 0;
			}
		}
	}
}
