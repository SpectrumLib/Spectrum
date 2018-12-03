using System;
using System.IO;
using System.Runtime.InteropServices;
using static Spectrum.InternalLog;

namespace Spectrum.Audio
{
	// This class interfaces with the audio loading library, and is meant to perform all audio file input processing.
	internal static class AudioLoader
	{

		#region SoundEffect
		// Create a sound buffer from a WAV encoded file
		public static SoundBuffer LoadWaveFile(string path)
		{
			if (!File.Exists(path))
				throw new FileNotFoundException($"Audio file does not exist: '{path}'");

			IntPtr samples = DrWav.open_and_read_file_s16(path, out uint channels, out uint sampleRate, out ulong sampleCount);
			if (samples == IntPtr.Zero)
			{
				throw new Exception($"Unable to load audio file '{path}'");
			}

			if (channels < 1 || channels > 2)
			{
				throw new Exception($"Unable to load audio file, incorrect number of channels ({channels}) ('{path}')");
			}
			if (((int)sampleCount / channels) > (sampleRate * 10))
			{
				LDEBUG($"Audio file '{path}' is long for a SoundEffect, consider using a Song.");
			}

			var sb = new SoundBuffer();
			sb.SetData(samples, (channels == 1) ? AudioFormat.Mono16 : AudioFormat.Stereo16, sampleRate, (uint)(sampleCount * 2));

			DrWav.free(samples);
			return sb;
		}

		// Create a sound buffer from an OGG Vorbis encoded file
		public static SoundBuffer LoadVorbisFile(string path)
		{
			if (!File.Exists(path))
				throw new FileNotFoundException($"Audio file does not exist: '{path}'");

			int read = StbVorbis.decode_filename(path, out int channels, out int sample_rate, out IntPtr output) * 2;
			if (read < 0)
			{
				throw new Exception($"Unable to load audio file '{path}'");
			}

			if (channels < 1 || channels > 2)
			{
				throw new Exception($"Unable to load audio file, incorrect number of channels ({channels}) ('{path}')");
			}
			if ((read / channels) > (sample_rate * 10))
			{
				LDEBUG($"Audio file '{path}' is long for a SoundEffect, consider using a Song.");
			}

			var sb = new SoundBuffer();
			short[] data = new short[read];
			Marshal.Copy(output, data, 0, read);
			sb.SetData(data, channels == 1 ? AudioFormat.Mono16 : AudioFormat.Stereo16, (uint)sample_rate, 0, (uint)read);

			StbVorbis.stl_c_free(output);
			return sb;
		}

		// Create a sound buffer from a FLAC encoded file
		public static SoundBuffer LoadFlacFile(string path)
		{
			if (!File.Exists(path))
				throw new FileNotFoundException($"Audio file does not exist: '{path}'");

			IntPtr samples = DrFlac.open_and_decode_file_s16(path, out uint channels, out uint sampleRate, out ulong sampleCount);
			if (samples == IntPtr.Zero)
			{
				throw new Exception($"Unable to load audio file '{path}'");
			}

			if (channels < 1 || channels > 2)
			{
				throw new Exception($"Unable to load audio file, incorrect number of channels ({channels}) ('{path}')");
			}
			if (((int)sampleCount / channels) > (sampleRate * 10))
			{
				LDEBUG($"Audio file '{path}' is long for a SoundEffect, consider using a Song.");
			}

			var sb = new SoundBuffer();
			sb.SetData(samples, (channels == 1) ? AudioFormat.Mono16 : AudioFormat.Stereo16, sampleRate, (uint)(sampleCount * 2));

			DrFlac.free(samples);
			return sb;
		}
		#endregion // SoundEffect

		// Interface for dr_wav.h code
		private static class DrWav
		{
			[DllImport("audio.dll", EntryPoint = "drwav_open_and_read_file_s16")]
			public static extern IntPtr open_and_read_file_s16(string path, out uint channels, out uint sampleRate, out ulong sampleCount);

			[DllImport("audio.dll", EntryPoint = "drwav_free")]
			public static extern void free(IntPtr data);
		}

		// Interface for dr_flac.h code
		private static class DrFlac
		{
			[DllImport("audio.dll", EntryPoint = "drflac_open_and_decode_file_s16")]
			public static extern IntPtr open_and_decode_file_s16(string path, out uint channels, out uint sampleRate, out ulong sampleCount);

			[DllImport("audio.dll", EntryPoint = "drflac_free")]
			public static extern void free(IntPtr data);
		}

		// Interface for stb_vorbis.c code
		private static class StbVorbis
		{
			[DllImport("audio.dll", EntryPoint = "stb_vorbis_decode_filename")]
			public static extern int decode_filename(string path, out int channels, out int sample_rate, out IntPtr output);

			[DllImport("audio.dll", EntryPoint = "_stl_c_free")]
			public static extern void stl_c_free(IntPtr memory);

			public static string GetError(int error)
			{
				switch (error)
				{
					case 0: return "no error";
					case 1: return "need more data";
					case 2: return "invalid api mixing";
					case 3: return "out of memory";
					case 4: return "feature not supported";
					case 5: return "too many channels";
					case 6: return "file open failure";
					case 7: return "seek without length";
					case 10: return "unexpected eof";
					case 11: return "seek invalid";
					case 20: return "invalid setup";
					case 21: return "invalid stream";
					case 30: return "missing capture pattern";
					case 31: return "invalid stream structre pattern";
					case 32: return "continued packet flag invalid";
					case 33: return "incorrect stream serial number";
					case 34: return "invalid first page";
					case 35: return "bad packet type";
					case 36: return "cant find last page";
					case 37: return "seek failed";
					default: return "unknown error";
				}
			}
		}
	}
}
