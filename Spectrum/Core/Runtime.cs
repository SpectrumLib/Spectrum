/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Spectrum
{
	/// <summary>
	/// Contains information about the runtime platform and environment of the current application.
	/// </summary>
	public static class Runtime
	{
		/// <summary>
		/// Information about the runtime operating system.
		/// </summary>
		public static class OS
		{
			#region Fields
			/// <summary>
			/// The current operating system family.
			/// </summary>
			public static readonly OSFamily Family;

			/// <summary>
			/// If the operating system is a Microsoft Windows desktop variant.
			/// </summary>
			public static bool IsWindows => Family == OSFamily.Windows;
			/// <summary>
			/// If the operating system is an Apple Macintosh OSX desktop version.
			/// </summary>
			public static bool IsOSX => Family == OSFamily.OSX;
			/// <summary>
			/// If the operating system is a supported desktop Linux variant.
			/// </summary>
			public static bool IsLinux => Family == OSFamily.Linux;
			/// <summary>
			/// If the operating system is POSIX compliant.
			/// </summary>
			public static bool IsPosix => Family != OSFamily.Windows;

			/// <summary>
			/// The version of the operating system.
			/// </summary>
			public static readonly Version Version;
			#endregion // Fields

			static OS()
			{
				Family = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? OSFamily.Windows :
						 RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? OSFamily.OSX :
						 RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? OSFamily.Linux :
						 throw new InvalidOperationException("Unable to run Spectrum applications on FreeBSD.");
				Version = Environment.OSVersion.Version;
			}
		}

		/// <summary>
		/// Information about the runtime platform CPU.
		/// </summary>
		public static class CPU
		{
			#region Fields
			/// <summary>
			/// The number of logical (physical + hyperthreaded) processors on the CPU.
			/// </summary>
			public static readonly uint ProcCount;
			#endregion // Fields

			static CPU()
			{
				ProcCount = (uint)Environment.ProcessorCount;
			}
		}

		/// <summary>
		/// Information about the runtime platform physical memory.
		/// </summary>
		public static class Memory
		{
			#region Fields
			#endregion // Fields

			static Memory()
			{

			}
		}
	}

	/// <summary>
	/// Represents the set of operating systems that Spectrum applications can run on.
	/// </summary>
	public enum OSFamily
	{
		/// <summary>
		/// Microsoft Windows desktop environment.
		/// </summary>
		Windows,
		/// <summary>
		/// Apple Macintosh OSX desktop environment.
		/// </summary>
		OSX,
		/// <summary>
		/// Supported Linux variant desktop environment.
		/// </summary>
		Linux
	}
}
