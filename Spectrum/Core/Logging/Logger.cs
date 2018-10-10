using System;

namespace Spectrum
{
	/// <summary>
	/// Performs the actual formatting and passing of messages to <see cref="ILogPolicy"/> instances. Each logger
	/// represents a set of logging policies govered by a message mask. Logger instances are thread-safe.
	/// </summary>
	public sealed class Logger : IDisposable
	{
		private const uint THREAD_SLEEP = 100; // Threaded loggers sleep for X ms between writes

		~Logger()
		{
			dispose(false);
		}

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{

		}
		#endregion // IDisposable
	}
}
