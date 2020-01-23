/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly : InternalsVisibleTo("Spectrum")]

namespace Spectrum.Native
{
	internal delegate void NativeLibraryLoadCallback(string libname, bool @new, TimeSpan time);

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

		// Frees a single native library if it is managed by this type
		public static void FreeLibrary(IntPtr ptr)
		{
			if (_LoadedLibraries.Remove(ptr))
				NativeLibrary.Free(ptr);
		}

		// Loads (after extraction, if needed) the library with the given name
		public static IntPtr LoadLibrary(string libname, string linuxname, NativeLibraryLoadCallback cb)
		{
			Stopwatch timer = Stopwatch.StartNew();
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				if (NativeLibrary.TryLoad(linuxname, out var handle))
				{
					_LoadedLibraries.Add(handle);
					cb?.Invoke(linuxname, false, timer.Elapsed);
					return handle;
				}
				throw new DllNotFoundException($"The native library '{linuxname}' could not be loaded.");
			}
			else // OSX and Windows
			{
				bool isWin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
				var respath = $"Spectrum.Native.{libname}.{(isWin ? 'w' : 'm')}";
				var libpath = Path.Combine(LibraryPath, $"{libname}.{(isWin ? "dll" : "so")}");

				// Extract the library (if needed)
				bool @new = false;
				if (!File.Exists(libpath))
				{
					try
					{
						using var rstream = _ThisAssembly.GetManifestResourceStream(respath);
						using var fstream = File.Open(libpath, FileMode.Create, FileAccess.Write, FileShare.None);
						rstream.CopyTo(fstream, 32768);
						@new = true;
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
					cb?.Invoke(libname, @new, timer.Elapsed);
					return handle;
				}
				throw new DllNotFoundException($"The native library '{libname}' could not be loaded.");
			}
		}
	}
}
