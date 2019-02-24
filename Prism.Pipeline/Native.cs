using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Prism")]

namespace Prism
{
	// Controls the extracting, loading, and disposable of embedded native libraries
	internal static class Native
	{
		#region Fields
		private static readonly PlatformOS s_platform;
		private static readonly Assembly s_this;
		private static readonly string s_thisDir;

		private static readonly Dictionary<string, IntPtr> s_loadedLibs;
		private static readonly List<string> s_libPaths;
		#endregion // Fields

		static Native()
		{
			s_platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PlatformOS.Windows :
						 RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? PlatformOS.OSX : PlatformOS.Linux;
			s_this = Assembly.GetAssembly(typeof(Native));
			s_thisDir = Path.GetDirectoryName(s_this.Location);
			s_loadedLibs = new Dictionary<string, IntPtr>();
			s_libPaths = new List<string>();

			AppDomain.CurrentDomain.ProcessExit += (sender, e) => { Release(); };

			// Load the libraries
			ExtractAndLoad("image");
			ExtractAndLoad("audio");
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

		// Called when the process exits
		private static void Release()
		{
			// Release the handles
			foreach (var pair in s_loadedLibs)
			{
				if (s_platform == PlatformOS.Windows)
				{
					// Twice is needed
					Kernel32.FreeLibrary(pair.Value);
					Kernel32.FreeLibrary(pair.Value);
				}
				else
				{
					// Twice is needed
					Dl.Close(pair.Value);
					Dl.Close(pair.Value);
				}
			}

			// Delete the extracted files
			foreach (var path in s_libPaths)
				File.Delete(path);
		}

		// Extract the resource into the path and load it
		private static void ExtractAndLoad(string name)
		{
			var rName = GetResourceName(name);
			var rPath = Path.Combine(s_thisDir, $"{name}.nl");

			// Extract
			using (var reader = s_this.GetManifestResourceStream(rName))
			{
				using (var writer = File.Open(rPath, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					reader.CopyTo(writer);
				}
			}
			s_libPaths.Add(rPath);

			// Load
			var handle = (s_platform == PlatformOS.Windows) ? Kernel32.LoadLibrary(rPath) : Dl.Open(rPath, Dl.RTLD_NOW);
			s_loadedLibs.Add(name, handle);
		}

		// Appends the platform extension for the embedded resource
		private static string GetResourceName(string baseName) =>
			"Prism.Native." + baseName + (s_platform == PlatformOS.Windows ? ".w" : s_platform == PlatformOS.OSX ? ".m" : ".l");

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
