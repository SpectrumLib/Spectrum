using System;

namespace Spectrum.Input
{
	/// <summary>
	/// Represents and manages user input from the keyboard. Keyboard input can be both event and polling based.
	/// </summary>
	public static class Keyboard
	{
		#region Fields
		// Track states and times
		private static readonly bool[] s_lastKeys = new bool[KeyUtils.MAX_KEY_INDEX + 1];
		private static readonly bool[] s_currKeys = new bool[KeyUtils.MAX_KEY_INDEX + 1];
		private static readonly float[] s_lastPress = new float[KeyUtils.MAX_KEY_INDEX + 1];
		private static readonly float[] s_lastRelease = new float[KeyUtils.MAX_KEY_INDEX + 1];
		private static readonly float[] s_lastHold = new float[KeyUtils.MAX_KEY_INDEX + 1];
		#endregion // Fields

		#region GLFW Interop
		internal static void KeyCallback(IntPtr window, int key, int scancode, int action, int mods)
		{
			if (action == Glfw.REPEAT) return; // We generate our own repeat events

			Keys keys = KeyUtils.Translate(key);
			Console.WriteLine($"Key: {keys}");
			Log.LDEBUG($"Keyboard Key: {keys}");
		}
		#endregion // GLFW Interop
	}
}
