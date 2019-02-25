using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Prism.Builtin
{
	// Interface to the native audio loading code
	internal static class NativeAudio
	{
		#region Delegates
		private static readonly Delegates.stl_c_free stl_c_free;
		private static readonly Delegates.stl_c_alloc stl_c_alloc;
		private static readonly Delegates.stb_vorbis_decode_filename stb_vorbis_decode_filename;
		private static readonly Delegates.drwav_open_file_and_read_pcm_frames_s16 drwav_open_file_and_read_pcm_frames_s16;
		private static readonly Delegates.drwav_free drwav_free;
		private static readonly Delegates.drflac_open_file_and_read_pcm_frames_s16 drflac_open_file_and_read_pcm_frames_s16;
		private static readonly Delegates.drflac_free drflac_free;
		private static readonly Delegates.drmp3_open_file_and_read_f32 drmp3_open_file_and_read_f32;
		private static readonly Delegates.drmp3_free drmp3_free;
		#endregion // Delegates

		static NativeAudio()
		{
			var handle = Native.GetLibraryHandle("audio");
			stl_c_free = Native.LoadFunction<Delegates.stl_c_free>(handle, nameof(stl_c_free));
			stl_c_alloc = Native.LoadFunction<Delegates.stl_c_alloc>(handle, nameof(stl_c_alloc));
			stb_vorbis_decode_filename = Native.LoadFunction<Delegates.stb_vorbis_decode_filename>(handle, nameof(stb_vorbis_decode_filename));
			drwav_open_file_and_read_pcm_frames_s16 = Native.LoadFunction<Delegates.drwav_open_file_and_read_pcm_frames_s16>(handle, nameof(drwav_open_file_and_read_pcm_frames_s16));
			drwav_free = Native.LoadFunction<Delegates.drwav_free>(handle, nameof(drwav_free));
			drflac_open_file_and_read_pcm_frames_s16 = Native.LoadFunction<Delegates.drflac_open_file_and_read_pcm_frames_s16>(handle, nameof(drflac_open_file_and_read_pcm_frames_s16));
			drflac_free = Native.LoadFunction<Delegates.drflac_free>(handle, nameof(drflac_free));
			drmp3_open_file_and_read_f32 = Native.LoadFunction<Delegates.drmp3_open_file_and_read_f32>(handle, nameof(drmp3_open_file_and_read_f32));
			drmp3_free = Native.LoadFunction<Delegates.drmp3_free>(handle, nameof(drmp3_free));
		}

		// Standard free() function, for use with stb_vorbis, and allocated image data with AllocData()
		public static void Free(IntPtr mem) => stl_c_free(mem);

		// Free function for dr_wav
		public static void FreeWav(IntPtr mem) => drwav_free(mem);

		// Free function for dr_flac
		public static void FreeFlac(IntPtr mem) => drflac_free(mem);

		// Free function for dr_mp3
		public static void FreeMp3(IntPtr mem) => drmp3_free(mem);

		// Unmanaged memory allocation
		public unsafe static byte* AllocData(ulong size) => (byte*)stl_c_alloc(size).ToPointer();

		// Load raw audio data from a wave file
		public static RawAudio LoadWave(string path)
		{
			IntPtr data = drwav_open_file_and_read_pcm_frames_s16(path, out int channels, out int rate, out ulong frames);
			try
			{
				if (data == IntPtr.Zero || frames == 0)
					throw new ArgumentException("The file could not be opened, or was not a valid WAV file.", nameof(path));
				if (frames > UInt32.MaxValue)
					throw new InvalidOperationException("The audio file was too long.");
				if (channels > 2)
					throw new InvalidOperationException("Cannot process data that has >2 channels.");
			}
			catch
			{
				FreeWav(data);
				throw;
			}

			return new RawAudio(AudioFormat.Wav, (uint)frames, channels == 2, (uint)rate, data);
		}

		// Load raw audio data from an ogg vorbis file
		public static RawAudio LoadVorbis(string path)
		{
			var samples = stb_vorbis_decode_filename(path, out int channels, out int rate, out IntPtr data);
			try
			{
				if (samples == -1)
					throw new ArgumentException("The file could not be opened, or was not a valid OGG Vorbis file.", nameof(path));
				if (channels > 2)
					throw new InvalidOperationException("Cannot process data that has >2 channels.");
			}
			catch
			{
				Free(data);
				throw;
			}

			return new RawAudio(AudioFormat.Ogg, (uint)(samples / channels), channels == 2, (uint)rate, data);
		}

		// Load raw audio data from a flac file
		public static RawAudio LoadFlac(string path)
		{
			IntPtr data = drflac_open_file_and_read_pcm_frames_s16(path, out int channels, out int rate, out ulong frames);
			try
			{
				if (data == IntPtr.Zero || frames == 0)
					throw new ArgumentException("The file could not be opened, or was not a valid WAV file.", nameof(path));
				if (frames > UInt32.MaxValue)
					throw new InvalidOperationException("The audio file was too long.");
				if (channels > 2)
					throw new InvalidOperationException("Cannot process data that has >2 channels.");
			}
			catch
			{
				FreeFlac(data);
				throw;
			}

			return new RawAudio(AudioFormat.Flac, (uint)frames, channels == 2, (uint)rate, data);
		}

		// Load raw audio data from a mp3 file
		public static RawAudio LoadMp3(string path)
		{
			IntPtr data = drmp3_open_file_and_read_f32(path, out drmp3_config config, out ulong frames);
			try
			{
				if (data == IntPtr.Zero || frames == 0)
					throw new ArgumentException("The file could not be opened, or was not a valid WAV file.", nameof(path));
				if (frames > UInt32.MaxValue)
					throw new InvalidOperationException("The audio file was too long.");
				if (config.Channels > 2)
					throw new InvalidOperationException("Cannot process data that has >2 channels.");
			}
			catch
			{
				FreeMp3(data);
				throw;
			}

			return new RawAudio(AudioFormat.Mp3, (uint)frames, config.Channels == 2, config.SampleRate, data);
		}
		
		// Unmanaged delegate types
		private static class Delegates
		{
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void stl_c_free(IntPtr mem);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr stl_c_alloc(ulong size);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int stb_vorbis_decode_filename(string filename, out int channels, out int rate, out IntPtr output);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr drwav_open_file_and_read_pcm_frames_s16(string filename, out int channels, out int rate, out ulong frames);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void drwav_free(IntPtr mem);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr drflac_open_file_and_read_pcm_frames_s16(string filename, out int channels, out int rate, out ulong frames);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void drflac_free(IntPtr mem);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr drmp3_open_file_and_read_f32(string filePath, out drmp3_config config, out ulong frames);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void drmp3_free(IntPtr mem);
		}

		// Config type (must remain 1:1 mapping with the unmanaged type)
		[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 8)]
		private struct drmp3_config
		{
			public uint Channels;
			public uint SampleRate;
		}
	}
}
