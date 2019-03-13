using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

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

		public static bool CompileModule(in PSSModule mod, string srcDir, string outFile, PipelineLogger logger)
		{
			// Get input file
			if (!PathUtils.TryGetFullPath(mod.SourceFile, out var fullPath, srcDir))
			{
				logger.Error($"The shader file path '{mod.SourceFile}' is not a valid path.");
				return false;
			}

			// Build the arguments
			StringBuilder args = new StringBuilder(256);
			args.Append("-V -l -q "); // Standard arguments (generate SPIR-V, link to output, echo reflection info)
			args.Append("-S ");
			args.Append(mod.Type); // Stage info
			args.Append(" -o \"");
			args.Append(outFile); // Output file
			args.Append("\" ");
			foreach (var mac in mod.Macros) // Specialization preprocessor macros
			{
				args.Append("-D");
				args.Append(mac);
				args.Append(' ');
			}
			args.Append('\"');
			args.Append(fullPath);
			args.Append('\"');
			logger.Info($"Building with args: {args.ToString()}");

			// Create the process info
			ProcessStartInfo psi = new ProcessStartInfo {
				FileName = TOOL_PATH,
				Arguments = args.ToString(),
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				WindowStyle = ProcessWindowStyle.Hidden
			};

			// Run the compiler
			string stdout = null;
			using (Process proc = new Process())
			{
				proc.StartInfo = psi;
				proc.Start();
				proc.WaitForExit(5);
				stdout = proc.StandardOutput.ReadToEnd(); // Contains errors and reflection dump
			}

			// Report any errors
			if (stdout.Contains("ERROR:"))
			{
				logger.Error($"Unable to compile shader, reason(s):");
				foreach (var err in ParseError(stdout, mod.SourceFile, fullPath))
					logger.Error($"     {err}");
				return false;
			}

			return true;
		}

		private static string[] ParseError(string stdout, string file, string path)
		{
			var split = stdout.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
				.Where(line => line.StartsWith("ERROR:"))
				.Select(line => line.Substring(7))
				.Where(line => line.StartsWith(path))
				.Select(line => file + line.Substring(path.Length))
				.ToList();

			return split.Take(split.Count - 1).ToArray(); // Last line is just a report that compilation failed
		}
	}
}
