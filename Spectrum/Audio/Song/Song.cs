/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;

namespace Spectrum.Audio
{
	/// <summary>
	/// Represents a specific set of audio data, that is streamed from the disk. Unlike <see cref="Sound"/>, the
	/// audio data and playback state reside in the same object for songs.
	/// </summary>
	public sealed class Song : IDisposable
	{
		private const uint SECONDS_PER_STREAM = 5; // Stream 5 seconds at a time

		#region Fields
		// Song objects/values
		private readonly IAudioStreamer _stream;
		private readonly uint _blockCount;
		internal readonly uint SampleRate;
		internal uint Source { get; private set; } = 0;
		internal bool HasSource => Source != 0;
		private SoundState _lastState = SoundState.Stopped;

		// Streaming objects/values
		private readonly AudioBuffer[] _sourceBuffers;
		private readonly byte[] _streamBuffer;
		private readonly uint _framesPerBlock;
		private uint _lastOffset = UInt32.MaxValue;
		private uint _bufferIndex = 0;   // Index in _sourceBuffers to use for next stream
		private uint _loadedSize = 0;    // Size of last queued buffer in frames
		private uint _playingSize = 0;   // Size of playing buffer in frames
		private uint _currentBlock = 0;  // Currently playing block index

		/// <summary>
		/// The total length of the song.
		/// </summary>
		public readonly TimeSpan Duration;
		/// <summary>
		/// The current offset into the song.
		/// </summary>
		public TimeSpan Offset => TimeSpan.FromSeconds(getFullOffset() / (double)SampleRate);
		/// <summary>
		/// The current playback state of the song.
		/// </summary>
		public SoundState State => getState();
		/// <summary>
		/// Gets if the song is currently stopped.
		/// </summary>
		public bool IsStopped => State == SoundState.Stopped;
		/// <summary>
		/// Gets if the song is currently paused.
		/// </summary>
		public bool IsPaused => State == SoundState.Paused;
		/// <summary>
		/// Gets if the song is currently playing.
		/// </summary>
		public bool IsPlaying => State == SoundState.Playing;

		#region Effects
		/// <summary>
		/// Gets or sets if the song should loop when it is done playing.
		/// </summary>
		public bool IsLooped = false;

		private uint _loopCount = 0;
		/// <summary>
		/// The number of times the song has looped since the last call to <see cref="Play()"/>.
		/// </summary>
		public uint LoopCount => IsLooped ? _loopCount : 0;

		private float _pitch = 0;
		private bool _pitchDirty = true;
		/// <summary>
		/// Gets or sets the pitch of the song, in the range [-1, 1]. -1 is a full octave down, 0 is unchanged, and 
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
		#endregion // Effects

		private bool _isDisposed = false;
		#endregion // Fields

		internal Song(IAudioStreamer stream, uint rate)
		{
			_stream = stream;
			SampleRate = rate;
			Duration = TimeSpan.FromSeconds(stream.TotalFrames / (double)rate);

			_framesPerBlock = rate * SECONDS_PER_STREAM;
			_blockCount = (uint)Math.Ceiling(stream.TotalFrames / (double)_framesPerBlock);
			_streamBuffer = new byte[_framesPerBlock * stream.Format.GetFrameSize()];
			_sourceBuffers = new AudioBuffer[] { new AudioBuffer(), new AudioBuffer() };

			streamBlock(); // Stream first block
		}
		~Song()
		{
			dispose(false);
		}

		#region States
		/// <summary>
		/// Starts playing the song from the start (if stopped), or from the last played point (if paused).
		/// </summary>
		public void Play()
		{
			if (_isDisposed)
				throw new ObjectDisposedException(nameof(Song));
			if (IsPlaying)
				return;

			// If starting to play, reserve a source and disable looping
			if (IsStopped)
			{
				Source = AudioEngine.ReserveSource();
				queueLastBlock(); // Will queue frame index 0
				_playingSize = _loadedSize;
				_loopCount = 0;
			}

			// Set the effects, if they are dirty
			if (IsStopped || _pitchDirty)
				Pitch = _pitch;
			if (IsStopped || _volumeDirty)
				Volume = _volume;

			// Play the source
			var oldState = State;
			AudioEngine.OpenAL.SourcePlay(Source);
			AudioEngine.OpenAL.CheckALError("Unable to play audio source.");

			// Start and add the song to the thread
			SongThread.AddSong(this);
			SongThread.Start();
		}

		/// <summary>
		/// Pauses the song playback at the current point.
		/// </summary>
		public void Pause()
		{
			if (_isDisposed)
				throw new ObjectDisposedException(nameof(Song));
			if (!IsPlaying)
				return;

			AudioEngine.OpenAL.SourcePause(Source);
			AudioEngine.OpenAL.CheckALError("Unable to pause audio source.");
		}

		/// <summary>
		/// Stops the song playback. Resets the song to the beginning of playback.
		/// </summary>
		public void Stop()
		{
			if (_isDisposed)
				throw new ObjectDisposedException(nameof(Song));
			if (IsStopped)
				return;

			var oldState = State;
			AudioEngine.OpenAL.SourceStop(Source);
			AudioEngine.OpenAL.CheckALError("Unable to stop audio source.");

			reset();
			AudioEngine.ReleaseSource(Source);
			Source = 0;
			SongThread.RemoveSong(this);
		}
		#endregion // States

		// Performs song streaming, and cleanup of songs that have finished playing
		// Called by the SongThread, nominally at 10Hz
		internal void Update()
		{
			var nowState = getState();
			if ((nowState == SoundState.Stopped) && (_lastState == SoundState.Playing)) // Song ran out on its own
			{
				reset();
				AudioEngine.ReleaseSource(Source);
				Source = 0;
				SongThread.RemoveSong(this);
			}
			_lastState = nowState;
			if (_lastState != SoundState.Playing) // Dont update if we arent currently playing
				return;

			// Get the current frame offset
			uint foff = getFrameOffset();
			bool onLastBlock = _currentBlock == (_blockCount - 1);

			// Check if we are beyond the current limit (we have moved into the next buffer, one is free for streaming)
			// When this happens, dequeue one buffer, which will cause a sudden shift backwards in the sample offset for the source
			if (foff > _playingSize)
			{
				uint handle = 0;
				AudioEngine.OpenAL.SourceUnqueueBuffers(Source, 1, out handle);
				AudioEngine.OpenAL.CheckALError("Unable to dequeue streaming buffer.");
				_currentBlock += 1;
				if (IsLooped && onLastBlock)
				{
					_currentBlock = 0;
					_loopCount += 1;
				}
			}

			// Sudden jump backwards from the last offset means a new frame has started and we need to stream
			// This will not happen in the same frame as the buffer dequeue, which is what triggers this
			// _lastOffset is set to u32 max value to trigger an immediate stream of the second frame when a song starts
			if ((foff < _lastOffset) && (IsLooped || !onLastBlock))
			{
				if (onLastBlock && IsLooped)
					_stream.Reset();

				_playingSize = _loadedSize;
				streamBlock();
				queueLastBlock();
			}
			_lastOffset = foff;
		}

		#region Streaming
		// Resets the stream and playback state (ensure that the source is stopped before calling this)
		private void reset()
		{
			if (_isDisposed || !HasSource)
				return;

			// Unqueue the buffers (dont care about errors, just need the queue emtpy)
			uint handle = 0;
			AudioEngine.OpenAL.SourceUnqueueBuffers(Source, 1, out handle);
			AudioEngine.OpenAL.SourceUnqueueBuffers(Source, 1, out handle);
			AudioEngine.OpenAL.ClearALError();

			// Reset the stream information
			_stream.Reset();
			_currentBlock = 0;
			_lastState = SoundState.Stopped;
			_lastOffset = UInt32.MaxValue;

			// Re-stream the first block to be ready
			_bufferIndex = 0;
			streamBlock();
		}

		private void streamBlock()
		{
			// Load the next block of data into the buffer
			var toload = Math.Min(_framesPerBlock, _stream.RemainingFrames);
			if (_stream.ReadFrames(_streamBuffer.AsSpan(), toload) != toload)
				throw new AudioException("Unable to stream expected number of frames.");

			// Set the buffer data and update streaming values
			_sourceBuffers[_bufferIndex].SetData(
				_streamBuffer.AsReadOnlySpan().Slice(0, (int)(toload * _stream.Format.GetFrameSize())),
				_stream.Format, SampleRate);
			_bufferIndex = 1 - _bufferIndex;
			_loadedSize = toload;
		}

		// Queues the last used frame (!_bufferIndex) to be played next
		private void queueLastBlock()
		{
			uint handle = _sourceBuffers[1 - _bufferIndex].Handle;
			AudioEngine.OpenAL.SourceQueueBuffers(Source, 1, out handle);
			AudioEngine.OpenAL.CheckALError("Unable to queue buffer to play.");
		}
		#endregion // Streaming

		#region Source Info
		// Calculates the current offset into the entire song, in samples
		private uint getFullOffset()
		{
			if (!HasSource)
				return 0;

			AudioEngine.OpenAL.GetSourcei(Source, OpenAL.AL.SAMPLE_OFFSET, out int offset);
			AudioEngine.OpenAL.CheckALError("Unable to get frame offset.");
			return (_currentBlock * _framesPerBlock) + (uint)offset;
		}

		// Calculates the offset into the current playing frame
		private uint getFrameOffset()
		{
			if (!HasSource)
				return 0;

			AudioEngine.OpenAL.GetSourcei(Source, OpenAL.AL.SAMPLE_OFFSET, out int offset);
			AudioEngine.OpenAL.CheckALError("Unable to get frame offset.");
			return (uint)offset;
		}

		private SoundState getState()
		{
			if (!HasSource)
				return SoundState.Stopped;

			AudioEngine.OpenAL.GetSourcei(Source, OpenAL.AL.SOURCE_STATE, out int state);
			AudioEngine.OpenAL.CheckALError("Could not get audio state.");
			return state switch { 
				OpenAL.AL.INITIAL => SoundState.Stopped,
				OpenAL.AL.STOPPED => SoundState.Stopped,
				OpenAL.AL.PLAYING => SoundState.Playing,
				OpenAL.AL.PAUSED => SoundState.Paused,
				_ => throw new AudioException("Invalid value for source play state.")
			};
		}
		#endregion // Source Info

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
				Stop();
				if (disposing)
				{
					(_stream as IDisposable)?.Dispose();
					_sourceBuffers[0].Dispose();
					_sourceBuffers[1].Dispose();
				}
			}
			_isDisposed = true;
		}
		#endregion // IDisposable
	}
}
