using System;

namespace Spectrum.Audio
{
	// Controller for the OpenAL context and other objects, and the lifetime of the audio system
	internal static class AudioEngine
	{
		// Reasonable limit, since OpenAL Soft *technically* supports infinite sources as a software implementation
		// Nobody should need anything higher than this, and too many sources would slow it down
		// We can do testing in the future to get a better estimate for what this value should be
		public const int MAX_SOURCE_COUNT = 24;

		#region Fields
		// The OpenAL device handle
		public static IntPtr Device { get; private set; } = IntPtr.Zero;
		#endregion // Fields

		public static void Initialize()
		{

		}

		public static void Shutdown()
		{

		}
	}

	/// <summary>
	/// Exception thrown when the audio engine encounters an unrecoverable error.
	/// </summary>
	public class AudioException : Exception
	{
		internal AudioException(string message) :
			base(message)
		{ }

		internal AudioException(string message, Exception innerException) :
			base(message, innerException)
		{ }
	}
}
