/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;

namespace Spectrum.Input
{
	/// <summary>
	/// Represents and manages user input from the mouse. Mouse input can be both event and polling based.
	/// </summary>
	public static class Mouse
	{
		#region Fields
		// Event tracking
		private static MouseButtonMask _LastMouse = MouseButtonMask.None;
		private static MouseButtonMask _CurrMouse = MouseButtonMask.None;
		private static readonly float[] _LastPress = new float[MouseButtonUtils.MAX_BUTTON_INDEX + 1];
		private static readonly float[] _LastRelease = new float[MouseButtonUtils.MAX_BUTTON_INDEX + 1];
		private static readonly Point[] _LastPressPos = new Point[MouseButtonUtils.MAX_BUTTON_INDEX + 1];
		private static readonly Point[] _LastReleasePos = new Point[MouseButtonUtils.MAX_BUTTON_INDEX + 1];

		// Click event tracking
		private static readonly float[] _LastClick = new float[MouseButtonUtils.MAX_BUTTON_INDEX + 1]; // Time of last click
		private static readonly Point[] _LastClickPos = new Point[MouseButtonUtils.MAX_BUTTON_INDEX + 1]; // Pos of last click
		private static readonly bool[] _LastClickFrame = new bool[MouseButtonUtils.MAX_BUTTON_INDEX + 1]; // Click event in last frame
		private static readonly bool[] _NextDouble = new bool[MouseButtonUtils.MAX_BUTTON_INDEX + 1]; // Next click is double click

		// Position tracking
		private static Point _LastPos = Point.Zero;
		private static Point _CurrPos = Point.Zero;
		private static Point _DeltaWheel = Point.Zero;

		// Enter/Leave tracking
		private static bool _EnterEvent = false;
		private static bool _EnterData = false;

		// Event cache
		private static readonly List<MouseButtonEventData> _Events = new List<MouseButtonEventData>(16);

		#region Input Settings
		/// <summary>
		/// A mask of the buttons that can generate double click events. Defaults to the primary buttons.
		/// Max amount of time between a press and release event to generate a click event. Defaults to half a second.
		/// </summary>
		public static float ClickTime = 0.5f;

		/// <summary>
		/// Max amount of time between two click events to generated a double click event. Defaults to half a second.
		/// </summary>
		public static float DoubleClickTime = 0.5f;

		/// <summary>
		/// Max distance (in pixels) between a two events to generate a click or double click event. Set to zero to
		/// disable this check. Defaults to 25 pixels.
		/// </summary>
		public static uint ClickDistance = 25;

		private static CursorMode _CursorMode = CursorMode.Normal;
		/// <summary>
		/// The input mode used by the mouse cursor, see the CursorMode values for more information. Fires the
		/// <see cref="CursorModeChanged"/> event when changed.
		/// </summary>
		public static CursorMode CursorMode
		{
			get => _CursorMode;
			set
			{
				if (_CursorMode != value)
				{
					var old = _CursorMode;
					_CursorMode = value;
					Core.Instance.Window.Glfw.SetInputMode(Core.Instance.Window.Handle, Glfw3.CURSOR,
						(value == CursorMode.Normal) ? Glfw3.CURSOR_NORMAL :
						(value == CursorMode.Hidden) ? Glfw3.CURSOR_HIDDEN : Glfw3.CURSOR_DISABLED);
					CursorModeChanged?.Invoke(old, value);
				}
			}
		}
		#endregion // Input Settings

		#region Events
		/// <summary>
		/// Event that is raised whenever a mouse button is pressed.
		/// </summary>
		public static event MouseButtonEvent ButtonPressed;
		/// <summary>
		/// Event that is raised whenever a mouse button is released.
		/// </summary>
		public static event MouseButtonEvent ButtonReleased;
		/// <summary>
		/// Event that is raised whenever a mouse button is clicked or double clicked.
		/// </summary>
		public static event MouseButtonEvent ButtonClicked;
		/// <summary>
		/// Event that can be used to subscribe or unsubscribe from all mouse button events.
		/// </summary>
		public static event MouseButtonEvent AllButtonEvents
		{
			add { ButtonPressed += value; ButtonReleased += value; ButtonClicked += value; }
			remove { ButtonPressed -= value; ButtonReleased -= value; ButtonClicked -= value; }
		}
		/// <summary>
		/// Event that is raised when the mouse is moved.
		/// </summary>
		public static event MouseMoveEvent Moved;
		/// <summary>
		/// Event that is raised when the mouse wheel is moved.
		/// </summary>
		public static event MouseWheelEvent WheelChanged;

		/// <summary>
		/// Event that is raised when the <see cref="CursorMode"/> of the mouse is changed.
		/// </summary>
		public static event CursorModeChangedEvent CursorModeChanged;

		/// <summary>
		/// Event that is raised when the mouse cursor either enters or leaves the window.
		/// </summary>
		public static event CursorEnteredEvent CursorEntered;
		#endregion // Events
		#endregion // Fields

		internal static void NewFrame()
		{
			_LastMouse = _CurrMouse;
			_LastPos = _CurrPos;
			_DeltaWheel = Point.Zero;
			Core.Instance.Window.Glfw.GetCursorPos(Core.Instance.Window.Handle, out double xpos, out double ypos);
			_CurrPos = new Point((int)xpos, (int)ypos);
			for (int i = 0; i < MouseButtonUtils.MAX_BUTTON_INDEX; ++i)
				_LastClickFrame[i] = false;
			_EnterEvent = false;
		}

		internal static void FireEvents()
		{
			// Fire the move and wheel events
			if (_CurrPos != _LastPos)
				Moved?.Invoke(new MouseMoveEventData(_CurrPos, _LastPos, _CurrMouse));
			if (_DeltaWheel != Point.Zero)
				WheelChanged?.Invoke(new MouseWheelEventData(_DeltaWheel));

			// Fire the enter/leave event
			if (_EnterEvent)
				CursorEntered?.Invoke(_EnterData);

			// Fire all of the button events
			foreach (var evt in _Events)
			{
				switch (evt.Type)
				{
					case ButtonEventType.Pressed: ButtonPressed?.Invoke(evt); break;
					case ButtonEventType.Released: ButtonReleased?.Invoke(evt); break;
					case ButtonEventType.Clicked:
					case ButtonEventType.DoubleClicked: ButtonClicked?.Invoke(evt); break;
				}
			}

			_Events.Clear();
		}

		#region Polling
		/// <summary>
		/// Gets if the mouse button is currently pressed.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public static bool IsButtonDown(MouseButton mb) => _CurrMouse.GetButton(mb);
		/// <summary>
		/// Gets if the mouse button is currently released.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public static bool IsButtonUp(MouseButton mb) => !_CurrMouse.GetButton(mb);
		/// <summary>
		/// Gets if the mouse button was pressed in the previous frame.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public static bool IsButtonPreviouslyDown(MouseButton mb) => _LastMouse.GetButton(mb);
		/// <summary>
		/// Gets if the mouse button was released in the previous frame.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public static bool IsButtonPreviouslyUp(MouseButton mb) => !_LastMouse.GetButton(mb);
		/// <summary>
		/// Gets if the button was just pressed in this frame.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public static bool IsButtonPressed(MouseButton mb) => _CurrMouse.GetButton(mb) && !_LastMouse.GetButton(mb);
		/// <summary>
		/// Gets if the button was just released in this frame.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public static bool IsButtonReleased(MouseButton mb) => !_CurrMouse.GetButton(mb) && _LastMouse.GetButton(mb);
		/// <summary>
		/// Gets if the button was clicked or double clicked in this frame.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public static bool IsButtonClicked(MouseButton mb) => _LastClickFrame[(int)mb];
		/// <summary>
		/// Gets if the button was double clicked in this frame. Returns false for clicks that are not double.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public static bool IsButtonDoubleClicked(MouseButton mb) => _LastClickFrame[(int)mb] && !_NextDouble[(int)mb];

		/// <summary>
		/// Gets a mask of all of the mouse buttons that are currently pressed down.
		/// </summary>
		public static MouseButtonMask GetCurrentButtons() => _CurrMouse;
		/// <summary>
		/// Enumerator for all of the keys that are currently pressed down.
		/// </summary>
		public static IEnumerator<MouseButton> EnumerateCurrentButtons()
		{
			if (_CurrMouse.Left) yield return MouseButton.Left;
			if (_CurrMouse.Right) yield return MouseButton.Right;
			if (_CurrMouse.Middle) yield return MouseButton.Middle;
			if (_CurrMouse.X1) yield return MouseButton.X1;
			if (_CurrMouse.X2) yield return MouseButton.X2;
		}

		/// <summary>
		/// Gets the value of <see cref="Time.Elapsed"/> when the mouse button was last pressed.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public static float GetLastPressTime(MouseButton mb) => _LastPress[(int)mb];
		/// <summary>
		/// Gets the value of <see cref="Time.Elapsed"/> when the mouse button was last released.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public static float GetLastReleaseTime(MouseButton mb) => _LastRelease[(int)mb];
		/// <summary>
		/// Gets the value of <see cref="Time.Elapsed"/> when the mouse button was last clicked.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public static float GetLastClickTime(MouseButton mb) => _LastClick[(int)mb];
		/// <summary>
		/// Gets the screen position where the mouse cursor was when the button was last pressed.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public static Point GetLastPressPos(MouseButton mb) => _LastPressPos[(int)mb];
		/// <summary>
		/// Gets the screen position where the mouse cursor was when the button was last released.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public static Point GetLastReleasePos(MouseButton mb) => _LastReleasePos[(int)mb];
		/// <summary>
		/// Gets the screen position where the mouse cursor was when the button was last clicked.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public static Point GetLastClickPos(MouseButton mb) => _LastClickPos[(int)mb];

		/// <summary>
		/// Gets the current position of the mouse, in pixels, relative to the top-left corner of the window client area.
		/// </summary>
		public static Point GetPosition() => _CurrPos;
		/// <summary>
		/// Gets if the position of the mouse in the last frame.
		/// </summary>
		public static Point GetLastPosition() => _LastPos;
		/// <summary>
		/// Gets the change in mouse position between this frame and the last.
		/// </summary>
		public static Point GetDelta() => _CurrPos - _LastPos;
		/// <summary>
		/// Gets if the mouse moved between this frame and the last.
		/// </summary>
		public static bool GetMoved() => (_CurrPos - _LastPos) != Point.Zero;

		/// <summary>
		/// Gets the change in the mouse wheel between this frame and the last. The X-component of the return value is
		/// the change along the primary wheel axis. The Y-component will only be non-zero on mice that support a
		/// second wheel axis.
		/// </summary>
		public static Point GetWheelDelta() => _DeltaWheel;

		/// <summary>
		/// Gets if the cursor entered the window area in this frame.
		/// </summary>
		public static bool GetCursorEntered() => _EnterEvent && _EnterData;
		/// <summary>
		/// Gets if the cursor exited the window area in this frame.
		/// </summary>
		public static bool GetCursorExited() => _EnterEvent && !_EnterData;
		/// <summary>
		/// Gets if the mouse cursor is currently in the window area. Note that this value may be inaccurate if the
		/// window does not have focus, or if the application has just started up.
		/// </summary>
		public static bool IsInWindow() => _EnterData;
		#endregion // Polling

		#region GLFW Interop
		internal static void ButtonCallback(IntPtr window, int button, int action, int mods)
		{
			MouseButton mb = MouseButtonUtils.Translate(button);

			int index = (int)mb;
			if (action == Glfw3.PRESS)
			{
				_CurrMouse.SetButton(mb);
				_LastPress[index] = Time.Elapsed;
				_LastPressPos[index] = _CurrPos;
				_Events.Add(new MouseButtonEventData(ButtonEventType.Pressed, mb, Time.Elapsed - _LastRelease[index]));
			}
			else // Glfw.RELEASE
			{
				_CurrMouse.ClearButton(mb);
				_LastRelease[index] = Time.Elapsed;
				Point lastClick = _LastReleasePos[index];
				_LastReleasePos[index] = _CurrPos;
				float diff = Time.Elapsed - _LastPress[index];
				_Events.Add(new MouseButtonEventData(ButtonEventType.Released, mb, diff));

				if ((diff < ClickTime) && (ClickDistance == 0 || Point.Distance(_CurrPos, _LastPressPos[index]) <= ClickDistance))
				{
					_Events.Add(new MouseButtonEventData(ButtonEventType.Clicked, mb, diff));

					if (_NextDouble[index])
					{
						if (((Time.Elapsed - _LastClick[index]) < DoubleClickTime) && (ClickDistance == 0 || Point.Distance(_CurrPos, lastClick) <= ClickDistance))
						{
							_Events.Add(new MouseButtonEventData(ButtonEventType.DoubleClicked, mb, Time.Elapsed - _LastClick[index]));
							_NextDouble[index] = false;
						}
					}
					else
						_NextDouble[index] = true;

					_LastClickFrame[index] = true;
					_LastClick[index] = Time.Elapsed;
					_LastClickPos[index] = _CurrPos;
				}
				else
					_NextDouble[index] = false; // Failed click resets the double-click tracking
			}
		}

		internal static void ScrollCallback(IntPtr window, double xoffset, double yoffset)
		{
			_DeltaWheel = new Point((int)xoffset, (int)yoffset);
		}

		internal static void CursorEnterCallback(IntPtr window, int entered)
		{
			_EnterEvent = true;
			_EnterData = (entered == Glfw3.TRUE);
		}
		#endregion // GLFW Interop

		static Mouse()
		{
			for (int i = 0; i < MouseButtonUtils.MAX_BUTTON_INDEX; ++i)
			{
				_LastPress[i] = _LastRelease[i] = 0;
				_LastPressPos[i] = _LastReleasePos[i] = _LastClickPos[i] = Point.Zero;
				_LastClick[i] = 0;
				_LastClickFrame[i] = _NextDouble[i] = false;
			}
		}
	}

	/// <summary>
	/// Values representing the different modes that the mouse cursor can use.
	/// </summary>
	public enum CursorMode : byte
	{
		/// <summary>
		/// The standard visible cursor that is not locked to the screen.
		/// </summary>
		Normal,
		/// <summary>
		/// Like the standard mode, but the cursor is hidden while it is in the client area of the window.
		/// </summary>
		Hidden,
		/// <summary>
		/// The cursor is both hidden and locked to the center of the window. The window will automatically calculate
		/// and provide virtual offsets while keeping the cursor locked in position. Use this mode to implment first
		/// person cameras controlled by mouse movement.
		/// </summary>
		Locked
	}
}
