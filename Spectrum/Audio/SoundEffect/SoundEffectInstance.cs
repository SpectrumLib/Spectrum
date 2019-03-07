using System;
using System.Collections.Generic;
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
		private uint _handle = 0;
		internal bool HasHandle => (_handle != 0);

		/// <summary>
		/// The current playback state of this instance.
		/// </summary>
		public SoundState State
		{
			get
			{
				if (!HasHandle)
					return SoundState.Stopped;

				AL10.alGetSourcei(_handle, AL10.AL_SOURCE_STATE, out int state);
				ALUtils.CheckALError("could not get source state");
				switch (state)
				{
					case AL10.AL_INITIAL:
					case AL10.AL_STOPPED: return SoundState.Stopped;
					case AL10.AL_PAUSED: return SoundState.Paused;
					case AL10.AL_PLAYING: return SoundState.Playing;
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

		#region Standard Control
		private bool _isLooped = false;
		private bool _loopedDirty = true;
		/// <summary>
		/// Gets or sets if the audio is looped.
		/// </summary>
		public bool IsLooped
		{
			get => _isLooped;
			set
			{
				_isLooped = value;
				if (HasHandle)
				{
					AL10.alSourcei(_handle, AL10.AL_LOOPING, value ? 1 : 0);
					ALUtils.CheckALError("could not set the audio looping");
					_loopedDirty = false;
				}
				else
					_loopedDirty = true;
			}
		}

		private float _pitch = 0;
		private bool _pitchDirty = true;
		/// <summary>
		/// Gets or sets the pitch of the sound, in the range [-1, 1]. -1 is a full octave down, 0 is unchanged, and 
		/// +1 is a full octave up. Note that pitch shifting is performed simply by speeding up or slowing down the
        /// audio playback.
		/// </summary>
		public float Pitch
		{
			get => _pitch;
			set
			{
				_pitch = Mathf.Clamp(value, -1, 1);
				if (HasHandle)
				{
					AL10.alSourcef(_handle, AL10.AL_PITCH, Mathf.Pow(2, _pitch)); // Map to OpenAL's [0.5, 2] range
					ALUtils.CheckALError("could not set audio pitch");
					_pitchDirty = false;
				}
				else
					_pitchDirty = true;
			}
		}

		private float _volume = 1;
		private bool _volumeDirty = true;
		/// <summary>
		/// Gets or sets the volume of the sound effect, in the range [0, 1].
		/// </summary>
		public float Volume
		{
			get => _volume;
			set
			{
				_volume = Mathf.Clamp(value, 0, 1);
				if (HasHandle)
				{
					AL10.alSourcef(_handle, AL10.AL_GAIN, _volume);
					ALUtils.CheckALError("could not set audio volume");
					_volumeDirty = false;
				}
				else
					_volumeDirty = true;
			}
		}
		#endregion // Standard Control

		private bool _isDisposed = false;
		#endregion // Fields

		internal SoundEffectInstance(SoundEffect effect)
		{
			Effect = effect;
			_handle = 0;
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
			var currState = State;
			if (currState == SoundState.Playing) return;

			// Reserve a source if we dont have one, and set the buffer
			if (!HasHandle)
			{
				_handle = AudioEngine.ReserveSource();
				AL10.alSourcei(_handle, AL10.AL_BUFFER, (int)Effect.Buffer.Handle);
				ALUtils.CheckALError("could not set the source buffer");
			}

			// Set the standard control values
			if (_loopedDirty)
			{
				AL10.alSourcei(_handle, AL10.AL_LOOPING, _isLooped ? 1 : 0);
				ALUtils.CheckALError("could not set audio looping");
				_loopedDirty = false;
			}
			if (_pitchDirty)
			{
				AL10.alSourcef(_handle, AL10.AL_PITCH, (float)Math.Pow(2, _pitch));
				ALUtils.CheckALError("could not set audio pitch");
				_pitchDirty = false;
			}
			if (_volumeDirty)
			{
				AL10.alSourcef(_handle, AL10.AL_GAIN, _volume);
				ALUtils.CheckALError("could not set audio volume");
				_volumeDirty = false;
			}

			// Play the sound
			AL10.alSourcePlay(_handle);
			ALUtils.CheckALError("unable to play audio source");

			// Register this is an active instance (only if stopped and not paused)
			if (currState == SoundState.Stopped)
				RegisterInstance(this);
		}

		/// <summary>
		/// Pauses the playback of the audio. If the effect is not playing, this function has no effect.
		/// </summary>
		public void Pause()
		{
			if (!IsPlaying) return;

			AL10.alSourcePause(_handle);
			ALUtils.CheckALError("unable to pause source");
		}
		
		/// <summary>
		/// Stops the playback of the audio. If the effect is stopped, this function has no effect.
		/// </summary>
		public void Stop()
		{
			if (IsStopped) return;

			AL10.alSourceStop(_handle);
			ALUtils.CheckALError("unable to stop source");

			freeSource();
			ReleaseInstance(this);
		}
		#endregion // State Control

		// See UpdateInstances for the reason for this to return true always
		private bool freeSource()
		{
			if (!_isDisposed && HasHandle)
			{
				AudioEngine.ReleaseSource(_handle);
				_handle = 0;
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
				s_activeInstances.RemoveAll(inst => (inst.State == SoundState.Stopped) && inst.freeSource());
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
