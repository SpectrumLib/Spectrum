using System;

namespace Spectrum.Audio
{
	// Interface for types that support streaming audio data from the disk
	internal interface IAudioStreamer
	{
		// The total number of frames available 
		uint FrameCount { get; }

		// If the stream is stereo data
		bool Stereo { get; }

		// The number of frames available to stream
		uint RemainingFrames { get; }

		// Stream in some frames
		uint ReadFrames(short[] dst, uint count);

		// Reset the stream to read from the beginning
		void Reset();
	}
}
