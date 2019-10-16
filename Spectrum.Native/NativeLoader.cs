using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly : InternalsVisibleTo("Spectrum")]

namespace Spectrum.Native
{
	internal static class NativeLoader
	{
		#region Fields
		internal static readonly string LibraryPath;

		private static readonly Assembly _ThisAssembly;
		private static readonly List<IntPtr> _LoadedLibraries;
		#endregion // Fields

		static NativeLoader()
		{
			_ThisAssembly = Assembly.GetExecutingAssembly();
			_LoadedLibraries = new List<IntPtr>();

			// Calculate the extraction path for the libraries
			var tdir = Path.GetTempPath();
			var adir = $"Spectrum.{_ThisAssembly.GetName().Version}";
			LibraryPath = Path.Combine(tdir, adir);
			if (!Directory.Exists(LibraryPath))
				Directory.CreateDirectory(LibraryPath);
		}

		// Frees all native libraries loaded through this class
		public static void FreeAll()
		{
			_LoadedLibraries.ForEach(lib => NativeLibrary.Free(lib));
			_LoadedLibraries.Clear();
		}

		// Loads (after extraction, if needed) the library with the given name
		public static IntPtr LoadLibrary(string libname)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				var fname = $"lib{libname}.so";
				if (NativeLibrary.TryLoad(fname, out var handle))
				{
					_LoadedLibraries.Add(handle);
					return handle;
				}
				throw new DllNotFoundException($"The native library '{libname}' could not be loaded.");
			}
			else // OSX and Windows
			{
				bool isWin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
				var respath = $"Spectrum.Native.{libname}.{(isWin ? 'w' : 'm')}";
				var libpath = Path.Combine(LibraryPath, $"{libname}.{(isWin ? "dll" : "so")}");

				// Extract the library (if needed)
				if (!File.Exists(libpath))
				{
					try
					{
						using var rstream = _ThisAssembly.GetManifestResourceStream(respath);
						using var fstream = File.Open(libpath, FileMode.Create, FileAccess.Write, FileShare.None);
						rstream.CopyTo(fstream, 32768);
					}
					catch (Exception ex)
					{
						throw new DllNotFoundException($"The native library '{libname}' could not be extracted.", ex);
					}
				}

				// Try to load
				if (NativeLibrary.TryLoad(libpath, out var handle))
				{
					_LoadedLibraries.Add(handle);
					return handle;
				}
				throw new DllNotFoundException($"The native library '{libname}' could not be loaded.");
			}
		}
	}
}
