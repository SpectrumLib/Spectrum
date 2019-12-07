/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;

namespace Spectrum.Audio
{
    /// <summary>
    /// Represents an instance of a <see cref="Sound"/> that is currently in playback.
    /// </summary>
	public sealed class SoundInstance : IDisposable
	{
		private static readonly List<SoundInstance> _ActiveInstances = new List<SoundInstance>();
		private static readonly object _InstLock = new object();
		private static float _LastClean = 0;

		#region Fields
		/// <summary>
		/// The sound object that this is an instance of.
		/// </summary>
		public readonly Sound Sound;

		// The source handle
		internal uint Source = 0;
		internal bool HasSource => (Source != 0);

		/// <summary>
		/// The current playback state of this instance.
		/// </summary>
		public SoundState State
		{
			get
			{
				if (!HasSource)
					return SoundState.Stopped;

				AudioEngine.OpenAL.GetSourcei(Source, OpenAL.AL.SOURCE_STATE, out var state);
				AudioEngine.OpenAL.CheckALError("get source state");

				return state switch { 
					OpenAL.AL.INITIAL => SoundState.Stopped,
					OpenAL.AL.STOPPED => SoundState.Stopped,
					OpenAL.AL.PAUSED => SoundState.Paused,
					OpenAL.AL.PLAYING => SoundState.Playing,
					_ => throw new AudioException("Invalid value for source state.")
				};
			}
		}
		/// <summary>
		/// Gets if the instance is currently playing.
		/// </summary>
		public bool IsPlaying => State == SoundState.Playing;
		/// <summary>
		/// Gets if the instance is currently paused.
		/// </summary>
		public bool IsPaused => State == SoundState.Paused;
		/// <summary>
		/// Gets if the instance is stopped, or has never been played.
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
				if (HasSource)
				{
					AudioEngine.OpenAL.Sourcei(Source, OpenAL.AL.LOOPING, value ? 1 : 0);
					AudioEngine.OpenAL.CheckALError("could not set the audio looping");
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
				_pitch = Math.Clamp(value, -1, 1);
				if (HasSource)
				{
					AudioEngine.OpenAL.Sourcef(Source, OpenAL.AL.PITCH, MathF.Pow(2, _pitch)); // Map to OpenAL's [0.5, 2] range
					AudioEngine.OpenAL.CheckALError("could not set audio pitch");
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
				_volume = Math.Clamp(value, 0, 1);
				if (HasSource)
				{
					AudioEngine.OpenAL.Sourcef(Source, OpenAL.AL.GAIN, _volume);
					AudioEngine.OpenAL.CheckALError("could not set audio volume");
					_volumeDirty = false;
				}
				else
					_volumeDirty = true;
			}
		}
		#endregion // Standard Control
		#endregion // Fields

		internal SoundInstance(Sound sound)
		{
			Sound = sound;
			Source = 0;	
		}
		~SoundInstance()
		{
			dispose(false);
		}

		// See UpdateInstances for the reason for this to return true always
		private bool freeSource()
		{
			if (HasSource)
			{
				AudioEngine.ReleaseSource(Source);
				Source = 0;
			}
			return true;
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

			var oal = AudioEngine.OpenAL;

			// Reserve a source if we dont have one, and set the buffer
			if (!HasSource)
			{
				Source = AudioEngine.ReserveSource();
				oal.Sourcei(Source, OpenAL.AL.BUFFER, (int)Sound.Buffer.Handle);
				oal.CheckALError("could not set the source buffer");
			}

			// Set the standard control values
			if (_loopedDirty)
			{
				oal.Sourcei(Source, OpenAL.AL.LOOPING, _isLooped ? 1 : 0);
				oal.CheckALError("could not set audio looping");
				_loopedDirty = false;
			}
			if (_pitchDirty)
			{
				oal.Sourcef(Source, OpenAL.AL.PITCH, (float)Math.Pow(2, _pitch));
				oal.CheckALError("could not set audio pitch");
				_pitchDirty = false;
			}
			if (_volumeDirty)
			{
				oal.Sourcef(Source, OpenAL.AL.GAIN, _volume);
				oal.CheckALError("could not set audio volume");
				_volumeDirty = false;
			}

			// Play the sound
			oal.SourcePlay(Source);
			oal.CheckALError("unable to play audio source");

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

			var oal = AudioEngine.OpenAL;
			oal.SourcePause(Source);
			oal.CheckALError("unable to pause source");
		}

		/// <summary>
		/// Stops the playback of the audio. If the effect is stopped, this function has no effect.
		/// </summary>
		public void Stop()
		{
			if (IsStopped) return;

			var oal = AudioEngine.OpenAL;
			oal.SourceStop(Source);
			oal.CheckALError("unable to stop source");

			freeSource();
			ReleaseInstance(this);
		}
		#endregion // State Control

		#region Instance Management
		// Called once every frame, but only checks for out-of-date instances every 1/4 second
		internal static void UpdateInstances()
		{
			if ((Time.Elapsed - _LastClean) < 0.25f) return;

			_LastClean = Time.Elapsed;

			lock (_InstLock)
			{
				_ActiveInstances.RemoveAll(inst => (inst.State == SoundState.Stopped) && inst.freeSource());
			}
		}

		private static void RegisterInstance(SoundInstance inst)
		{
			lock (_InstLock)
			{
				_ActiveInstances.Add(inst);
			}
		}

		private static void ReleaseInstance(SoundInstance inst)
		{
			lock (_InstLock)
			{
				_ActiveInstances.Remove(inst);
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
			if (HasSource)
				Stop();
		}
		#endregion // IDisposable
	}
}
