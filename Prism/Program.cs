using System;
using System.Linq;

namespace Prism
{
	public static class Program
	{
		private static readonly string[] VALID_ACTIONS = { "new", "build", "rebuild", "clean" };

		public static int Main(string[] args)
		{
			// No arguments = open gui with no project (or just exit for now)
			if (args.Length == 0)
			{
				Console.WriteLine("ERROR: The GUI for Prism is not yet complete. Please use the command line interface for now.");
				Console.WriteLine("       Usage: Prism.exe <action> <file> [args]");
				return -1;
			}

			// Convert all of the flags to lower case and normalizes the first character
			args = args.Select(arg => {
				bool isFlag = arg.StartsWith("/") || arg.StartsWith("-");
				if (isFlag) return '/' + arg.Substring(arg.StartsWith("--") ? 2 : 1).ToLower();
				return arg;
			}).ToArray();

			// Check for the help flag
			if (ArgParser.Help(args))
			{
				PrintHelp();
				return 0;
			}

			// If we want verbose
			bool verbose = ArgParser.Verbose(args);

			// Check for a valid action
			string action;
			if (!GetAction(args, out action))
			{
				Console.WriteLine($"ERROR: The action '{action}' is not valid. Please use one of: {String.Join(", ", VALID_ACTIONS)}.");
				return -1;
			}

			// Make sure there are enough arguments
			if (args.Length < 2)
			{
				Console.WriteLine("ERROR: Not enough command line arguments specified.");
				return -1;
			}

			// Dispatch the action to the proper handler
			switch (action)
			{
				case "new": return NewFile.Create(args, verbose);
				case "build":
				case "rebuild":
				case "clean": return CommandLineAction.RunAction(action, args, verbose);
				default: Console.WriteLine($"ERROR: The action '{action}' is not yet implemented."); return -1;
			}
		}

		private static bool GetAction(string[] args, out string act) => VALID_ACTIONS.Contains(act = args[0].ToLower());

		private static void PrintHelp()
		{
			// ==================================================================================================================
			Console.WriteLine("\nPrism\n-----");
			Console.WriteLine("Prism is an extensible build tool and project manager for pre-processing content files for the\n" +
							  "Spectrum library. It can be used in both command-line and user interface mode.");

			Console.WriteLine("\nUsage:             Prism.exe <action> <file> [args]");
			Console.WriteLine("<action> can be one of:");
			Console.WriteLine("    > new <type>     - Creates a new content file of the given type.");
			Console.WriteLine("    > build          - Builds the project file.");
			Console.WriteLine("    > rebuild        - Builds the project file, ignoring the current cache for a full rebuild.");
			Console.WriteLine("    > clean          - Cleans the cache and the output for the project file.");
			Console.WriteLine("The action must come as the first argument to the program. The GUI will open if one of the valid\n" +
							  "actions is not specified. The content file to operate on must always come immediately after the\n" +
							  "action.");

			Console.WriteLine("\nThe command line flags are:");
			Console.WriteLine("    > /help;/?       - Prints this help message, and then quits.");
			Console.WriteLine("    > /verbose;/v    - Prints out more information while running. Valid for all commands.");
			Console.WriteLine("For compatibility, all flags can be specified with '/', '-', or '--'.\n");
		}
	}
}
