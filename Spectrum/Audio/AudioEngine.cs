/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using static Spectrum.InternalLog;

namespace Spectrum.Audio
{
	// Central type for managing the audio subsystem components
	internal static class AudioEngine
	{
		public static OpenAL OpenAL { get; private set; } = null;
		public static IntPtr Device { get; private set; } = IntPtr.Zero;
		public static IntPtr Context { get; private set; } = IntPtr.Zero;

		public static void Initialize()
		{
			OpenAL = new OpenAL();

			var dname = OpenAL.GetAlcString(OpenAL.ALC.DEFAULT_DEVICE_SPECIFIER, IntPtr.Zero);
				
			// Open default audio device
			Device = OpenAL.AlcOpenDevice(dname);
			OpenAL.CheckALCError(IntPtr.Zero, "error in open device");
			if (Device == IntPtr.Zero)
				throw new AudioException("Unable to open default audio playback device.");

			// Create and activate audio context
			Context = OpenAL.AlcCreateContext(Device, new int[2] { 0, 0 }); // Double zero for no special attribs
			OpenAL.CheckALCError(Device, "error in create context");
			if (Context == IntPtr.Zero)
				throw new AudioException("Unable to create context on audio playback device.");
			OpenAL.AlcMakeContextCurrent(Context);
			OpenAL.CheckALCError(Device, "error in make context current");

			IINFO($"Started OpenAL audio engine (device: {dname}).");
		}

		public static void Terminate()
		{
			// Destroy the context, and close device
			OpenAL.AlcMakeContextCurrent(IntPtr.Zero);
			OpenAL.CheckALCError(Device, "deactivate context");
			OpenAL.AlcDestroyContext(Context);
			OpenAL.CheckALCError(Device, "destroy context");
			Context = IntPtr.Zero;
			OpenAL.AlcCloseDevice(Device);
			Device = IntPtr.Zero;

			OpenAL.Dispose();

			IINFO("Terminated OpenAL audio engine.");
		}
	}
}
