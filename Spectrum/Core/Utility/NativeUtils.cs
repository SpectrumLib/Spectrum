/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Spectrum
{
	// Utility functionality for working with native libraries and native interop
	internal static class NativeUtils
	{
		// Attempts to load 
		[SuppressUnmanagedCodeSecurity]
		public static T LoadFunction<T>(IntPtr lib, string fname)
			where T : Delegate
		{
			if (NativeLibrary.TryGetExport(lib, fname, out var addr))
			{
				try
				{
					return Marshal.GetDelegateForFunctionPointer<T>(addr);
				}
				catch
				{
					throw new InvalidOperationException($"The function '{fname}' does not match the provided delegate type");
				}
			}
			throw new ArgumentException($"The function '{fname}' cannot be found in the library.", nameof(fname));
		}
	}
}
