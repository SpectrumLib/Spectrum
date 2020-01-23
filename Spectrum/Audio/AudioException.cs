/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;

namespace Spectrum.Audio
{
    /// <summary>
    /// Base type of exceptions thrown by the audio engine.
    /// </summary>
	public class AudioException : Exception
	{
        internal AudioException(string msg) :
            base(msg)
        { }

        internal AudioException(string msg, Exception inner) :
            base(msg, inner)
        { }
	}

    /// <summary>
	/// Exception thrown when too many audio sources are requested.
	/// </summary>
	public class AudioPlayLimitException : AudioException
    {
        internal AudioPlayLimitException() :
            base($"The maximum number of audio sources ({AudioEngine.MAX_SOURCE_COUNT}) has been reached")
        { }
    }
}
