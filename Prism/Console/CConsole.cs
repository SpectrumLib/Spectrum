/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;

namespace Prism
{
    // Console wrapper that adds colors and message severity tags
	internal static class CConsole
	{
        private static readonly ConsoleColor FORE_DEFAULT;

        static CConsole()
        {
            FORE_DEFAULT = Console.ForegroundColor;
        }

        public static void Info(string msg) => Console.WriteLine(msg);

        public static void Warn(string msg)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"W: {msg}");
            Console.ForegroundColor = FORE_DEFAULT;
        }

        public static void Error(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"E: {msg}");
            Console.ForegroundColor = FORE_DEFAULT;
        }

        public static void Verbose(string msg)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(msg);
            Console.ForegroundColor = FORE_DEFAULT;
        }
	}
}
