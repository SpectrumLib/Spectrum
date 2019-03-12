using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Prism.Builtin
{
	// Provides the interface to the native glslangValidator tool in the Vulkan SDK
	internal static class GLSLV
	{
		#region Fields
		public static readonly string TOOL_PATH; // The absolute path to the executable
		#endregion // Fields

		static GLSLV()
		{
			bool isWin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
			if (!isWin) // Not ideal but required until more testing can be done
				throw new PlatformNotSupportedException("Cannot use GLSLV on non-Windows platforms (yet).");

			// Define the locator process
			ProcessStartInfo psi = new ProcessStartInfo {
				FileName = isWin ? "powershell" : "/bin/sh",
				Arguments = isWin ? "-command \"get-command glslangValidator\"" : "-c 'which glslangValidator'",
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				WindowStyle = ProcessWindowStyle.Hidden
			};

			// Run the locator process
			string stderr = null;
			string stdout = null;
			using (Process proc = new Process())
			{
				proc.StartInfo = psi;
				proc.Start();
				proc.WaitForExit(250);
				stderr = proc.StandardError.ReadToEnd();
				stdout = proc.StandardOutput.ReadToEnd();
			}

			// Check for the error
			if (!String.IsNullOrEmpty(stderr))
				throw new PlatformNotSupportedException("You must have the Vulkan SDK installed and in the system path.");

			// Parse the results (Windows 'get-command' returns a lot of cruft)
			if (isWin)
			{
				// Trim the whitespace (empty lines) and split into lines
				var lines = stdout.Trim().Split('\n');

				// Get the starting index for the path from the header, and extract the path from line 3
				var pi = lines[0].IndexOf("Source");
				TOOL_PATH = lines[2].Substring(pi).TrimEnd();
			}
			else
				TOOL_PATH = stdout; // Easy, as it is the only thing in the resulting string
		}
	}
}
