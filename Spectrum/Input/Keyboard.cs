using System;

namespace Spectrum.Input
{
	/// <summary>
	/// Represents and manages user input from the keyboard. Keyboard input can be both event and polling based.
	/// </summary>
	public static class Keyboard
	{
		#region GLFW Interop
		internal static void KeyCallback(IntPtr window, int key, int scancode, int action, int mods)
		{
			Log.LDEBUG($"Keyboard Key: {key}");
		}
		#endregion // GLFW Interop
	}
}
