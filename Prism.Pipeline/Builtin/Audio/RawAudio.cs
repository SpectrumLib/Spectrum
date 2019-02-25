using System;

namespace Prism.Builtin
{
	// Holds information about a raw chunk of PCM data (of varying formats)
	internal class RawAudio : IDisposable
	{
		#region Fields
		public readonly AudioFormat Format;
		public readonly uint FrameCount;
		public readonly bool Stereo;
		public readonly uint Rate;
		public uint SampleCount => FrameCount * (Stereo ? 2u : 1u);
		// The size of a single sample
		public uint SampleSize => (Format == AudioFormat.Mp3) ? 4u : 2u;
		// Size of the data (in bytes)
		public ulong DataLength => SampleCount * SampleSize;

		public IntPtr Data { get; private set; } // The data in unmanaged memory

		private bool _isDisposed = false;
		#endregion // Fields

		public RawAudio(AudioFormat format, uint fc, bool s, uint r, IntPtr data)
		{
			Format = format;
			FrameCount = fc;
			Stereo = s;
			Rate = r;
			Data = data;
		}
		~RawAudio()
		{
			Dispose();
		}

		// Moves ownership of data to a processed data type, and this type no longer needs to dispose the data
		public IntPtr TakeData()
		{
			var tmp = Data;
			Data = IntPtr.Zero;
			return tmp;
		}

		public void Dispose()
		{
			if (!_isDisposed && (Data != IntPtr.Zero))
			{
				switch (Format)
				{
					case AudioFormat.Wav: NativeAudio.FreeWav(Data); break;
					case AudioFormat.Ogg: NativeAudio.Free(Data); break;
					case AudioFormat.Flac: NativeAudio.FreeFlac(Data); break;
					case AudioFormat.Mp3: NativeAudio.FreeMp3(Data); break;
				}
			}
			_isDisposed = true;
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
