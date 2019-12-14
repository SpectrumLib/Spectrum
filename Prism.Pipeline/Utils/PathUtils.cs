﻿/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.IO;

namespace Prism.Pipeline
{
	/// <summary>
	/// Contains utility functionality for working with filesystem paths.
	/// </summary>
	public static class PathUtils
	{
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
	}
}
