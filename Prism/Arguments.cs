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
		#endregion // Fields

		public static bool Parse(string[] args)
		{
			// Prepare the arguments
			var argpairs = new (string name, string value)[args.Length];
			Prepare(args, argpairs);

			// Check first for the help flag, exit early if present
			if (argpairs.Any(arg => (arg.value is null) && (arg.name == "help" || arg.name == "h" || arg.name == "?")))
			{
				Help = true;
				return true;
			}

			return true;
		}

		private static void Prepare(string[] args, (string name, string value)[] sanargs)
		{
			int aidx = 0;
			foreach (var arg in args)
			{
				bool ispar = (arg.Length > 1) && ((arg[0] == '-') || (arg[0] == '/'));
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
