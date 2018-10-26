using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly:InternalsVisibleTo("Spectrum")]

namespace Spectrum
{
	// Performs extraction, loading, and unloading of the native libraries contained as embedded resources
	// Embedded resources should be named <name>.<platform>, where <platform> is one of `w`, `m`, or `l`.
	internal static class NativeLoader
	{
		#region Fields
		private static readonly PlatformOS s_platform;
		private static readonly Assembly s_this;
		private static readonly string s_thisDir;
		// List of available embedded resources
		private static readonly List<string> s_resourceList;
		public static IReadOnlyList<string> AvailableResources => s_resourceList;

		private static readonly Dictionary<string, IntPtr> s_loadedLibs = new Dictionary<string, IntPtr>();
		private static readonly Dictionary<string, string> s_libPaths = new Dictionary<string, string>();

		// The last load time
		public static TimeSpan LastLoadTime { get; private set; } = TimeSpan.Zero;
		#endregion // Fields

		static NativeLoader()
		{
			s_platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PlatformOS.Windows :
						 RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? PlatformOS.OSX : PlatformOS.Linux;
			s_this = Assembly.GetAssembly(typeof(NativeLoader));
			s_thisDir = Path.GetDirectoryName(s_this.Location);
			s_resourceList = new List<string>(s_this.GetManifestResourceNames());
		}

		// Extracts and loads a resource with the base name into the same directory with the base output name
		public static void LoadUnmanagedLibrary(string resourceBaseName, string outFile)
		{
			if (s_loadedLibs.ContainsKey(resourceBaseName))
				return;
			string resName = GetResourceName(resourceBaseName);
			if (!s_resourceList.Contains(resName))
				throw new Exception($"The native library {resourceBaseName} does not exist as an embedded resource.");

			Stopwatch timer = Stopwatch.StartNew();

			string outPath = Path.Combine(s_thisDir, outFile);
			try
			{
				WriteResourceStream(resName, outPath);
			}
			catch (Exception)
			{
				throw;
			}

			IntPtr handle = IntPtr.Zero;
			if (s_platform == PlatformOS.Windows)
			{
				handle = Kernel32.LoadLibrary(outPath);
				if (handle == IntPtr.Zero)
				{
					int err = Marshal.GetLastWin32Error();
					string errstr = (new Win32Exception(err)).Message;
					throw new Exception(errstr);
				}
			}
			else
			{
				handle = Dl.Open(outPath, Dl.RTLD_NOW);
				if (handle == IntPtr.Zero)
				{
					IntPtr err = Dl.Error();
					string errstr = Marshal.PtrToStringAuto(err);
					throw new Exception(errstr);
				}
			}

			if (s_loadedLibs.Count == 0)
			{
				// Unloads the libraries in the event of a crash (or the user forgets)
				AppDomain.CurrentDomain.ProcessExit += (sender, e) => UnloadLibraries();
			}

			s_loadedLibs.Add(resourceBaseName, handle);
			s_libPaths.Add(resourceBaseName, outPath);

			LastLoadTime = timer.Elapsed;
		}

		// Unloads all of the loaded libraries
		// Note that each library needs to be freed twice, since their use counter is increased by 2 byt the use of
		//   both the manual loader, as well as DllImport
		public static void UnloadLibraries()
		{
			if (s_loadedLibs.Count == 0)
				return;

			if (s_platform == PlatformOS.Windows)
			{
				foreach (var pair in s_loadedLibs)
				{
					Kernel32.FreeLibrary(pair.Value);
					Kernel32.FreeLibrary(pair.Value);
				}
			}
			else
			{
				foreach (var pair in s_loadedLibs)
				{
					Dl.Close(pair.Value);
					Dl.Close(pair.Value);
				}
			}

			foreach (var pair in s_libPaths)
			{
				File.Delete(pair.Value);
			}

			s_loadedLibs.Clear();
			s_libPaths.Clear();
		}

		// Gets the handle of the loaded library
		public static IntPtr GetLibraryHandle(string lib) => s_loadedLibs[lib];

		// Loads a function from the passed C library handle (see GetLibraryHandle())
		public static T LoadFunction<T>(IntPtr library, string function)
			where T : Delegate
		{
			IntPtr handle = (s_platform == PlatformOS.Windows) ? Kernel32.GetProcAddress(library, function) : Dl.Symbol(library, function);
			return (handle == IntPtr.Zero) ? null : Marshal.GetDelegateForFunctionPointer(handle, typeof(T)) as T;
		}

		// Appends the platform extension for the embedded resource
		private static string GetResourceName(string baseName) =>
			"Spectrum.Native." + baseName + (s_platform == PlatformOS.Windows ? ".w" : s_platform == PlatformOS.OSX ? ".m" : ".l");

		// Copies the embedded resource into the filesystem
		private static void WriteResourceStream(string resBase, string outPath)
		{
			outPath = Path.Combine(s_thisDir, outPath);

			if (!File.Exists(outPath))
			{
				using (BinaryReader reader = new BinaryReader(s_this.GetManifestResourceStream(resBase)))
				{
					using (BinaryWriter writer = new BinaryWriter(File.Open(outPath, FileMode.Create, FileAccess.Write, FileShare.None)))
					{
						byte[] buffer = new byte[8192];
						int read = 0;
						while ((read = reader.Read(buffer, 0, 8192)) > 0)
						{
							writer.Write(buffer, 0, read);
						}
					}
				}
			}

			File.SetAttributes(outPath, FileAttributes.Normal);
		}

		// Native loader methods for windows
		private static class Kernel32
		{
			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern IntPtr LoadLibrary(string lpFileName);
			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern int FreeLibrary(IntPtr hModule);
			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
		}

		// Native loader methods for *nix
		private static class Dl
		{
			[DllImport("libdl.so", EntryPoint = "dlopen")]
			public static extern IntPtr Open(string fileName, int flags);
			[DllImport("libdl.so", EntryPoint = "dlclose")]
			public static extern int Close(IntPtr handle);
			[DllImport("libdl.so", EntryPoint = "dlerror")]
			public static extern IntPtr Error();
			[DllImport("libdl.so", EntryPoint = "dlsym")]
			public static extern IntPtr Symbol(IntPtr handle, string symbol);
			public const int RTLD_NOW = 2;
		}

		// Platform identifiers
		private enum PlatformOS
		{
			Windows,
			Linux,
			OSX
		}
	}
}
