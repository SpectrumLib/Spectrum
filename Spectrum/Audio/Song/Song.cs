using System;
using System.Diagnostics;
using OpenAL;
using Spectrum.Content;

namespace Spectrum.Audio
{
	/// <summary>
	/// Represents a source of audio data that is streamed from the disk. Unlike <see cref="SoundEffect"/>, this type
	/// streams data from the disk in chunks, instead of loading it all into memory at once.
	/// </summary>
	public sealed class Song : IDisposableContent, IAudioSource
	{
		private const uint BUFF_SIZE = 524_288; // 1MB buffers for audio (2MB for stereo) (~11.9 seconds at 44100 Hz)

		#region Fields
		// The data stream
		private readonly IAudioStreamer _stream;
		// The sampling rate for this audio
		internal readonly uint SampleRate;
		// The number of frames in this song
		private readonly uint _frameCount;

		// The intermediate buffer to load samples into before passing them to OpenAL
		internal short[] SampleBuffer { get; private set; }

		// The OpenAL source handle
		private uint _handle = 0;
		internal bool HasHandle => _handle != 0;

		// The OpenAL buffers for this song
		private SoundBuffer[] _buffers;

		// The last update states used for state changes and streaming
		private SoundState _lastState = SoundState.Stopped;
		private uint _lastOffset = UInt32.MaxValue;
		private uint _bufferIndex = 0; // Tracks the OpenAL buffer to use for the next streaming operation (can only be 0 or 1)
		private uint _lastLoadedSize = 0; // Tracks the size of the last queued buffer
		private uint _playingBufferSize = 0; // The size of the currently playing buffer (to detect when it is time to stream)

		// The current song frame number
		private uint _currentFrame = 0;

		#region Public Fields
		/// <summary>
		/// The length of this song at normal playback speed.
		/// </summary>
		public readonly TimeSpan Duration;

		/// <summary>
		/// The current offset into the song.
		/// </summary>
		public TimeSpan Offset => TimeSpan.FromSeconds(getFullOffset() / (double)SampleRate);

		/// <summary>
		/// Gets the current playback state of this song instance.
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

		/// <summary>
		/// Gets or sets if the song should loop when it is done playing.
		/// </summary>
		public bool IsLooped = false;

		private uint _loopCount = 0;
		/// <summary>
		/// The number of times the song has looped since the last call to <see cref="Play()"/>. If <see cref="IsLooped"/>
		/// is <c>false</c>, then this value will be zero.
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
		#endregion // Public Fields

		/// <summary>
		/// Event raised when the song starts to play, is paused, or is stopped.
		/// </summary>
		public event SongStateChangeHandler StateChanged;
		/// <summary>
		/// Event raised when the song streams a frame from the disk. This event will only be raised if the song is 
		/// currently playing.
		/// </summary>
		public event SongStreamHandler FrameStreamed;

		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		internal Song(IAudioStreamer stream, uint rate)
		{
			_stream = stream;
			SampleRate = rate;
			_frameCount = (uint)Math.Ceiling(stream.FrameCount / (double)BUFF_SIZE);
			Duration = TimeSpan.FromSeconds(stream.FrameCount / (double)rate);

			SampleBuffer = new short[BUFF_SIZE * (_stream.Stereo ? 2u : 1u)];

			_buffers = new SoundBuffer[2] { new SoundBuffer(), new SoundBuffer() };

			// Stream the first frame to be ready (but dont queue it, which is done in Play())
			_bufferIndex = 0;
			streamFrame();
		}
		~Song()
		{
			dispose(false);
		}

		/// <summary>
		/// Starts playing the song from the start (if stopped), or from the last played point (if paused). If already
		/// playing, this call does nothing.
		/// </summary>
		public void Play()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(nameof(Song));
			if (IsPlaying)
				return;

			// If starting to play, reserve a source and disable looping
			if (IsStopped)
			{
				_handle = AudioEngine.ReserveSource();
				queueLastFrame(); // Will queue frame index 0
				_playingBufferSize = _lastLoadedSize;
				_loopCount = 0;
			}

			// Set the effects, if they are dirty
			if (IsStopped || _pitchDirty)
				Pitch = _pitch;
			if (IsStopped || _volumeDirty)
				Volume = _volume;

			// Play the source
			var oldState = State;
			AL10.alSourcePlay(_handle);
			ALUtils.CheckALError("Unable to play audio source.");

			// Start and add the song to the thread
			SongThread.AddSong(this);
			SongThread.Start();

			// Raise the event
			StateChanged?.Invoke(this, oldState, SoundState.Playing, false);
		}

		/// <summary>
		/// Pauses the song playback at the current point. If the song is not playing, this call does nothing.
		/// </summary>
		public void Pause()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(nameof(Song));
			if (!IsPlaying)
				return;

			AL10.alSourcePause(_handle);
			ALUtils.CheckALError("Unable to pause audio source.");

			// Raise the event
			StateChanged?.Invoke(this, SoundState.Playing, SoundState.Paused, false);
		}

		/// <summary>
		/// Stops the song playback. Resets the song to the beginning of playback. If the song is already stopped, this
		/// call does nothing.
		/// </summary>
		public void Stop()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(nameof(Song));
			if (IsStopped)
				return;

			var oldState = State;
			AL10.alSourceStop(_handle);
			ALUtils.CheckALError("Unable to stop audio source.");

			reset();
			AudioEngine.ReleaseSource(_handle);
			_handle = 0;
			SongThread.RemoveSong(this);

			// Raise the event
			StateChanged?.Invoke(this, oldState, SoundState.Stopped, true);
		}

		// Performs the check to see if the song needs to stream or be removed from the list of active songs
		internal void Update()
		{
			var nowState = State;
			if ((nowState == SoundState.Stopped) && (_lastState == SoundState.Playing)) // Song ran out on its own
			{
				reset();
				AudioEngine.ReleaseSource(_handle);
				_handle = 0;
				SongThread.RemoveSong(this);

				// Raise the event (needs to post to the main thread)
				CoroutineManager.PostEvent(() => StateChanged?.Invoke(this, SoundState.Playing, SoundState.Stopped, false));
			}
			_lastState = nowState;
			if (_lastState != SoundState.Playing) // Dont update if we arent currently playing
				return;

			// Get the current frame offset
			uint foff = getFrameOffset();
			bool onLastFrame = _currentFrame == (_frameCount - 1);

			// Check if we are beyond the current limit (we have moved into the next buffer, one is free for streaming)
			// When this happens, dequeue one buffer, which will cause a sudden shift backwards in the sample offset for the source
			if (foff > _playingBufferSize)
			{
				uint handle = 0;
				AL10.alSourceUnqueueBuffers(_handle, 1, ref handle);
				ALUtils.CheckALError("Unable to dequeue streaming buffer.");
				_currentFrame += 1;
				if (IsLooped && (_currentFrame == _frameCount))
				{
					_currentFrame = 0;
					_loopCount += 1;
				}
			}

			// Sudden jump backwards from the last offset means a new frame has started and we need to stream
			// This will not happen in the same frame as the buffer dequeue, which is what triggers this
			// _lastOffset is set to u32 max value to trigger an immediate stream of the second frame when a song starts
			if ((foff < _lastOffset) && (IsLooped || !onLastFrame))
			{
				if (onLastFrame && IsLooped)
					_stream.Reset();

				_playingBufferSize = _lastLoadedSize;
				streamFrame();
				queueLastFrame();
			}
			_lastOffset = foff;
		}

		// Resets the stream and playback state (ensure that the source is stopped before calling this)
		private void reset()
		{
			if (IsDisposed || !HasHandle)
				return;

			// Unqueue the buffers (dont care about errors, just need the queue emtpy)
			uint handle = 0;
			AL10.alSourceUnqueueBuffers(_handle, 1, ref handle);
			AL10.alSourceUnqueueBuffers(_handle, 1, ref handle);
			ALUtils.ClearALError();

			// Reset the stream information
			_stream.Reset();
			_currentFrame = 0;
			_lastState = SoundState.Stopped;
			_lastOffset = UInt32.MaxValue;

			// Re-stream the first frame to be ready (but dont queue it, which is done in Play())
			_bufferIndex = 0;
			streamFrame();
		}

		// Streams a single frame into the current buffer, updates the buffer index
		private void streamFrame()
		{
			Stopwatch timer = Stopwatch.StartNew();
			uint sCount = Math.Min(BUFF_SIZE, _stream.RemainingFrames);
			if (_stream.ReadFrames(SampleBuffer, sCount) != sCount)
				throw new InvalidOperationException("Unable to read expected number of samples from stream.");
			_buffers[_bufferIndex].SetData(SampleBuffer, _stream.Stereo ? AudioFormat.Stereo16 : AudioFormat.Mono16, SampleRate, 0, sCount * (_stream.Stereo ? 2u : 1u));
			_bufferIndex = 1 - _bufferIndex;
			_lastLoadedSize = sCount;

			// Raise stream event
			if (IsPlaying)
			{
				bool isLastFrame = _currentFrame == (_frameCount - 1);
				TimeSpan start = isLastFrame ? TimeSpan.Zero : TimeSpan.FromSeconds(((_currentFrame * BUFF_SIZE) + _playingBufferSize) / (double)SampleRate);
				var length = TimeSpan.FromSeconds(sCount / (double)SampleRate);
				CoroutineManager.PostEvent(() => FrameStreamed?.Invoke(this, start, length, timer.Elapsed)); 
			}
		}

		// Queues the last used frame (!_bufferIndex) to be played next
		private void queueLastFrame()
		{
			uint handle = _buffers[1 - _bufferIndex].Handle;
			AL10.alSourceQueueBuffers(_handle, 1, ref handle);
			ALUtils.CheckALError("Unable to queue buffer to play.");
		}

		// Calculates the current offset into the entire song, in samples
		private uint getFullOffset()
		{
			if (!HasHandle)
				return 0;

			AL10.alGetSourcei(_handle, AL11.AL_SAMPLE_OFFSET, out int offset);
			ALUtils.CheckALError("Unable to get frame offset.");
			return (_currentFrame * BUFF_SIZE) + (uint)offset;
		}

		// Calculates the offset into the current playing frame
		private uint getFrameOffset()
		{
			if (!HasHandle)
				return 0;

			AL10.alGetSourcei(_handle, AL11.AL_SAMPLE_OFFSET, out int offset);
			ALUtils.CheckALError("Unable to get frame offset.");
			return (uint)offset;
		}

		private SoundState getState()
		{
			if (!HasHandle)
				return SoundState.Stopped;

			AL10.alGetSourcei(_handle, AL10.AL_SOURCE_STATE, out int state);
			ALUtils.CheckALError("Could not get audio state.");
			switch (state)
			{
				case AL10.AL_INITIAL:
				case AL10.AL_STOPPED: return SoundState.Stopped;
				case AL10.AL_PLAYING: return SoundState.Playing;
				case AL10.AL_PAUSED: return SoundState.Paused;
				default: throw new InvalidOperationException("Invalid state for OpenAL source.");
			}
		}

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (!IsDisposed && disposing)
			{
				Stop();
				if (_stream is IDisposable)
					(_stream as IDisposable).Dispose();
				_buffers[0].Dispose();
				_buffers[1].Dispose();
				_buffers = null;
				SampleBuffer = null;
			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}

	/// <summary>
	/// Delegate for events raised when the state of a song changes (starts playing, pauses, or stops).
	/// </summary>
	/// <param name="song">The song that raised the event.</param>
	/// <param name="oldState">The old state of the song.</param>
	/// <param name="newState">The new state of the song.</param>
	/// <param name="manual">
	/// If `newState` is <see cref="SoundState.Stopped"/>, this indicates if the song was stopped manually, or finished
	/// normally by reaching the end while playing.
	/// </param>
	public delegate void SongStateChangeHandler(Song song, SoundState oldState, SoundState newState, bool manual);

	/// <summary>
	/// Delegate for the event raised when a song streams a frame from the disk.
	/// </summary>
	/// <param name="song">The song that raised the event.</param>
	/// <param name="start">The offset into the song that the streamed frame starts at.</param>
	/// <param name="length">The length of the audio data streamed from the disk.</param>
	/// <param name="elapsed">The time it took to stream the frame.</param>
	public delegate void SongStreamHandler(Song song, TimeSpan start, TimeSpan length, TimeSpan elapsed);
}
