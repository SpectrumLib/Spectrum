﻿using System;
using System.Linq;

namespace Prism
{
	public static class Program
	{
		private static readonly string[] VALID_ACTIONS = { "new", "build", "rebuild", "clean", "view" };

		public static int Main(string[] args)
		{
			// No arguments = open gui with no project (or just exit for now)
			if (args.Length == 0)
			{
				CConsole.Error("The GUI for Prism is not yet complete. Please use the command line interface for now.");
				CConsole.Error("    Usage: Prism.exe <action> <file> [args]");
				return -1;
			}

			// Convert all of the flags to lower case and normalizes the first character
			args = ArgParser.Sanitize(args);

			// Check for the help flag
			if (ArgParser.Help(args))
			{
				PrintHelp();
				return 0;
			}

			// If we want verbose
			bool verbose = ArgParser.Verbose(args);

			// Check for a valid action
			if (!GetAction(args, out string action))
			{
				CConsole.Error($"The action '{action}' is not valid. Please use one of: {String.Join(", ", VALID_ACTIONS)}.");
				return -1;
			}

			// Make sure there are enough arguments
			if (args.Length < 2)
			{
				CConsole.Error("Not enough command line arguments specified.");
				return -1;
			}

			// Dispatch the action to the proper handler
			switch (action)
			{
				case "new": return NewFile.Create(args, verbose);
				case "build":
				case "rebuild":
				case "clean": return CommandLineAction.RunAction(action, args, verbose);
				case "view": return ViewProject.Summarize(args, verbose);
				default: CConsole.Error($"The action '{action}' is not yet implemented."); return -1;
			}
		}

		private static bool GetAction(string[] args, out string act) => VALID_ACTIONS.Contains(act = args[0].ToLower());

		private static void PrintHelp()
		{
			// ==================================================================================================================
			Console.WriteLine();
			Console.WriteLine("Prism");
			Console.WriteLine("-----");
			Console.WriteLine("Prism is an extensible build tool and project manager for pre-processing content files for the\n" +
							  "Spectrum library. It can be used in both command-line and user interface mode.");

			Console.WriteLine();
			Console.WriteLine("Usage:             Prism.exe <action> <file> [args]");
			Console.WriteLine("<action> can be one of:");
			Console.WriteLine("    > new <type>     - Creates a new content file of the given type.");
			Console.WriteLine("    > build          - Builds the project file.");
			Console.WriteLine("    > rebuild        - Builds the project file, ignoring the current cache for a full rebuild.");
			Console.WriteLine("    > clean          - Cleans the cache and the output for the project file.");
			Console.WriteLine("    > view           - Shows a summary of the content project file.");
			Console.WriteLine("The action must come as the first argument to the program. The GUI will open if one of the valid\n" +
							  "actions is not specified. The content file to operate on must always come immediately after the\n" +
							  "action.");

			Console.WriteLine();
			Console.WriteLine("The command line flags are:");
			Console.WriteLine("    > /help;/h;/?             - Prints this help message, and then quits.");
			Console.WriteLine("    > /verbose;/v             - Prints out more information while running. Valid for all commands.");
			Console.WriteLine("    > /parallel[:<num>]       - Sets the number of threads to use in build or rebuild tasks. If");
			Console.WriteLine("      /p[:<num>]                '0' or no number is specified, then the number of threads will be");
			Console.WriteLine("                                set to the numer of cores. If not specified, only a single thread");
			Console.WriteLine("                                will be used. The thread count will be clamped to the number of");
			Console.WriteLine("                                available cores.");
			Console.WriteLine("    > /release;/r             - Sets that the build will be in release mode, which packs and");
			Console.WriteLine("                                potentially compresses the content items for distribution. If this");
			Console.WriteLine("                                flag is not present, the build will be debug mode. Only valid for");
			Console.WriteLine("                                build or rebuild actions.");
			Console.WriteLine("    > /ipath:<value>          - Sets the root path for the intermediate content files.");
			Console.WriteLine("    > /opath:<value>          - Sets the root path for the final output content files.");
			Console.WriteLine("    > /compress:<value>       - Sets if the content files are compressed in release builds.");
			Console.WriteLine("    > /size[:<value>]         - Sets the maximum size of the content pack files for release mode");
			Console.WriteLine("                                builds, in MB. If no value is specified, than it forces the pipeline");
			Console.WriteLine("                                to use the default value of 512 MB. This has no effect in debug builds.");
			Console.WriteLine("For compatibility, all flags can be specified with '/', '-', or '--'.");
			Console.WriteLine();
		}
	}
}
