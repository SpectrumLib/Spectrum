using System;

namespace Spectrum.Audio
{
	// Interface for types that support streaming audio data from the disk
	internal interface IAudioStreamer
	{
		// Stream in some frames
		uint ReadFrames(short[] dst, uint count);

		// Reset the stream to read from the beginning
		void Reset();
	}
}
