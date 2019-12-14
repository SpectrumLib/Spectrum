/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;

namespace Prism
{
	internal static class Program
	{
		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				CConsole.Info("Usage (cmd):  Prism.exe <action> <project> [args]");
				CConsole.Info("Usage (gui):  Prism.exe <project>");
				CConsole.Info("Usage (help): Prism.exe /(help|h|?)");
				return;
			}

			// Parse the arguments
			if (!Arguments.Parse(args))
			{
				CConsole.Error(Arguments.ParseError);
				return;
			}

			// Display help and exit
			if (Arguments.Help)
			{
				PrintHelp();
				return;
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
				"\n" + 
				"\nFor compatiblity, all parameters can be specified with '/', '-', or '--'." +
				"\n\n"
			);
		}
	}
}
