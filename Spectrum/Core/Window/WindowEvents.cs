/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;

namespace Spectrum
{
	/// <summary>
	/// Data describing a window position change event.
	/// </summary>
	public struct WindowPositionEventData
	{
		/// <summary>
		/// A quick reference to the application window.
		/// </summary>
		public CoreWindow Window => Core.Instance.Window;
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
	/// Data describing a window size change event.
	/// </summary>
	public struct WindowSizeEventData
	{
		/// <summary>
		/// A quick reference to the application window.
		/// </summary>
		public CoreWindow Window => Core.Instance.Window;
		/// <summary>
		/// The old size of the window.
		/// </summary>
		public readonly Extent OldSize;
		/// <summary>
		/// The new size of the window.
		/// </summary>
		public readonly Extent NewSize;

		internal WindowSizeEventData(Extent o, Extent n)
		{
			OldSize = o;
			NewSize = n;
		}
	}

	/// <summary>
	/// Data describing a window fullscreen state change event.
	/// </summary>
	public struct WindowStyleEventData
	{
		/// <summary>
		/// A quick reference to the application window.
		/// </summary>
		public CoreWindow Window => Core.Instance.Window;
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
