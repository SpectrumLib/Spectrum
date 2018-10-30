using System;

namespace Spectrum.Input
{
	/// <summary>
	/// Represents and manages user input from the mouse.
	/// </summary>
	public static class Mouse
	{
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
