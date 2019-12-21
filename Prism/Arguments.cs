/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Linq;

namespace Prism
{
	// Parses the program arguments
	internal static class Arguments
	{
		#region Fields
		public static string ParseError = null;

		public static bool Help = false;

		public static string Action = null;
		public static string ActionArg = null;
		public static string Path = null;

		public static int Verbosity = 0;

		public static uint Parallel = 1;

		public static bool Debug = false;
		#endregion // Fields

		public static bool Parse(string[] args)
		{
			ParseError = null;

			// Prepare the arguments
			var argpairs = new (string name, string value)[args.Length];
			Prepare(args, argpairs);

			// Check first for the help flag, exit early if present
			if (argpairs.Any(arg => (arg.value is null) && (arg.name == "help" || arg.name == "h" || arg.name == "?")))
			{
				Help = true;
				return true;
			}

			// Check for the action and path
			if (argpairs[0].name != null)
			{
				ParseError = "First argument must be path or action.";
				return false;
			}
			if (argpairs[0].value.Contains('.')) // No actions have '.'
			{
				Action = "gui";
				Path = argpairs[0].value;
				return true; // No additional flags are supported when using the GUI
			}
			Action = argpairs[0].value;
			if (Action == "new")
			{
				if (args.Length < 3 || (argpairs[1].name != null) || (argpairs[2].name != null))
				{
					ParseError = "Not enough arguments for action 'new'.";
					return false;
				}
				ActionArg = argpairs[1].value;
				Path = argpairs[2].value;
			}
			else
			{
				if (args.Length < 2 || (argpairs[1].name != null))
				{
					ParseError = $"Not enough arguments for action '{Action}'.";
					return false;
				}
				Path = argpairs[1].value;
			}

			// Loop over parameters
			foreach (var param in argpairs.Skip(Action[0] == 'n' ? 3 : 2))
			{
				if (param.name is null)
				{
					ParseError = $"Too many arguments for action '{Action}'.";
					return false;
				}

				switch (param.name)
				{
					// Verbosity/Quiet
					case "v":     Verbosity = 1;  break;
					case "vv":    Verbosity = 2;  break;
					case "vvv":   Verbosity = 3;  break;
					case "q":
					case "quiet": Verbosity = -1; break;
					// Parallel thread count
					case "p":
					case "parallel":
						if (param.value != null)
						{
							if (!UInt32.TryParse(param.value, out Parallel))
							{
								CConsole.Warn($"Invalid value for thread count: {param.value}.");
								Parallel = 1;
							}
							Parallel = Math.Clamp(Parallel, 1u, (uint)Environment.ProcessorCount);
						}
						else
							Parallel = (uint)Environment.ProcessorCount;
						break;
					case "r":
					case "release":
						if (param.value == null)
							Debug = false;
						break;
					case "d":
					case "debug":
						if (param.value == null)
							Debug = true;
						break;
				}
			}

			return true;
		}

		private static void Prepare(string[] args, (string name, string value)[] sanargs)
		{
			int aidx = 0;
			foreach (var arg in args)
			{
				bool ispar = (arg.Length > 1) && ((arg[0] == '-') || (arg[0] == '/' && Program.IsWindows));
				sanargs[aidx++] = ispar ? _split_param(arg) : (null, arg);
			}

			static (string, string) _split_param(ReadOnlySpan<char> par)
			{
				var idx = par.IndexOfAny(':', '=');
				var plen = (par[1] == '-') ? 2 : 1;
				var name = (idx != -1) ? par.Slice(plen, idx - plen) : par.Slice(plen);
				var value = (idx != -1) ? par.Slice(idx + 1) : ReadOnlySpan<char>.Empty;
				return (name.ToString(), value.IsEmpty ? null : value.ToString());
			}
		}
	}
}
