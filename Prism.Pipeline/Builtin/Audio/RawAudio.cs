using System;

namespace Prism.Builtin
{
	// Holds information about a raw chunk of PCM data (of varying formats)
	internal class RawAudio
	{
		#region Fields
		public readonly AudioFormat Format;
		public readonly uint FrameCount;
		public readonly uint ChannelCount;
		public readonly uint Rate;
		public uint SampleCount => FrameCount * ChannelCount;
		// The size of a single sample
		public uint SampleSize => (Format == AudioFormat.Mp3) ? 4u : 2u;
		// Size of the data (in bytes)
		public ulong DataLength => SampleCount * SampleSize;

		public readonly IntPtr Data; // The data in unmanaged memory
		#endregion // Fields

		public RawAudio(AudioFormat format, uint fc, uint cc, uint r, IntPtr data)
		{
			Format = format;
			FrameCount = fc;
			ChannelCount = cc;
			Rate = r;
			Data = data;
		}
	}

	public enum AudioFormat
	{
		Wav,  // Data is interleaved s16
		Ogg,  // Data is interleaved s16
		Flac, // Data is interleaved s16
		Mp3   // Data is interleaved f32
	}
}
