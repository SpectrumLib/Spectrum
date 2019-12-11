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
				CConsole.Info("Usage (cmd): Prism.exe <action> <project> [args]");
				CConsole.Info("Usage (gui): Prism.exe <project>");
			}
		}
	}
}
