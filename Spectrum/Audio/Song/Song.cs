using System;
using Spectrum.Content;

namespace Spectrum.Audio
{
	/// <summary>
	/// Represents a source of audio data that is streamed from the disk. Unlike <see cref="SoundEffect"/>, this type
	/// streams data from the disk in chunks, instead of loading it all into memory at once.
	/// </summary>
	public sealed class Song : IDisposableContent, IAudioSource
	{
		private const uint BUFF_SIZE = 524_288; // 1MB buffers for audio (~11.9 seconds at mono 44100 Hz)

		#region Fields
		// The data stream
		private readonly FSRStream _stream;

		// The intermediate buffer to load samples into before passing them to OpenAL
		internal short[] SampleBuffer { get; private set; }

		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		internal Song(FSRStream stream)
		{
			_stream = stream;

			SampleBuffer = new short[BUFF_SIZE * (_stream.Stereo ? 2u : 1u)];
		}
		~Song()
		{
			dispose(false);
		}

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed && disposing)
			{
				_stream.Dispose();
			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
