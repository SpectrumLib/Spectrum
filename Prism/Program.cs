using System;
using System.Linq;

namespace Prism
{
	public static class Program
	{
		public static void Main(string[] args)
		{
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
				return;
			}
		}

		private static bool ContainsArgument(string[] args, string arg) => args.Contains('/' + arg);

		private static void PrintHelp()
		{
			Console.WriteLine("\nPrism\n-----");
			Console.WriteLine(
				"Prism is an extensible build tool and project manager for pre-processing content\n" +
				"files for the Spectrum library. It can be used in both command-line and user interface\n" +
				"mode. Its command line arguments are:\n"
			);
			Console.WriteLine("    > /help;/?   - Prints this help message, and then quits.");
			Console.WriteLine("\nFor compatibility, all arguments can be specified with '/', '-', or '--'.\n");
		}
	}
}
