using System;
using System.Linq;

namespace Prism
{
	// Contains functions to parse an argument list for specific flags
	public static class ArgParser
	{
		// Raw check for an argument in the list
		public static bool ContainsArgument(string[] args, string arg) => args.Contains('/' + arg);

		// If there is a help flag
		public static bool Help(string[] args) => ContainsArgument(args, "help") || ContainsArgument(args, "?");

		// If there is a verbose flag
		public static bool Verbose(string[] args) => ContainsArgument(args, "verbose") || ContainsArgument(args, "v");
	}
}
