/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;

namespace Spectrum.Input
{
	/// <summary>
	/// Represents the different possible input events for keyboard keys.
	/// </summary>
	public enum KeyEventType
	{
		/// <summary>
		/// The key was pressed down in the last frame.
		/// </summary>
		Pressed,
		/// <summary>
		/// The key was released in the last frame.
		/// </summary>
		Released,
		/// <summary>
		/// The key has been held down long enough to start generating hold events.
		/// </summary>
		Held,
		/// <summary>
		/// The key has been pressed and released quickly enough to register as a tap.
		/// </summary>
		Tapped
	}

	/// <summary>
	/// Data describing an input event of a keyboard key.
	/// </summary>
	public struct KeyEventData
	{
		#region Fields
		/// <summary>
		/// The event type.
		/// </summary>
		public KeyEventType Type { get; internal set; }
		/// <summary>
		/// The key that generated the event.
		/// </summary>
		public Keys Key { get; internal set; }
		/// <summary>
		/// A mask of the modifier keys that were pressed for this event.
		/// </summary>
		public ModKeyMask Mods { get; internal set; }
		/// <summary>
		/// The time (in seconds) associated with the event. This field takes on different meanings based on the event type:
		/// <list type="bullet">
		///		<item>
		///			<see cref="KeyEventType.Pressed"/> - The time since the key was last released.
		///		</item>
		///		<item>
		///			<see cref="KeyEventType.Released"/> - The time since the key was last pressed.
		///		</item>
		///		<item>
		///			<see cref="KeyEventType.Held"/> - The amount of time the key has been held for.
		///		</item>
		///		<item>
		///			<see cref="KeyEventType.Tapped"/> - The time since the last tap event.
		///		</item>
		/// </list>
		/// </summary>
		public float EventTime { get; internal set; }
		/// <summary>
		/// The application time (in seconds) at which this event was generated.
		/// </summary>
		public float Timestamp { get; internal set; }
		#endregion Fields

		internal KeyEventData(KeyEventType type, Keys key, ModKeyMask mods, float et)
		{
			Type = type;
			Key = key;
			Mods = mods;
			EventTime = et;
			Timestamp = Time.Elapsed;
		}
	}

	/// <summary>
	/// Callback for a keyboard key event.
	/// </summary>
	/// <param name="data">The event data.</param>
	public delegate void KeyEvent(KeyEventData data);
}