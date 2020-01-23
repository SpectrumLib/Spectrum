/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.InteropServices;

namespace Spectrum.Audio
{
    // Wraps an OpenAL sample buffer
	internal class AudioBuffer : IDisposable
	{
		#region Fields
		private uint _handle = 0;
		public uint Handle => _handle;

		public AudioFormat Format { get; private set; }
		public uint DataSize { get; private set; }
		public TimeSpan Duration { get; private set; }
		#endregion // Fields

		public AudioBuffer()
		{
			AudioEngine.OpenAL.GenBuffers(1, out _handle);
			AudioEngine.OpenAL.CheckALError("generate buffer");
			if (_handle == 0)
				throw new AudioException("Failed to generate audio buffer.");
		}
		~AudioBuffer()
		{
			dispose(false);
		}

		public unsafe void SetData<T>(ReadOnlySpan<T> data, AudioFormat fmt, uint hz)
			where T : struct
		{
			if (_handle == 0)
				throw new AudioException("Cannot set data in unallocated buffer.");
			var oal = AudioEngine.OpenAL;

			var bytes = MemoryMarshal.AsBytes(data);
			fixed (void* ptr = bytes)
			{
				oal.BufferData(_handle, (int)fmt, new IntPtr(ptr), bytes.Length, (int)hz);
				oal.CheckALError("buffer set data");
			}

			oal.GetBufferi(_handle, OpenAL.AL.BITS, out var bits);
			oal.GetBufferi(_handle, OpenAL.AL.CHANNELS, out var channels);
			oal.GetBufferi(_handle, OpenAL.AL.SIZE, out var size);

			Format = fmt;
			DataSize = (uint)size;
			Duration = TimeSpan.FromSeconds((double)(size / ((bits / 8) * channels)) / hz);
		}

		#region Disposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (_handle != 0 && AudioEngine.IsRunning)
			{
				AudioEngine.OpenAL.DeleteBuffers(1, out _handle);
				AudioEngine.OpenAL.CheckALError("delete buffer");
			}
			_handle = 0;
		}
		#endregion // Disposable
	}
}
