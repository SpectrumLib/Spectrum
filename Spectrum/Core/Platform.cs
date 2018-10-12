using System;
using System.Runtime.InteropServices;

namespace Spectrum
{
	/// <summary>
	/// Represents the different operating systems that the library can run on.
	/// </summary>
	public enum PlatformOS
	{
		Windows,
		Linux,
		OSX
	}

	/// <summary>
	/// Contains values and checks for working with different operating systems.
	/// </summary>
	public static class Platform
	{
		/// <summary>
		/// The current operating system.
		/// </summary>
		public static readonly PlatformOS Current;
		/// <summary>
		/// Gets if the current operating system is Microsoft Windows.
		/// </summary>
		public static bool IsWindows => Current == PlatformOS.Windows;
		/// <summary>
		/// Gets if the current operating system is a type of Linux.
		/// </summary>
		public static bool IsLinux => Current == PlatformOS.Linux;
		/// <summary>
		/// Gets if the current operating system is desktop MacOSX.
		/// </summary>
		public static bool IsOSX => Current == PlatformOS.OSX;
		/// <summary>
		/// Gets if the current operating system is a Unix derivative (not Windows).
		/// </summary>
		public static bool IsUnix => Current != PlatformOS.Windows;

		static Platform()
		{
			Current = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PlatformOS.Windows :
					  RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? PlatformOS.OSX :
					  PlatformOS.Linux;
		}
	}
}
