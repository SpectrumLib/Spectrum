using System;
using System.Collections.Generic;
using System.Linq;
using OpenAL;

namespace Spectrum.Audio
{
	/// <summary>
	/// Represents a playing instance of a specific <see cref="SoundEffect"/>.
	/// </summary>
	public sealed class SoundEffectInstance : IDisposable
	{
		private static readonly List<SoundEffectInstance> s_activeInstances = new List<SoundEffectInstance>();
		private static readonly object s_instLock = new object();
		private static float s_lastClean = 0;

		#region Fields
		/// <summary>
		/// The sound effect that this instance is playing from.
		/// </summary>
		public readonly SoundEffect Effect;

		// The source handle
		internal uint Source { get; private set; }
		internal bool HasSource => (Source != 0);

		/// <summary>
		/// The current playback state of this instance.
		/// </summary>
		public SoundState State
		{
			get
			{
				if (Source == 0) return SoundState.Stopped;

				AL10.alGetSourcei(Source, AL10.AL_SOURCE_STATE, out int state);
				ALUtils.CheckALError("could not get source state");
				switch (state)
				{
					case AL10.AL_INITIAL:
					case AL10.AL_STOPPED:
						return SoundState.Stopped;
					case AL10.AL_PAUSED:
						return SoundState.Paused;
					case AL10.AL_PLAYING:
						return SoundState.Playing;
					default:
						throw new AudioException("OpenAL source state returned invalid value");
				}
			}
		}
		/// <summary>
		/// If the instance is currently playing.
		/// </summary>
		public bool IsPlaying => State == SoundState.Playing;
		/// <summary>
		/// If the instance is currently paused.
		/// </summary>
		public bool IsPaused => State == SoundState.Paused;
		/// <summary>
		/// If the instance is stopped, or has never been played.
		/// </summary>
		public bool IsStopped => State == SoundState.Stopped;

		private bool _isDisposed = false;
		#endregion // Fields

		internal SoundEffectInstance(SoundEffect effect)
		{
			Effect = effect;
			Source = 0;
		}
		~SoundEffectInstance()
		{
			dispose(false);
		}

		#region State Control
		/// <summary>
		/// Either starts playing the sound effect, or resumes playback after pausing. If the sound effect is already
		/// playing, this function has no effect.
		/// </summary>
		public void Play()
		{
			if (State == SoundState.Playing) return;

			// Reserve a source if we dont have one
			if (Source == 0)
				Source = AudioEngine.ReserveSource();

			// Set the source buffer
			AL10.alSourcei(Source, AL10.AL_BUFFER, (int)Effect.Buffer.Handle);
			ALUtils.CheckALError("could not set the source buffer");

			// Play the sound
			AL10.alSourcePlay(Source);
			ALUtils.CheckALError("unable to play audio source");

			// Register this is an active instance
			RegisterInstance(this);
		}

		/// <summary>
		/// Pauses the playback of the audio. If the effect is not playing, this function has no effect.
		/// </summary>
		public void Pause()
		{
			if (State != SoundState.Playing) return;

			AL10.alSourcePause(Source);
			ALUtils.CheckALError("unable to pause source");
		}
		
		/// <summary>
		/// Stops the playback of the audio. If the effect is stopped, this function has no effect.
		/// </summary>
		public void Stop()
		{
			if (State == SoundState.Stopped) return;

			AL10.alSourceStop(Source);
			ALUtils.CheckALError("unable to stop source");

			freeSource();
			ReleaseInstance(this);
		}
		#endregion // State Control

		// See UpdateInstances for the reason for this to return true always
		private bool freeSource()
		{
			if (!_isDisposed && (Source != 0))
			{
				AudioEngine.ReleaseSource(Source);
				Source = 0;
			}
			return true;
		}

		#region Instance Management
		// Called once every frame, but only checks for out-of-date instances every 1/4 second
		internal static void UpdateInstances()
		{
			if ((Time.Elapsed - s_lastClean) < 0.25f) return;

			s_lastClean = Time.Elapsed;

			lock (s_instLock)
			{
				s_activeInstances.RemoveAll(inst => (inst.State == SoundState.Stopped) ? inst.freeSource() : false);
			}
		}

		private static void RegisterInstance(SoundEffectInstance inst)
		{
			lock (s_instLock)
			{
				s_activeInstances.Add(inst);
			}
		}

		private static void ReleaseInstance(SoundEffectInstance inst)
		{
			lock (s_instLock)
			{
				s_activeInstances.Remove(inst);
			}
		}
		#endregion // Instance Management

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
				_isDisposed = true;
			}
		}
		#endregion // IDisposable
	}
}
