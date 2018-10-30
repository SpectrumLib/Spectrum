using System;

namespace Spectrum.Input
{
	/// <summary>
	/// Represents and manages user input from the mouse. Mouse input can be both event and polling based.
	/// </summary>
	public static class Mouse
	{
		#region Fields
		/// <summary>
		/// A mask of the buttons that can generate drag events. Defaults to the primary buttons (left, middle, right).
		/// </summary>
		public static MouseButtonMask DragMask = MouseButtonMask.Primary;
		#endregion // Fields

		#region GLFW Interop
		internal static void ButtonCallback(IntPtr window, int button, int action, int mods)
		{
			Log.LDEBUG($"Mouse Button: {button}");
		}

		internal static void ScrollCallback(IntPtr window, double xoffset, double yoffset)
		{
			Log.LDEBUG($"Mouse Scroll: {xoffset}, {yoffset}");
		}
		#endregion // GLFW Interop
	}
}
