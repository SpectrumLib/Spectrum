using System;

namespace Spectrum.Input
{
	/// <summary>
	/// Represents the different possible input events for mouse buttons.
	/// </summary>
	public enum ButtonEventType : byte
	{
		/// <summary>
		/// The button was pressed down in the last frame.
		/// </summary>
		Pressed,
		/// <summary>
		/// The button was released in the last frame.
		/// </summary>
		Released,
		/// <summary>
		/// The button was clicked, which is a press followed by a release within a certain amount of time and within
		/// a certain range of screen movement.
		/// </summary>
		Clicked,
		/// <summary>
		/// The button was double clicked, which are two Clicked events within a certain amount of time and within a
		/// certain range of screen movement.
		/// </summary>
		DoubleClicked
	}

	/// <summary>
	/// Data describing an input event of a mouse button.
	/// </summary>
	public struct MouseButtonEventData
	{
		#region Fields
		/// <summary>
		/// The type of event.
		/// </summary>
		public readonly ButtonEventType Type;
		/// <summary>
		/// The button that generated this event.
		/// </summary>
		public readonly MouseButton Button;
		/// <summary>
		/// The time (in seconds) associated with the event. This field takes on different meanings based on the event type:
		/// <list type="bullet">
		///		<item>
		///			<see cref="ButtonEventType.Pressed"/> - The time since the button was last released.
		///		</item>
		///		<item>
		///			<see cref="ButtonEventType.Released"/> - The time since the button was last pressed.
		///		</item>
		///		<item>
		///			<see cref="ButtonEventType.Clicked"/> - The time between the press and release of the event.
		///		</item>
		///		<item>
		///			<see cref="ButtonEventType.DoubleClicked"/> - The time between the component click events.
		///		</item>
		/// </list>
		/// </summary>
		public readonly float EventTime;
		/// <summary>
		/// The application time (in seconds) at which this event was generated.
		/// </summary>
		public readonly float TimeStamp;

		#region Helpers
		/// <summary>
		/// Gets if the event button is <see cref="MouseButton.Left"/>.
		/// </summary>
		public bool Left => (Button == MouseButton.Left);
		/// <summary>
		/// Gets if the event button is <see cref="MouseButton.Middle"/>.
		/// </summary>
		public bool Middle => (Button == MouseButton.Middle);
		/// <summary>
		/// Gets if the event button is <see cref="MouseButton.Right"/>.
		/// </summary>
		public bool Right => (Button == MouseButton.Right);
		/// <summary>
		/// Gets if the event button is <see cref="MouseButton.X1"/>.
		/// </summary>
		public bool X1 => (Button == MouseButton.X1);
		/// <summary>
		/// Gets if the event button is <see cref="MouseButton.X2"/>.
		/// </summary>
		public bool X2 => (Button == MouseButton.X2);

		/// <summary>
		/// Gets if the event is a <see cref="ButtonEventType.Pressed"/> event.
		/// </summary>
		public bool Press => (Type == ButtonEventType.Pressed);
		/// <summary>
		/// Gets if the event is a <see cref="ButtonEventType.Released"/> event.
		/// </summary>
		public bool Release => (Type == ButtonEventType.Released);
		/// <summary>
		/// Gets if the event is a <see cref="ButtonEventType.Clicked"/> event.
		/// </summary>
		public bool Click => (Type == ButtonEventType.Clicked);
		/// <summary>
		/// Gets if the event is a <see cref="ButtonEventType.DoubleClicked"/> event.
		/// </summary>
		public bool DoubleClick => (Type == ButtonEventType.DoubleClicked);
		#endregion // Helpers
		#endregion // Fields

		internal MouseButtonEventData(ButtonEventType type, MouseButton button, float time)
		{
			Type = type;
			Button = button;
			EventTime = time;
			TimeStamp = Time.Elapsed;
		}
	}

	/// <summary>
	/// Data describing an input event of the mouse moving.
	/// </summary>
	public struct MouseMoveEventData
	{
		#region Fields
		/// <summary>
		/// The current position of the mouse, when the event was fired.
		/// </summary>
		public readonly Point Current;
		/// <summary>
		/// The position of the mouse in the frame before the event was fired.
		/// </summary>
		public readonly Point Last;
		/// <summary>
		/// The change in position of this move event.
		/// </summary>
		public Point Delta => Current - Last;
		/// <summary>
		/// The mask of buttons that were down during the move event.
		/// </summary>
		public readonly MouseButtonMask Buttons;
		/// <summary>
		/// The mask of buttons that are generating drag events.
		/// </summary>
		public readonly MouseButtonMask DragButtons;
		/// <summary>
		/// The application time (in seconds) at which this event was generated.
		/// </summary>
		public readonly float TimeStamp;
		#endregion // Fields

		internal MouseMoveEventData(in Point curr, in Point last, in MouseButtonMask buttons)
		{
			Current = curr;
			Last = last;
			Buttons = buttons;
			DragButtons = buttons & Mouse.DragMask;
			TimeStamp = Time.Elapsed;
		}
	}

	/// <summary>
	/// Data describing an input event of the mouse wheel.
	/// </summary>
	public struct MouseWheelEventData
	{
		#region Fields
		/// <summary>
		/// The change in the mouse wheel value in both dimensions.
		/// </summary>
		public readonly Point Delta;
		/// <summary>
		/// The x-value delta of the mouse wheel. This is the standard up/down scroll direction for mice.
		/// </summary>
		public int X => Delta.X;
		/// <summary>
		/// The y-value delta of the mouse wheel. This is horizonal change only supported by some mice.
		/// </summary>
		public int Y => Delta.Y;
		/// <summary>
		/// The application time (in seconds) at which this event was generated.
		/// </summary>
		public readonly float TimeStamp;
		#endregion // Fields

		internal MouseWheelEventData(in Point delta)
		{
			Delta = delta;
			TimeStamp = Time.Elapsed;
		}
	}

	/// <summary>
	/// Callback for a mouse button event.
	/// </summary>
	/// <param name="data">The data describing the event.</param>
	public delegate void MouseButtonEvent(in MouseButtonEventData data);

	/// <summary>
	/// Callback for a mouse move event.
	/// </summary>
	/// <param name="data">The data describing the event.</param>
	public delegate void MouseMoveEvent(in MouseMoveEventData data);

	/// <summary>
	/// Callback for a mouse wheel event.
	/// </summary>
	/// <param name="data">The data describing the event.</param>
	public delegate void MouseWheelEvent(in MouseWheelEventData data);
}
