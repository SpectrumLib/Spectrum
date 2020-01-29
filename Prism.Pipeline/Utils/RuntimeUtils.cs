/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.InteropServices;

namespace Prism.Pipeline
{
	// Contains various runtime utilties and extern calls to system APIs
	internal static class RuntimeUtils
	{
		public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
		public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
		public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
		public static bool IsBSD => RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);

		public static class Win32
		{
			[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
			public static extern Int32 CreateHardLink(
				string lpFileName,
				string lpExistingFileName,
				IntPtr lpSecurityAttributes
			);
		}

		public static class Posix
		{
			[DllImport("libc", SetLastError = true)]
			public static extern Int32 symlink(
				string oldpath,
				string newpath
			);
		}
	}
}
