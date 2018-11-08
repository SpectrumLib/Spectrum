using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using OpenAL;

namespace Spectrum.Audio
{
	// Utility functionality for working with OpenAL
	internal static class ALUtils
	{
		// Last error from AL
		public static int LastALError { get; private set; } = AL10.AL_NO_ERROR;
		// Last error from ALC
		public static int LastALCError { get; private set; } = ALC10.ALC_NO_ERROR;

		// Calls alGetString() with error checking
		public static string GetALString(int param)
		{
			string str = AL10.alGetString(param);
			CheckALError($"could not get AL string param {param}");
			return str;
		}

		// Helper function to call alcGetString() and translate the result
		// Passing in In32.MaxValue for device will use the current device, or pass any other value to use that device pointer
		public static string GetALCString(int param, int device = Int32.MaxValue)
		{
			var sPtr = ALC10.alcGetString((device == Int32.MaxValue) ? AudioEngine.Device : (IntPtr)device, param);
			CheckALCError($"could not get ALC string param {param}");
			if (sPtr == IntPtr.Zero)
				return null;

			// We need to handle the string lists in a different way, as they dont play well with Marshal.PtrToStringAnsi()
			// Scan through until a double null terminator is found, and return as a newline separated list
			if (device == 0 && (param == ALC10.ALC_DEVICE_SPECIFIER || param == ALC11.ALC_CAPTURE_DEVICE_SPECIFIER || param == ALC11.ALC_ALL_DEVICES_SPECIFIER))
			{
				// Copy the string data to managed memory
				byte[] charData = new byte[GetStringListPtrLength(sPtr)];
				Marshal.Copy(sPtr, charData, 0, charData.Length);

				// Find split indices for null characters
				var splits = charData.Select((b, i) => b == 0 ? i : -1).Where(i => i != -1).ToList();
				splits.Insert(0, -1); // Add the beginning of the first string (pos = 0)
				splits.RemoveAt(splits.Count - 1); // Remove the secondary null terminator at the very end

				// Create list of strings
				List<string> strList = new List<string>();
				for (int i = 0; i < (splits.Count - 1); ++i)
					strList.Add(Encoding.ASCII.GetString(charData, splits[i] + 1, splits[i + 1] - splits[i] - 1));

				// Return newline separated list
				return String.Join("\n", strList);

			}
			else
				return Marshal.PtrToStringAnsi(sPtr);
		}

		private static int GetStringListPtrLength(IntPtr sPtr)
		{
			unsafe
			{
				byte* ptr = (byte*)sPtr.ToPointer();
				int length = 0;
				// Scan until two adjacent null terminators are found
				while ((*(ptr++) != 0) || (*ptr != 0)) ++length;
				return length + 2;
			}
		}

		#region Error Checking
		// Simply clears any AL error without reporting it
		public static void ClearALError()
		{
			AL10.alGetError();
		}

		// Simply clears any ALC error without reporting it
		public static void ClearALCError()
		{
			ALC10.alcGetError(AudioEngine.Device);
		}

		// Checks for an AL error, and throws an exception if one is found
		[Conditional("DEBUG")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void CheckALError(
			string message = null,
			[CallerMemberName] string name = "",
			[CallerLineNumber] int line = 0
		)
		{
			LastALError = AL10.alGetError();
			if (LastALError != AL10.AL_NO_ERROR)
			{
				string error = GetALErrorString(LastALError);
				throw new AudioException($"AL Error ({LastALError}) at [{name}:{line}]: {error} {(String.IsNullOrWhiteSpace(message) ? "" : $"({message})")}");
			}
		}

		// Checks for an ALC error, and throws an exception if one if found
		[Conditional("DEBUG")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void CheckALCError(
			string message = null,
			[CallerMemberName] string name = "",
			[CallerLineNumber] int line = 0
		)
		{
			LastALCError = ALC10.alcGetError(AudioEngine.Device);
			if (LastALCError != ALC10.ALC_NO_ERROR)
			{
				string error = GetALCErrorString(LastALCError);
				throw new AudioException($"ALC Error ({LastALCError}) at [{name}:{line}]: {error} {(String.IsNullOrWhiteSpace(message) ? "" : $"({message})")}");
			}
		}

		// Gets a string describing the AL error code
		public static string GetALErrorString(int err)
		{
			switch (err)
			{
				case AL10.AL_NO_ERROR: return "No error";
				case AL10.AL_INVALID_NAME: return "Invalid name specified";
				case AL10.AL_INVALID_ENUM: return "Invalid enum for operation";
				case AL10.AL_INVALID_VALUE: return "Invalid parameter value for operation";
				case AL10.AL_INVALID_OPERATION: return "Illegal operation call";
				case AL10.AL_OUT_OF_MEMORY: return "Out of memory";
				default: return $"AL error code not understood ({err})";
			}
		}

		// Gets a string describing the ALC error code
		public static string GetALCErrorString(int err)
		{
			switch (err)
			{
				case ALC10.ALC_NO_ERROR: return "No error";
				case ALC10.ALC_INVALID_DEVICE: return "Invalid device handle/name";
				case ALC10.ALC_INVALID_CONTEXT: return "Invalid context handle/name";
				case ALC10.ALC_INVALID_ENUM: return "Invalid enum for operation";
				case ALC10.ALC_INVALID_VALUE: return "Invalid parameter value for operation";
				case ALC10.ALC_OUT_OF_MEMORY: return "Out of memory";
				default: return $"ALC error code not understood ({err})";
			}
		}
		#endregion // Error Checking
	}
}
