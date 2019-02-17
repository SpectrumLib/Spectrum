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
}
