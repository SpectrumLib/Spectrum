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
		private uint _writeIndex = 0;
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
				if (_writeIndex >= _queueSize)
				{
					Array.Resize(ref _queue, (int)(_queueSize + INITIAL_POOL_SIZE));
					_queueSize += INITIAL_POOL_SIZE;
				}

				_queue[_writeIndex] = msg;
				++_writeIndex;
			}
		}

		public IEnumerable<string> GetMessages()
		{
			uint msgCount = 0;
			lock (_writeLock) { msgCount = _writeIndex; }

			if (msgCount > 0)
			{
				for (uint i = 0; i < msgCount; ++i)
				{
					yield return _queue[i];
				}

				lock (_writeLock)
				{
					uint rem = _writeIndex - msgCount;
					if (rem > 0)
						Array.Copy(_queue, msgCount, _queue, 0, rem);
					_writeIndex = rem;
				} 
			}
		}
	}
}
