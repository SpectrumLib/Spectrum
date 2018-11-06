using System;
using OpenAL;
using static Spectrum.InternalLog;

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
		// The OpenAL context
		internal static IntPtr Context { get; private set; } = IntPtr.Zero;
		#endregion // Fields

		public static void Initialize()
		{
			// Populate the device lists
			PlaybackDevice.PopulateDeviceList();
			if (PlaybackDevice.Devices.Count == 0)
				throw new AudioException("There are no audio playback devices available");

			// Open the default playback device
			Device = ALC10.alcOpenDevice(PlaybackDevice.Devices[0].Identifier);
			ALUtils.CheckALCError();
			if (Device == IntPtr.Zero)
				throw new AudioException("Unable to open default audio playback device");

			// Create the al context and set it as active
			Context = ALC10.alcCreateContext(Device, new int[2] { 0, 0 }); // Two 0s tell OpenAL no special attribs
			ALUtils.CheckALCError();
			if (Context == IntPtr.Zero)
				throw new AudioException("Unable to create audio context");
			ALC10.alcMakeContextCurrent(Context);
			ALUtils.CheckALCError();

			// Report
			LINFO("Started OpenAL audio engine.");
		}

		public static void Shutdown()
		{
			// Destroy the context, and then close the device
			ALC10.alcMakeContextCurrent(IntPtr.Zero);
			ALUtils.CheckALCError();
			ALC10.alcDestroyContext(Context);
			ALUtils.CheckALCError();
			Context = IntPtr.Zero;
			ALC10.alcCloseDevice(Device);
			ALUtils.CheckALCError();
			Device = IntPtr.Zero;

			// Report
			LINFO("Shutdown OpenAL audio engine.");
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
