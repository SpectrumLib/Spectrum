/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.IO;
using System.Reflection;

namespace Prism.Pipeline
{
	/// <summary>
	/// Manages the generation of new default Prism-related files.
	/// </summary>
	public static class PrismFileGenerator
	{
		/// <summary>
		/// Creates a new default file of the given type.
		/// </summary>
		/// <param name="type">The type of file to generate.</param>
		/// <param name="path">The path to generate the file at.</param>
		/// <returns>The full path to the file that was generated.</returns>
		public static string GenerateFile(GeneratedFileType type, string path)
		{
			// Get the resource name
			var resName = type switch { 
				GeneratedFileType.Project => "Default.prism",
				_ => null
			};
			if (resName is null)
				throw new ArgumentException($"Invalid file type {(int)type}", nameof(type));

			// Check file info
			if (!PathUtils.TryGetFileInfo(path, out var fileinfo))
				throw new IOException($"Invalid file path '{path}'");
			if (fileinfo.Exists)
				throw new ArgumentException($"The file '{fileinfo.FullName}' already exists");

			// Write the resource to the file
			try
			{
				using var resource = 
					Assembly.GetExecutingAssembly().GetManifestResourceStream($"Prism.Pipeline.Resources.{resName}");
				using var fileout = fileinfo.OpenWrite();
				resource.CopyTo(fileout, 8192);
				return fileinfo.FullName;
			}
			catch (Exception e)
			{
				throw new Exception($"Unable to generate file, reason: {e.Message}", e);
			}
		}
	}

	/// <summary>
	/// The file types that can be generated with the <see cref="PrismFileGenerator"/> class.
	/// </summary>
	public enum GeneratedFileType
	{
		/// <summary>
		/// A default empty Prism project file.
		/// </summary>
		Project
	}
}
