using System;

namespace Spectrum
{
	/// <summary>
	/// Contains information relating to the application window changing position.
	/// </summary>
	public struct WindowPositionEventData
	{
		/// <summary>
		/// A quick reference to the application window.
		/// </summary>
		public AppWindow Window => SpectrumApp.Instance.Window;
		/// <summary>
		/// The old position of the window.
		/// </summary>
		public readonly Point OldPos;
		/// <summary>
		/// The new position of the window.
		/// </summary>
		public readonly Point NewPos;

		internal WindowPositionEventData(Point o, Point n)
		{
			OldPos = o;
			NewPos = n;
		}
	}

	/// <summary>
	/// Contains information relating to the application window changing size.
	/// </summary>
	public struct WindowSizeEventData
	{
		/// <summary>
		/// A quick reference to the application window.
		/// </summary>
		public AppWindow Window => SpectrumApp.Instance.Window;
		/// <summary>
		/// The old size of the window.
		/// </summary>
		public readonly Point OldSize;
		/// <summary>
		/// The new size of the window.
		/// </summary>
		public readonly Point NewSize;

		internal WindowSizeEventData(Point o, Point n)
		{
			OldSize = o;
			NewSize = n;
		}
	}

	/// <summary>
	/// Contains information relating to the application window changing fullscreen mode.
	/// </summary>
	public struct WindowStyleEventData
	{
		/// <summary>
		/// A quick reference to the application window.
		/// </summary>
		public AppWindow Window => SpectrumApp.Instance.Window;
		/// <summary>
		/// <c>true</c> if the window entered fullscreen, <c>false</c> if the window left fullscreen.
		/// </summary>
		public readonly bool Fullscreen;

		internal WindowStyleEventData(bool fs)
		{
			Fullscreen = fs;
		}
	}

	/// <summary>
	/// Callback for a window position change event.
	/// </summary>
	/// <param name="data">The event data.</param>
	public delegate void WindowPositionChangedEvent(WindowPositionEventData data);

	/// <summary>
	/// Callback for a window size change event.
	/// </summary>
	/// <param name="data">The event data.</param>
	public delegate void WindowSizeChangedEvent(WindowSizeEventData data);

	/// <summary>
	/// Callback for a window style change event.
	/// </summary>
	/// <param name="data">The event data.</param>
	public delegate void WindowStyleChangeEvent(WindowStyleEventData data);
}
