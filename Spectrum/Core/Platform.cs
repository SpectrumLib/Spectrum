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
	/// Represents the different .NET frameworks that the library can run on.
	/// </summary>
	public enum PlatformFramework
	{
		Core,
		Framework,
		Native
	}

	/// <summary>
	/// Contains values and checks for working with different operating systems.
	/// </summary>
	public static class Platform
	{
		/// <summary>
		/// The current operating system.
		/// </summary>
		public static readonly PlatformOS OS;
		/// <summary>
		/// The current framework running the code.
		/// </summary>
		public static readonly PlatformFramework Framework;
		/// <summary>
		/// Gets if the current operating system is Microsoft Windows.
		/// </summary>
		public static bool IsWindows => OS == PlatformOS.Windows;
		/// <summary>
		/// Gets if the current operating system is a type of Linux.
		/// </summary>
		public static bool IsLinux => OS == PlatformOS.Linux;
		/// <summary>
		/// Gets if the current operating system is desktop MacOSX.
		/// </summary>
		public static bool IsOSX => OS == PlatformOS.OSX;
		/// <summary>
		/// Gets if the current operating system is a Unix derivative (not Windows).
		/// </summary>
		public static bool IsUnix => OS != PlatformOS.Windows;

		static Platform()
		{
			OS = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PlatformOS.Windows :
				 RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? PlatformOS.OSX :
				 PlatformOS.Linux;

			string sname = RuntimeInformation.FrameworkDescription;
			Framework = sname.Contains("Core") ? PlatformFramework.Core :
						sname.Contains("Framework") ? PlatformFramework.Framework : 
						PlatformFramework.Native;
		}
	}
}
