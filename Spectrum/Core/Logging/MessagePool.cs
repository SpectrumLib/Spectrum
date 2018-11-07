using System;
using System.Collections.Generic;

namespace Spectrum
{
	// Used as a pool for messages waiting to be logged in asynchronous loggers. Allows multi-threaded reads and
	// writes.
	internal class MessagePool
	{
		// Initial size of the message pool
		private const uint INITIAL_POOL_SIZE = 128;

		#region Fields
		// The queue of waiting messages
		private string[] _queue;
		private uint _queueSize;
		// The lock object for the queue
		private readonly object _writeLock = new object();
		// The pointers in the queue
		public uint Count { get; private set; } = 0;
		#endregion // Fields

		public MessagePool()
		{
			_queue = new string[INITIAL_POOL_SIZE];
			_queueSize = INITIAL_POOL_SIZE;
		}

		public void AddMessage(string msg)
		{
			lock (_writeLock)
			{
				if (Count >= _queueSize)
				{
					Array.Resize(ref _queue, (int)(_queueSize + INITIAL_POOL_SIZE));
					_queueSize += INITIAL_POOL_SIZE;
				}

				_queue[Count] = msg;
				++Count;
			}
		}

		public IEnumerable<string> GetMessages()
		{
			uint msgCount = 0;
			lock (_writeLock) { msgCount = Count; }

			if (msgCount > 0)
			{
				for (uint i = 0; i < msgCount; ++i)
				{
					yield return _queue[i];
				}

				lock (_writeLock)
				{
					uint rem = Count - msgCount;
					if (rem > 0)
						Array.Copy(_queue, msgCount, _queue, 0, rem);
					Count = rem;
				} 
			}
		}
	}
}
