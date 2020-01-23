/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;

namespace Spectrum.Content
{
	/// <summary>
	/// Represents an exception that occurs in the process of loading or managing content files.
	/// </summary>
	public class ContentException : Exception
	{
		internal ContentException(string message) :
			base(message)
		{ }

		internal ContentException(string message, Exception inner) :
			base(message, inner)
		{ }
	}

	/// <summary>
	/// Represents an exception that occurs while attempting to load a specific content item.
	/// </summary>
	public class ContentLoadException : ContentException
	{
		/// <summary>
		/// The name of the content item that generated the exception.
		/// </summary>
		public readonly string Item;

		internal ContentLoadException(string item, string message) :
			base($"Content item '{item}' error: {message}")
		{
			Item = item;
		}

		internal ContentLoadException(string item, string message, Exception inner) :
			base($"Content item '{item}' error: {message}", inner)
		{
			Item = item;
		}
	}
}
