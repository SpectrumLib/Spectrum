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
