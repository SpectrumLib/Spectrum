using System;
using System.Diagnostics;
using System.IO;
using static Spectrum.InternalLog;

namespace Spectrum.Audio
{
	/// <summary>
	/// Contains audio information for a specific sound effect.
	/// </summary>
	/// <remarks>
	/// This class keeps all of its audio data in memory at once. This makes it good for short sounds that are played
	/// often or in quick succession, but bad for large sound bytes. Use <see cref="Song"/> for streaming audio.
	/// </remarks>
	public sealed class SoundEffect : IDisposable
	{
		#region Fields
		/// <summary>
		/// The duration of this audio data, played at default settings.
		/// </summary>
		public TimeSpan Duration => Buffer.Duration;

		internal readonly SoundBuffer Buffer;

		private bool _isDisposed = false;
		#endregion // Fields

		// Buffer instances are wholey owned by each sound effect, do not reuse the same buffer for multiple effects
		private SoundEffect(SoundBuffer buffer)
		{
			Buffer = buffer;
		}
		~SoundEffect()
		{
			dispose(false);
		}

		/// <summary>
		/// Plays an instance of the sound effect. This instance plays in a fire-and-forget fashion, and cannot be
		/// controlled past the original volume and pitch settings.
		/// </summary>
		/// <param name="volume">The volume of the sound effect being played (<see cref="SoundEffectInstance.Volume"/>).</param>
		/// <param name="pitch">The pitch of the sound effect being played (<see cref="SoundEffectInstance.Pitch"/>).</param>
		/// <returns>If the effect was able to play.</returns>
		public bool Play(float volume = 1, float pitch = 0)
		{
			try
			{
				var sf = new SoundEffectInstance(this);
				sf.Volume = volume;
				sf.Pitch = pitch;
				sf.Play();
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Creates a controllable instance of this sound effect.
		/// </summary>
		/// <returns>The new sound effect instance.</returns>
		public SoundEffectInstance CreateInstance() => new SoundEffectInstance(this);

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
				if (disposing)
					Buffer.Dispose();

				_isDisposed = true;
			}
		}
		#endregion // IDisposable

        /// <summary>
        /// This function will attempt to load a sound effect from an unprocessed file. This function only supports
        /// WAV, OGG (Vorbis), and FLAC formats. It will select the format based on the file extension.
        /// </summary>
        /// <param name="path">The path to the audio file to load.</param>
        /// <returns>A sound effect containing the audio file data.</returns>
        public static SoundEffect LoadFromFile(string path)
        {
			string ext = Path.GetExtension(path);

			Stopwatch timer = Stopwatch.StartNew();

			SoundBuffer sb = null;
			switch (ext)
			{
				case ".wav":
				case ".wave":
					sb = AudioLoader.LoadWaveFile(path);
					break;
				case ".ogg":
					sb = AudioLoader.LoadVorbisFile(path);
					break;
				case ".flac":
					sb = AudioLoader.LoadFlacFile(path);
					break;
				default:
					throw new ArgumentException($"The file extension '{ext}' is not an understood audio file extension");
			}

			LDEBUG($"Loaded audio file '{path}' as SoundEffect in {timer.ElapsedMilliseconds:.00} ms.");

			return new SoundEffect(sb);
        }
	}
}
