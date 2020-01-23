/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;

namespace Spectrum.Audio
{
	// Describes types that stream audio data
	internal interface IAudioStreamer
	{
		// The total number of frames available for streaming
		uint TotalFrames { get; }
		// The number of frames remaining available for streaming
		uint RemainingFrames { get; }
		// The format of the samples available for streaming
		AudioFormat Format { get; }

		// Attempts to stream data into the buffer, returns the actual number of frames read
		uint ReadFrames(Span<byte> data, uint fcount);
		// Resets the streamer to read from the beginning of the stream
		void Reset();
	}
}
