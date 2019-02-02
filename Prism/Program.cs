using System;
using System.Linq;

namespace Prism
{
	public static class Program
	{
		private static readonly string[] VALID_COMMANDS = { "new", "build", "rebuild", "clean" };

		public static int Main(string[] args)
		{
			// No arguments = open gui with no project (or just exit for now)
			if (args.Length == 0)
			{
				Console.WriteLine("ERROR: The GUI for Prism is not yet complete. Please use the command line interface for now.");
				Console.WriteLine("       Usage: Prism.exe [command] <file> [args]");
				return -1;
			}

			// Convert all of the flags to lower case and normalizes the first character
			args = args.Select(arg => {
				bool isFlag = arg.StartsWith("/") || arg.StartsWith("-");
				if (isFlag) return '/' + arg.Substring(arg.StartsWith("--") ? 2 : 1).ToLower();
				return arg;
			}).ToArray();

			// Check for the help flag
			if (ContainsArgument(args, "help") || ContainsArgument(args, "?"))
			{
				PrintHelp();
				return 0;
			}

			// If we want verbose
			bool verbose = ContainsArgument(args, "verbose") || ContainsArgument(args, "v");

			// Check for a valid command
			string command;
			if (!GetCommand(args, out command))
			{
				Console.WriteLine($"ERROR: The command '{command}' is not valid. Please use one of: {String.Join(", ", VALID_COMMANDS)}.");
				return -1;
			}

			// Dispatch the command to the proper handler
			switch (command)
			{
				case "new": return NewFile.Create(args, verbose);
				default: Console.WriteLine($"ERROR: The command '{command}' is not yet implemented."); return -1;
			}
		}

		public static bool ContainsArgument(string[] args, string arg) => args.Contains('/' + arg);

		private static bool GetCommand(string[] args, out string cmd) => VALID_COMMANDS.Contains(cmd = args[0].ToLower());

		private static void PrintHelp()
		{
			Console.WriteLine("\nPrism\n-----");
			Console.WriteLine("Prism is an extensible build tool and project manager for pre-processing content\n" +
							  "files for the Spectrum library. It can be used in both command-line and user interface\n" +
							  "mode.");

			Console.WriteLine("\nUsage:             Prism.exe [command] <file> [args]");
			Console.WriteLine("[command] can be one of:");
			Console.WriteLine("    > new <type>     - Creates a new content file of the given type.");
			Console.WriteLine("    > build          - Builds the project file.");
			Console.WriteLine("    > rebuild        - Builds the project file, ignoring the current cache for a full rebuild.");
			Console.WriteLine("    > clean          - Cleans the cache and the output for the project file.");
			Console.WriteLine("The command must come as the first argument to the program. The GUI will open if one of these\n" +
							  "commands is not specified.");

			Console.WriteLine("\nThe command line arguments are:");
			Console.WriteLine("    > /help;/?       - Prints this help message, and then quits.");
			Console.WriteLine("    > /verbose;/v    - Prints out more information while running. Valid for all commands.");
			Console.WriteLine("For compatibility, all arguments can be specified with '/', '-', or '--'.\n");
		}
	}
}
