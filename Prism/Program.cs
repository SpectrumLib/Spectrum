/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.InteropServices;

namespace Prism
{
	internal static class Program
	{
		#region Fields
		public static bool IsWindows => _IsWindows ?? 
			(_IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)).Value;
		private static bool? _IsWindows = null;
		#endregion // Fields

		static int Main(string[] args)
		{
			if (args.Length == 0)
			{
				CConsole.Info("Usage (cmd):  Prism.exe <action> <project> [args]");
				CConsole.Info("Usage (gui):  Prism.exe <project>");
				CConsole.Info("Usage (help): Prism.exe /(help|h|?)");
				return 0;
			}

			// Parse the arguments
			if (!Arguments.Parse(args))
			{
				CConsole.Error(Arguments.ParseError);
				return -1;
			}

			// Display help and exit
			if (Arguments.Help)
			{
				PrintHelp();
				return 0;
			}

			// GUI not supported yet
			if (Arguments.Action == "gui")
			{
				CConsole.Error("Prism GUI not yet ready - please use the command line.");
				return -1;
			}

			// Call the action handler
			switch (Arguments.Action)
			{
				case "new": return NewAction.Process() ? 0 : -1;
				case "view": return ViewAction.Process() ? 0 : -1;
				case "clean": return CleanAction.Process() ? 0 : -1;
				case "build": return BuildAction.Process(false) ? 0 : -1;
				case "rebuild": return BuildAction.Process(true) ? 0 : -1;
				default:
					CConsole.Error($"No such action: {Arguments.Action}.");
					return -1;
			}
		}

		private static void PrintHelp()
		{
			Console.Write(
				// ===========================================================================
				"\nPrism" +
				"\n-----" +
				"\nPrism is an extensible build tool and project manager for content file" +
				"\npre-processing for the Spectrum library." +
				"\n" +
				"\nUsage (gui):    Prism.exe <project_file>" +
				"\nUsage (cmd):    Prism.exe <action> <project_file> [args...]" +
				"\n   <action> can be one of:" +
				"\n      new <type>    - Creates a default file of <type> at the given path." +
				"\n      build         - Builds the project file." +
				"\n      rebuild       - Rebuilds the project file, ignoring the build cache." +
				"\n      clean         - Cleans the build cache for the project file." +
				"\n      view          - Shows a summary of the project file." +
				"\n   The action must come as the first argument of the program for command-" +
				"\n   line mode. The project file must be the second argument. A project file" +
				"\n   as the first argument will cause the GUI to open." +
				"\n" + 
				"\nThe command line parameters are:" +
				"\n   > help;h;?       - Prints this message, and exits immediately." +
				"\n   > q;quiet        - Prints minimal output messages (overrides 'v' flags)." +
				"\n   > v;vv;vvv       - Makes verbose output messages, with increasing levels" +
				"\n                        of verbosity." +
				"\n   > p;parallel     - For 'build' and 'rebuild' tasks, this sets the number" +
				"\n     [=<int>]           of threads to use. Not specifying a count will use" +
				"\n                        the number of cores on the system." +
				"\n   > hc             - Use high (max) compression for release builds that" +
				"\n                        use compression. This will significantly increase" +
				"\n                        the build time, but not the runtime load time." +
				"\n   > r;release      - Set (re)build tasks to be release." +
				"\n   > d;debug        - Set (re)build tasks to be debug (default)." +
				"\n" + 
				"\nParameters can be specified with '-', '--', and '/' (on Windows)." +
				"\n\n"
			);
		}
	}
}
