using System;
using System.Runtime.InteropServices;
using OpenAL;

namespace Spectrum.Audio
{
	// Wrapper around an OpenAL buffer containing audio data in memory
	internal class SoundBuffer : IDisposable
	{
		#region Fields
		private uint _handle = 0;
		public uint Handle => _handle;

		public AudioFormat Format { get; private set; }
		public uint DataSize { get; private set; } // In bytes
		public TimeSpan Duration { get; private set; }

		private bool _isDisposed = false;
		#endregion // Fields

		public SoundBuffer()
		{
			AL10.alGenBuffers(1, out _handle);
			ALUtils.CheckALError("unable to create sound buffer");
			if (_handle == 0)
				throw new AudioException("Unable to create OpenAL sound buffer");
		}
		~SoundBuffer()
		{
			dispose(false);
		}

		public void SetData<T>(T[] data, AudioFormat fmt, uint hz, uint start, uint size)
			where T : struct
		{
			if (_handle == 0)
				throw new InvalidOperationException("Cannot upload data to a buffer that doesn't exist");
			if ((start + size) > data.Length)
				throw new AudioException("The passed data was not large enough to supply the requested amount of data");

			uint typeSize = (uint)Marshal.SizeOf<T>();
			uint dataSize = size * typeSize;

			var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
			try
			{
				SetData(handle.AddrOfPinnedObject() + (int)(start * typeSize), fmt, hz, dataSize);
			}
			finally
			{
				handle.Free();
			}
		}

		internal void SetData(IntPtr data, AudioFormat fmt, uint hz, uint size)
		{
			AL10.alBufferData(_handle, (int)fmt, data, (int)size, (int)hz);
			ALUtils.CheckALError("unable to set audio buffer data");

			AL10.alGetBufferi(_handle, AL10.AL_BITS, out int bits);
			AL10.alGetBufferi(_handle, AL10.AL_CHANNELS, out int channels);
			AL10.alGetBufferi(_handle, AL10.AL_SIZE, out int unpackedSize);

			Format = fmt;
			DataSize = (uint)unpackedSize;
			Duration = TimeSpan.FromSeconds((double)(unpackedSize / ((bits / 8) * channels)) / hz);
		}

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (_handle != 0 && !AudioEngine.IsShutdown)
				{
					AL10.alDeleteBuffers(1, ref _handle);
					ALUtils.CheckALError("unable to destroy sound buffer");
					_handle = 0;
				}
				_isDisposed = true;
			}
		}
		#endregion // IDisposable
	}
}
