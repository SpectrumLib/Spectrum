using System;

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
	}
}
