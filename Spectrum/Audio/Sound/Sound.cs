/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using Spectrum.Content;

namespace Spectrum.Audio
{
    /// <summary>
    /// Represents a specific set of audio data, which can be spawned into <see cref="SoundInstance"/> objects for
    /// audio playback. Objects of this type hold all of their audio data in memory at once.
    /// </summary>
	public sealed class Sound : IDisposable, IAudioContent
	{
        #region Fields
        internal readonly AudioBuffer Buffer;

        /// <summary>
        /// The duration of the audio data for the sound.
        /// </summary>
        public TimeSpan Duration => Buffer.Duration;
		#endregion // Fields

        internal Sound(AudioBuffer buffer)
        {
            Buffer = buffer;
        }
        ~Sound()
        {
            dispose(false);
        }

        /// <summary>
        /// Plays an instance of the sound in a fire-and-forget fashion. The instance is cleaned up automatically when 
        /// it is complete.
        /// </summary>
        /// <param name="volume">The volume of the sound being played (<see cref="SoundInstance.Volume"/>).</param>
		/// <param name="pitch">The pitch of the sound being played (<see cref="SoundInstance.Pitch"/>).</param>
		/// <returns>If the effect was able to play.</returns>
        public bool Play(float volume = 1, float pitch = 0)
        {
            try
            {
                var si = new SoundInstance(this);
                si.Volume = volume;
                si.Pitch = pitch;
                si.Play();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a new controllable instance of this sound.
        /// </summary>
        /// <returns>The new sound instance.</returns>
        public SoundInstance CreateInstance() => new SoundInstance(this);

		#region IDisposable
		public void Dispose()
        {
            dispose(true);
            GC.SuppressFinalize(this);
        }

        private void dispose(bool disposing)
        {
            if (disposing && Buffer.Handle != 0)
                Buffer.Dispose();
        }
		#endregion // IDisposable
	}
}
