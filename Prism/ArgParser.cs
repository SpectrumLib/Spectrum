using System;
using System.Linq;

namespace Prism
{
	// Contains functions to parse an argument list for specific flags
	public static class ArgParser
	{
		private static readonly char[] COLON_SPLIT = { ':' };

		// Raw check for an argument in the list
		public static bool ContainsArgument(string[] args, string arg) => args.Contains('/' + arg);

		// Checks for a value argument (/<arg>:<value>)
		public static bool ContainsValueArgument(string[] args, string arg, out string value)
		{
			value = null;

			var srcArg = args.FirstOrDefault(a => a.StartsWith('/' + arg + ':'));
			if (srcArg == null)
				return false;

			var aval = srcArg.Split(COLON_SPLIT, 2)[1];
			if (aval.Length > 0)
				value = aval;
			return true;
		}

		// If there is a help flag
		public static bool Help(string[] args) => ContainsArgument(args, "help") || ContainsArgument(args, "?");

		// If there is a verbose flag
		public static bool Verbose(string[] args) => ContainsArgument(args, "verbose") || ContainsArgument(args, "v");

		// If there is a parallel flag, and the number of threads it specifies (pre clamped to CPU count)
		//   Returns 1 if there is no parallel flag
		public static uint Parallel(string[] args)
		{
			uint cpuCount = (uint)Environment.ProcessorCount;

			// Check for default flag (use as many as possible)
			if (ContainsArgument(args, "parallel") || ContainsArgument(args, "p"))
				return cpuCount;

			// Check for a specified amount of threads
			string rawValue = null;
			if (ContainsValueArgument(args, "parallel", out rawValue) || ContainsValueArgument(args, "p", out rawValue))
			{
				// Nothing after the colon
				if (rawValue == null)
					return cpuCount;

				// Try to parse it
				if (!Int32.TryParse(rawValue, out int pValue))
				{
					Console.WriteLine($"WARN: The parallel count '{rawValue}' is not a valid integer, using one thread.");
					return 1;
				}

				// Check the different conditions
				if (pValue == 0)
					return cpuCount;
				else if (pValue < 0)
				{
					Console.WriteLine("WARN: The parallel count cannot be negative, using one thread.");
					return 1;
				}
				else if (pValue > cpuCount)
				{
					Console.WriteLine($"WARN: The parallel count {pValue} is greater than the number of cpu cores, clamping.");
					return cpuCount;
				}

				return (uint)pValue;
			}

			// No flag specified, only use a single thread
			return 1;
		}
	}
}
