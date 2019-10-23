/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Threading;

namespace Spectrum
{
	/// <summary>
	/// Critical section protection type that enforces FIFO ordering for the threads that try to lock the protected
	/// section. Can be implemented as a spinlock for short critical sections, otherwise uses <see cref="Monitor"/> and
	/// thread sleeping.
	/// </summary>
	/// <remarks>Implements the Ticket Lock concept.</remarks>
	public sealed class FifoLock
	{
		#region Fields
		private readonly object _lock = new object();
		private readonly bool _spin;
		private long _lockValue = Int64.MinValue;
		private long _releaseValue = Int64.MinValue;
		#endregion // Fields

		/// <summary>
		/// Creates a new lock object.
		/// </summary>
		/// <param name="spinlock">If the lock function should use spinlocks - only for short critical sections.</param>
		public FifoLock(bool spinlock = false)
		{
			_spin = spinlock;
		}

		/// <summary>
		/// Enters the current thread into the Fifo queue, and blocks until it acquires the lock.
		/// </summary>
		public void Lock()
		{
			long ticket = Interlocked.Increment(ref _lockValue) - 1;
			if (_spin)
			{
				while (ticket != _releaseValue) ;
				return;
			}
			else
			{
				Monitor.Enter(_lock);
				while (true)
				{
					if (ticket == _releaseValue) return;
					else Monitor.Wait(_lock);
				}
			}
		}

		/// <summary>
		/// Releases the lock for the current thread, allowing the next waiting thread to acquire the lock.
		/// </summary>
		public void Unlock()
		{
			Interlocked.Increment(ref _releaseValue);
			if (_spin)
			{
				Monitor.PulseAll(_lock);
				Monitor.Exit(_lock);
			}
		}
	}
}
