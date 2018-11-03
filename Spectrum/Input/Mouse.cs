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
		/// A mask of the buttons that can generate drag events. Defaults to the primary buttons (left, middle, right).
		/// </summary>
		public static MouseButtonMask DragMask = MouseButtonMask.Primary;

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
				Console.WriteLine($"Mouse Move: {s_currPos - s_lastPos}"); // TEMP
			}
			if (s_deltaWheel != Point.Zero)
			{
				WheelChanged?.Invoke(new MouseWheelEventData(s_deltaWheel));
				Console.WriteLine($"Wheel Change: {s_deltaWheel}"); // TEMP
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

				// TEMP
				Console.WriteLine($"Mouse Event: {evt.Type} {evt.Button}");
			}

			s_events.Clear();
		}

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
