using System;
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
		private readonly FSRStream _stream;
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
		#endregion // Public Fields

		public bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		internal Song(FSRStream stream, uint rate)
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

			// Will only actually start the thread if it is not already running
			SongThread.Start();
		}
		~Song()
		{
			dispose(false);
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
			}
			_lastState = nowState;
			if (_lastState != SoundState.Playing) // Dont update if we arent currently playing
				return;

			// Get the current frame offset
			uint foff = getFrameOffset();
			bool onLastFrame = _currentFrame == (_frameCount - 1);

			// Sudden jump backwards from the last offset means a new frame has started and we need to stream
			// This will not happen in the same frame as the buffer dequeue, which is what triggers this
			if ((foff < _lastOffset) && !onLastFrame)
			{
				streamFrame();
				queueLastFrame();
			}
			_lastOffset = foff;

			// Check if we need to dequeue an old buffer (this happens in the frame before a new stream is triggered)
			if (foff > BUFF_SIZE)
			{
				uint handle = 0;
				AL10.alSourceUnqueueBuffers(_handle, 1, ref handle);
				ALUtils.CheckALError("Unable to dequeue streaming buffer.");
				_currentFrame += 1;
			}
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
			uint sCount = Math.Min(BUFF_SIZE, _stream.RemainingFrames);
			if (_stream.Read(SampleBuffer, sCount) != sCount)
				throw new InvalidOperationException("Unable to read expected number of samples from stream.");
			_buffers[_bufferIndex].SetData(SampleBuffer, _stream.Stereo ? AudioFormat.Stereo16 : AudioFormat.Mono16, SampleRate, 0, sCount * (_stream.Stereo ? 2u : 1u));
			_bufferIndex = 1 - _bufferIndex;
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
				_stream.Dispose();
				SampleBuffer = null;
			}
			IsDisposed = true;
		}
		#endregion // IDisposable
	}
}
