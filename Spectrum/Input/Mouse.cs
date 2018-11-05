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
		private static MouseButtonMask s_lastMouse = MouseButtonMask.None;
		private static MouseButtonMask s_currMouse = MouseButtonMask.None;
		private static readonly float[] s_lastPress = new float[MouseButtonUtils.MAX_BUTTON_INDEX + 1];
		private static readonly float[] s_lastRelease = new float[MouseButtonUtils.MAX_BUTTON_INDEX + 1];
		private static readonly Point[] s_lastPressPos = new Point[MouseButtonUtils.MAX_BUTTON_INDEX + 1];
		private static readonly Point[] s_lastReleasePos = new Point[MouseButtonUtils.MAX_BUTTON_INDEX + 1];

		// Click event tracking
		private static readonly float[] s_lastClick = new float[MouseButtonUtils.MAX_BUTTON_INDEX + 1]; // Time of last click
		private static readonly bool[] s_lastClickFrame = new bool[MouseButtonUtils.MAX_BUTTON_INDEX + 1]; // Click event in last frame
		private static readonly bool[] s_nextDouble = new bool[MouseButtonUtils.MAX_BUTTON_INDEX + 1]; // Next click is double click

		// Position tracking
		private static Point s_lastPos = Point.Zero;
		private static Point s_currPos = Point.Zero;
		private static Point s_deltaWheel = Point.Zero;

		// Event cache
		private static readonly List<MouseButtonEventData> s_events = new List<MouseButtonEventData>(16);

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
		#endregion // Events
		#endregion // Fields

		internal static void NewFrame()
		{
			s_lastMouse = s_currMouse;
			s_lastPos = s_currPos;
			s_deltaWheel = Point.Zero;
			Glfw.GetCursorPos(SpectrumApp.Instance.Window.Handle, out double xpos, out double ypos);
			s_currPos = new Point((int)xpos, (int)ypos);
			for (int i = 0; i < MouseButtonUtils.MAX_BUTTON_INDEX; ++i)
				s_lastClickFrame[i] = false;
		}

		internal static void FireEvents()
		{
			// Fire the move and wheel events
			if (s_currPos != s_lastPos)
			{
				Moved?.Invoke(new MouseMoveEventData(s_currPos, s_lastPos, s_currMouse));
			}
			if (s_deltaWheel != Point.Zero)
			{
				WheelChanged?.Invoke(new MouseWheelEventData(s_deltaWheel));
			}

			// Fire all of the button events
			foreach (var evt in s_events)
			{
				switch (evt.Type)
				{
					case ButtonEventType.Pressed: ButtonPressed?.Invoke(in evt); break;
					case ButtonEventType.Released: ButtonReleased?.Invoke(in evt); break;
					case ButtonEventType.Clicked:
					case ButtonEventType.DoubleClicked: ButtonClicked?.Invoke(in evt); break;
				}
			}

			s_events.Clear();
		}

		#region Polling
		/// <summary>
		/// Gets if the mouse button is currently pressed.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public static bool IsButtonDown(MouseButton mb) => s_currMouse.GetButton(mb);
		/// <summary>
		/// Gets if the mouse button is currently released.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public static bool IsButtonUp(MouseButton mb) => !s_currMouse.GetButton(mb);
		/// <summary>
		/// Gets if the mouse button was pressed in the previous frame.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public static bool IsButtonPreviouslyDown(MouseButton mb) => s_lastMouse.GetButton(mb);
		/// <summary>
		/// Gets if the mouse button was released in the previous frame.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public static bool IsButtonPreviouslyUp(MouseButton mb) => !s_lastMouse.GetButton(mb);
		/// <summary>
		/// Gets if the button was just pressed in this frame.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public static bool IsButtonPressed(MouseButton mb) => s_currMouse.GetButton(mb) && !s_lastMouse.GetButton(mb);
		/// <summary>
		/// Gets if the button was just released in this frame.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public static bool IsButtonReleased(MouseButton mb) => !s_currMouse.GetButton(mb) && s_lastMouse.GetButton(mb);
		/// <summary>
		/// Gets if the button was clicked or double clicked in this frame.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public static bool IsButtonClicked(MouseButton mb) => s_lastClickFrame[(int)mb];
		/// <summary>
		/// Gets if the button was double clicked in this frame. Returns false for clicks that are not double.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public static bool IsButtonDoubleClicked(MouseButton mb) => s_lastClickFrame[(int)mb] && !s_nextDouble[(int)mb];
		
		/// <summary>
		/// Gets a mask of all of the mouse buttons that are currently pressed down.
		/// </summary>
		public static MouseButtonMask GetCurrentButtons() => s_currMouse;
		/// <summary>
		/// Enumerator for all of the keys that are currently pressed down.
		/// </summary>
		public static IEnumerator<MouseButton> EnumerateCurrentButtons()
		{
			if (s_currMouse.Left) yield return MouseButton.Left;
			if (s_currMouse.Right) yield return MouseButton.Right;
			if (s_currMouse.Middle) yield return MouseButton.Middle;
			if (s_currMouse.X1) yield return MouseButton.X1;
			if (s_currMouse.X2) yield return MouseButton.X2;
		}

		/// <summary>
		/// Gets the value of <see cref="Time.Elapsed"/> when the mouse button was last pressed.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public static float GetLastPressTime(MouseButton mb) => s_lastPress[(int)mb];
		/// <summary>
		/// Gets the value of <see cref="Time.Elapsed"/> when the mouse button was last released.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public static float GetLastReleaseTime(MouseButton mb) => s_lastRelease[(int)mb];
		/// <summary>
		/// Gets the value of <see cref="Time.Elapsed"/> when the mouse button was last clicked.
		/// </summary>
		/// <param name="mb">The mouse button to check.</param>
		public static float GetLastClickTime(MouseButton mb) => s_lastClick[(int)mb];

		/// <summary>
		/// Gets the current position of the mouse, in pixels, relative to the top-left corner of the window client area.
		/// </summary>
		public static Point GetPosition() => s_currPos;
		/// <summary>
		/// Gets if the position of the mouse in the last frame.
		/// </summary>
		public static Point GetLastPosition() => s_lastPos;
		/// <summary>
		/// Gets the change in mouse position between this frame and the last.
		/// </summary>
		public static Point GetDelta() => s_currPos - s_lastPos;
		/// <summary>
		/// Gets if the mouse moved between this frame and the last.
		/// </summary>
		public static bool GetMoved() => (s_currPos - s_lastPos) != Point.Zero;

		/// <summary>
		/// Gets the change in the mouse wheel between this frame and the last. The X-component of the return value is
		/// the change along the primary wheel axis. The Y-component will only be non-zero on mice that support a
		/// second wheel axis.
		/// </summary>
		public static Point GetWheelDelta() => s_deltaWheel;
		#endregion // Polling

		#region GLFW Interop
		internal static void ButtonCallback(IntPtr window, int button, int action, int mods)
		{
			if (button > Glfw.MOUSE_BUTTON_5) return; // Do not yet support more than 2 extra buttons

			MouseButton mb = MouseButtonUtils.Translate(button);

			int index = (int)mb;
			if (action == Glfw.PRESS)
			{
				s_currMouse.SetButton(mb);
				s_lastPress[index] = Time.Elapsed;
				s_lastPressPos[index] = s_currPos;
				s_events.Add(new MouseButtonEventData(ButtonEventType.Pressed, mb, Time.Elapsed - s_lastRelease[index]));
			}
			else // Glfw.RELEASE
			{
				s_currMouse.ClearButton(mb);
				s_lastRelease[index] = Time.Elapsed;
				Point lastClick = s_lastReleasePos[index];
				s_lastReleasePos[index] = s_currPos;
				float diff = Time.Elapsed - s_lastPress[index];
				s_events.Add(new MouseButtonEventData(ButtonEventType.Released, mb, diff));

				if ((diff < ClickTime) && (ClickDistance == 0 || Point.DistanceTo(s_currPos, s_lastPressPos[index]) <= ClickDistance))
				{
					s_events.Add(new MouseButtonEventData(ButtonEventType.Clicked, mb, diff));

					if (s_nextDouble[index])
					{
						if (((Time.Elapsed - s_lastClick[index]) < DoubleClickTime) && (ClickDistance == 0 || Point.DistanceTo(s_currPos, lastClick) <= ClickDistance))
						{
							s_events.Add(new MouseButtonEventData(ButtonEventType.DoubleClicked, mb, Time.Elapsed - s_lastClick[index]));
							s_nextDouble[index] = false;
						}
					}
					else
						s_nextDouble[index] = true;

					s_lastClickFrame[index] = true;
					s_lastClick[index] = Time.Elapsed;
				}
				else
					s_nextDouble[index] = false; // Failed click resets the double-click tracking
			}
		}

		internal static void ScrollCallback(IntPtr window, double xoffset, double yoffset)
		{
			s_deltaWheel = new Point((int)xoffset, (int)yoffset);
		}
		#endregion // GLFW Interop

		static Mouse()
		{
			for (int i = 0; i < MouseButtonUtils.MAX_BUTTON_INDEX; ++i)
			{
				s_lastPress[i] = s_lastRelease[i] = 0;
				s_lastPressPos[i] = s_lastReleasePos[i] = Point.Zero;
				s_lastClick[i] = 0;
				s_lastClickFrame[i] = s_nextDouble[i] = false;
			}
		}
	}
}
