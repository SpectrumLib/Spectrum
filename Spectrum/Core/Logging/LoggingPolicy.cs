using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Spectrum
{
	/// <summary>
	/// Used to implement custom logging functionality. See <see cref="FileLogPolicy"/> for an example as to how to
	/// implement a custom policy. Policies cannot not be shared between loggers.
	/// </summary>
	public interface ILogPolicy
	{
		/// <summary>
		/// The mask of logging levels that this policy should handle.
		/// </summary>
		LoggingLevel LevelMask { get; }

		/// <summary>
		/// Called when the logger is first initialized to perform startup for the policy.
		/// </summary>
		void Open();
		/// <summary>
		/// Called when the logger is closed.
		/// </summary>
		void Close();

		/// <summary>
		/// Called to handle a new message to the logger.
		/// </summary>
		/// <param name="logger">The logger that generated the message.</param>
		/// <param name="ll">The level of the message, will only be one type, not a mask.</param>
		/// <param name="message">The message text, pre-formatted with extra information.</param>
		void Write(Logger logger, LoggingLevel ll, string message);
	}


	/// <summary>
	/// Default implementation of a log policy that writes to a log file. Used as the default policy by the library
	/// if the user does not specify a custom policy. Has the ability to make asynchronous writes to the file.
	/// Also supports timestamping the log file, as well as maintaining a limited history of log files.
	/// </summary>
	public sealed class FileLogPolicy : ILogPolicy
	{
		private const int THREAD_SLEEP = 100;

		#region Fields
		// Text writer for the file
		private StreamWriter _writer = null;

		// Threading members
		private MessagePool _pool;
		private Thread _thread;
		private bool _thread_should_exit = false;

		/// <summary>
		/// The absolute path to the log file written to by this policy.
		/// </summary>
		public readonly string FilePath;

		private LoggingLevel _level;
		LoggingLevel ILogPolicy.LevelMask => _level;
		#endregion // Fields

		/// <summary>
		/// Creates a new policy to write messages to the passed file.
		/// </summary>
		/// <param name="fileName">The path to the file (without the optional timestamp), without an extension.</param>
		///	<param name="level">The mask of message levels for this policy to process.</param>
		///	<param name="threading">If the policy should make asynchronous file writes.</param>
		///	<param name="timestamp">If the policy should add a timestamp to the file name.</param>
		///	<param name="history">The number of timestamped files to keep before deleting the oldest ones.</param>
		public FileLogPolicy(string fileName, LoggingLevel level, bool threading = false, bool timestamp = false, byte history = 0)
		{
			// Chop off the extension
			fileName = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), fileName));
			string baseName = Path.GetFileNameWithoutExtension(fileName);
			string dirName = Path.GetDirectoryName(fileName);
			fileName = fileName.Substring(0, fileName.Length - Path.GetExtension(fileName).Length);
			// Add the timestamp, if needed
			if (timestamp)
			{
				string tstamp = DateTime.Now.ToString(".yyMMdd_HHmmss");
				fileName += tstamp;
			}
			// Add the log extension
			fileName += ".log";
			// Make sure the directory exists
			if (!Directory.Exists(dirName))
				Directory.CreateDirectory(dirName);

			// Check and clean the history
			if (timestamp)
			{
				var lfhist = Directory.GetFiles(dirName, $"{baseName}*.log")
					.Select(Path.GetFullPath).OrderBy(path => new FileInfo(path).CreationTime).ToArray();
				if (lfhist.Length > history)
				{
					int toremove = lfhist.Length - history;
					foreach (var path in lfhist.Take(toremove))
						File.Delete(path);
				}
			}

			// Set up threading if needed
			if (threading)
			{
				_pool = new MessagePool();
				_thread = new Thread(_thread_func);
			}

			FilePath = fileName;
			_level = level;
		}

		void ILogPolicy.Open()
		{
			if (_writer != null)
				return;

			_writer = new StreamWriter(
				File.Open(FilePath, FileMode.Create, FileAccess.Write, FileShare.Write), System.Text.Encoding.UTF8
			);
			_thread?.Start();
		}

		void ILogPolicy.Close()
		{
			if (_thread != null)
			{
				_thread_should_exit = true;
				_thread.Join();
			}

			_writer.Flush();
			_writer.Close();
			_writer.Dispose();
		}

		private void _thread_func()
		{
			while (true)
			{
				Thread.Sleep(THREAD_SLEEP);

				foreach (var msg in _pool.GetMessages())
					_writer.WriteLine(msg);

				if (_thread_should_exit)
					break;
			}
		}

		void ILogPolicy.Write(Logger logger, LoggingLevel ll, string message)
		{
			if (_thread != null)
				_pool.AddMessage(message);
			else
				_writer.WriteLine(message);
		}
	}
}
