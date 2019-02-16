using System;
using System.Collections.Generic;
using System.Linq;

namespace Prism
{
	// Contains functions to parse an argument list for specific flags
	public static class ArgParser
	{
		private static readonly char[] COLON_SPLIT = { ':' };
		private static readonly char[] PARAM_SPLIT = { ':', '=' };

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

		// Santizes the flag beginnings and makes the flags lowercase
		public static string[] Sanitize(string[] args)
		{
			return args.Select(arg => {
				bool isFlag = arg.StartsWith("/") || arg.StartsWith("-");
				if (isFlag)
				{
					var comps = arg.Split(COLON_SPLIT, 2);
					var end = (comps.Length > 1) ? $":{comps[1]}" : "";
					return '/' + comps[0].Substring(comps[0].StartsWith("--") ? 2 : 1).ToLower() + end;
				}
				return arg;
			}).ToArray();
		}

		// If there is a help flag
		public static bool Help(string[] args) => ContainsArgument(args, "help") || ContainsArgument(args, "?") || ContainsArgument(args, "h");

		// If there is a verbose flag
		public static bool Verbose(string[] args) => ContainsArgument(args, "verbose") || ContainsArgument(args, "v");

		// If there is a release build flag
		public static bool Release(string[] args) => ContainsArgument(args, "release") || ContainsArgument(args, "r");

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
					CConsole.Warn($"The parallel count '{rawValue}' is not a valid integer, using one thread.");
					return 1;
				}

				// Check the different conditions
				if (pValue == 0)
					return cpuCount;
				else if (pValue < 0)
				{
					CConsole.Warn("The parallel count cannot be negative, using one thread.");
					return 1;
				}
				else if (pValue > cpuCount)
				{
					CConsole.Warn($"The parallel count {pValue} is greater than the number of cpu cores, clamping.");
					return cpuCount;
				}

				return (uint)pValue;
			}

			// No flag specified, only use a single thread
			return 1;
		}

		// Attempts to parse all of the /param arguments and their values
		public static Dictionary<string, object> Params(string[] args)
		{
			Dictionary<string, object> plist = new Dictionary<string, object>();

			if (ContainsValueArgument(args, "ipath", out string rawarg))
			{
				if (Uri.IsWellFormedUriString(rawarg, UriKind.RelativeOrAbsolute))
					plist.Add("ipath", rawarg);
				else
					CConsole.Error("Invalid path value for 'ipath' parameter.");
			}

			if (ContainsValueArgument(args, "opath", out rawarg))
			{
				if (Uri.IsWellFormedUriString(rawarg, UriKind.RelativeOrAbsolute))
					plist.Add("opath", rawarg);
				else
					CConsole.Error("Invalid path value for 'opath' parameter.");
			}

			if (ContainsValueArgument(args, "compress", out rawarg))
			{
				if (Boolean.TryParse(rawarg, out var compress))
					plist.Add("compress", compress);
				else
					CConsole.Error("Invalid boolean value for 'compress' parameter.");
			}

			if (ContainsValueArgument(args, "size", out rawarg))
			{
				if (UInt32.TryParse(rawarg, out var packSize))
				{
					if (packSize <= 0 || packSize > 2048)
						CConsole.Error($"The pack size ({packSize}) must be between 1MB (1) and 2GB (2048).");
					else
						plist.Add("packSize", packSize);
				}
				else
					CConsole.Error("Invalid unsigned integer value for 'size' parameter.");
			}
			else if (ContainsArgument(args, "size"))
			{
				plist.Add("packSize", 512u);
			}

			return plist;
		}
	}
}
