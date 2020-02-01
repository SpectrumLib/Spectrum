/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.IO;
using System.Linq;

namespace Spectrum
{
	/// <summary>
	/// Utility functionality for working with filesystem and network paths.
	/// </summary>
	public static class PathUtils
	{
		private static readonly char[] INVALID_FILE = Path.GetInvalidFileNameChars();
		private static readonly char[] INVALID_PATH = Path.GetInvalidPathChars();

		/// <summary>
		/// Sanitize a file name by removing or replacing invalid characters.
		/// </summary>
		/// <param name="filename">The file name to sanitize.</param>
		/// <param name="options">How to sanitize the file name.</param>
		/// <param name="replace">The replacement character if using <see cref="PathSanitizeOptions.Replace"/>.</param>
		/// <returns>The sanitized file name.</returns>
		public static string SanitizeFileName(string filename, PathSanitizeOptions options = PathSanitizeOptions.Remove, char replace = '_')
		{
			Span<char> sstr = stackalloc char[filename.Length];
			ReadOnlySpan<char> fstr = filename.AsSpan();
			int wi = 0;
			foreach (var ch in fstr)
			{
				if (Array.IndexOf(INVALID_FILE, ch) != -1)
				{
					if (options == PathSanitizeOptions.Replace)
						sstr[wi++] = replace;
				}
				else
					sstr[wi++] = ch;
			}
			return new string(sstr.Slice(0, wi));
		}

		/// <summary>
		/// Sanitize a folder name by removing or replacing invalid characters.
		/// </summary>
		/// <param name="folder">The folder name to sanitize.</param>
		/// <param name="options">How to sanitize the folder name.</param>
		/// <param name="replace">The replacement character if using <see cref="PathSanitizeOptions.Replace"/>.</param>
		/// <returns>The sanitized folder name.</returns>
		public static string SanitizeFolderName(string folder, PathSanitizeOptions options = PathSanitizeOptions.Remove, char replace = '_')
		{
			Span<char> sstr = stackalloc char[folder.Length];
			ReadOnlySpan<char> fstr = folder.AsSpan();
			int wi = 0;
			foreach (var ch in fstr)
			{
				if (Array.IndexOf(INVALID_PATH, ch) != -1)
				{
					if (options == PathSanitizeOptions.Replace)
						sstr[wi++] = replace;
				}
				else
					sstr[wi++] = ch;
			}
			return new string(sstr.Slice(0, wi));
		}

		/// <summary>
		/// Sanitize a filesystem path by removing or replacing invalid characters.
		/// </summary>
		/// <param name="path">The path to sanitize.</param>
		/// <param name="options">How to sanitize the path.</param>
		/// <param name="replace">The replacement character if using <see cref="PathSanitizeOptions.Replace"/>.</param>
		/// <returns>The sanitized path.</returns>
		public static string SanitizePath(string path, PathSanitizeOptions options = PathSanitizeOptions.Remove, char replace = '_') =>
			String.Join(Path.DirectorySeparatorChar,
				path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
					.Select(f => SanitizeFolderName(f, options, replace)).ToArray()
			);

		/// <summary>
		/// Checks if a path is valid, by attempting to load its information and catching operating system errors. This
		/// is a slightly slower (but more accurate) way to check for filesystem path validity.
		/// </summary>
		/// <param name="path">The path to check.</param>
		/// <returns>If the path is valid.</returns>
		public static bool IsValidPath(string path)
		{
			try
			{
				if (String.IsNullOrEmpty(Path.GetExtension(path))) // Directory
					_ = new DirectoryInfo(path);
				else // File
					_ = new FileInfo(path);
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Attempts to load information about a file in an exception-safe manner. This is a safe way to check if a
		/// path is valid.
		/// </summary>
		/// <param name="path">The file to load information for. The file does not have to exist.</param>
		/// <param name="info">The output info for the file.</param>
		/// <returns>If the file info could be retreived successfully.</returns>
		public static bool TryGetFileInfo(string path, out FileInfo info)
		{
			if (String.IsNullOrWhiteSpace(path))
			{
				info = null;
				return false;
			}

			try
			{
				info = new FileInfo(path);
				return true;
			}
			catch
			{
				info = null;
				return false;
			}
		}

		/// <summary>
		/// Attempts to load information about a directory in an exception-safe manner. This is a safe way to check if a
		/// path is valid.
		/// </summary>
		/// <param name="path">The directory to load information for. The directory does not have to exist.</param>
		/// <param name="info">The output info for the directory.</param>
		/// <returns>If the directory info could be retreived successfully.</returns>
		public static bool TryGetDirectoryInfo(string path, out DirectoryInfo info)
		{
			if (String.IsNullOrWhiteSpace(path))
			{
				info = null;
				return false;
			}

			try
			{
				info = new DirectoryInfo(path);
				return true;
			}
			catch
			{
				info = null;
				return false;
			}
		}

		/// <summary>
		/// Attempts to create an absolute path given a source path, and root path. Checks for path validity.
		/// </summary>
		/// <param name="path">The path to make aboslute.</param>
		/// <param name="root">The root path to create relative paths from.</param>
		/// <param name="abspath">The generated absolute path.</param>
		/// <returns>If the path could be created.</returns>
		public static bool TryMakeAbsolutePath(string path, string root, out string abspath)
		{
			abspath = null;
			if (path == null)
				return false;
			root ??= Directory.GetCurrentDirectory();

			// Check if already rooted
			bool abs;
			try
			{
				abs = Path.IsPathRooted(path);
			}
			catch { return false; }

			// Try to generate best approximation of full path
			string fullpath;
			try
			{
				fullpath = abs ? path : Path.Combine(root, path);
			}
			catch { return false; }

			// Try to load the path info for a final check
			try
			{
				FileSystemInfo fsi =
					(fullpath.EndsWith(Path.DirectorySeparatorChar) || fullpath.EndsWith(Path.AltDirectorySeparatorChar))
					? new DirectoryInfo(fullpath) : (FileSystemInfo)(new FileInfo(fullpath));
				abspath = fsi.FullName;
				return true;
			}
			catch { return false; }
		}

		/// <summary>
		/// Attempts to create the directory at the given path, in an exception safe-manner.
		/// </summary>
		/// <param name="path">The path of the directory to create.</param>
		/// <param name="info">The directory info for the resulting path.</param>
		/// <returns>If the directory already existed, or if it was successfully created.</returns>
		public static bool CreateDirectorySafe(string path, out DirectoryInfo info)
		{
			if (!TryGetDirectoryInfo(path, out info))
				return false;

			try
			{
				if (!info.Exists)
					info.Create();
				return true;
			}
			catch
			{
				info = null;
				return false;
			}
		}
	}

	/// <summary>
	/// Gives the options for sanitizing paths and file names.
	/// </summary>
	public enum PathSanitizeOptions
	{
		/// <summary>
		/// Remove invalid path characters.
		/// </summary>
		Remove,
		/// <summary>
		/// Replace invalid path characters with a different character.
		/// </summary>
		Replace
	}
}
