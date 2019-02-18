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
	public class ContentLoadException : Exception
	{
		/// <summary>
		/// The name of the content item that generated the exception while being loaded.
		/// </summary>
		public readonly string Item;

		internal ContentLoadException(string item, string message) :
			base($"Content item '{item}' exception: {message}")
		{
			Item = item;
		}

		internal ContentLoadException(string item, string message, Exception inner) :
			base($"Content item '{item}' exception: {message}", inner)
		{
			Item = item;
		}
	}
}
