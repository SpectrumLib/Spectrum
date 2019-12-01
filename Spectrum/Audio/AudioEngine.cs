/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;

namespace Spectrum.Audio
{
	// Central type for managing the audio subsystem components
	internal static class AudioEngine
	{
		internal static OpenAL OpenAL { get; private set; } = null;

		public static void Initialize()
		{
			OpenAL = new OpenAL();
		}

		public static void Terminate()
		{
			OpenAL.Dispose();
		}
	}
}
