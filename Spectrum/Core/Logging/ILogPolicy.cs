/*
 * GNU LGPLv3 License - Copyright (c) The Spectrum Team
 * This file is subject to the terms and conditions of the GNU LGPLv3 license, the text of which can be found in the
 * 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/LGPL-3.0>.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Spectrum
{
	/// <summary>
	/// Describes types that implement logging output operations. They do not have to be externally threadsafe, as
	/// access to all policies registered with <see cref="Logger"/> are externally locked.
	/// </summary>
	public interface ILogPolicy
	{
		/// <summary>
		/// The mask of logging levels that this policy should handle.
		/// </summary>
		MessageLevel LevelMask => MessageLevel.All;

		/// <summary>
		/// Called before the policy is first used to perform initialization.
		/// </summary>
		void Initialize();
		/// <summary>
		/// Called when the policy is being destroyed to perform cleanup.
		/// </summary>
		void Terminate();

		/// <summary>
		/// Log the provided message to the policy specific output. The level is checked against the mask before this
		/// function is called.
		/// </summary>
		/// <param name="logger">The logger that generated the message.</param>
		/// <param name="ml">The message level for the message.</param>
		/// <param name="msg">The message text to log.</param>
		void Write(Logger logger, MessageLevel ml, ReadOnlySpan<char> msg);
	}

	/// <summary>
	/// Default logging policy, which writes all messages to a log file. Supports optional async writes to file.
	/// </summary>
	public sealed class FileLogPolicy : ILogPolicy
	{
		private const int THREAD_SLEEP = 100; // 100 ms
		private const int QUEUE_SIZE = 128;

		#region Fields
		// Logging mask
		private MessageLevel _mask = MessageLevel.All;
		MessageLevel ILogPolicy.LevelMask => _mask;

		// File stream
		private StreamWriter _fileWriter;
		private bool _archive;

		// Threading objects
		private Thread _thread = null;
		private Queue<string> _msgQueue = null;
		private ManualResetEvent _waitEvent = null;
		private object _queueLock = null;
		private bool _threadShouldExit;
		
		/// <summary>
		/// The full path to the file that this policy is logging to.
		/// </summary>
		public readonly string FilePath;
		#endregion // Fields

		/// <summary>
		/// Creates a new file log policy with the given options.
		/// </summary>
		/// <param name="file">The name of the file to use (without an extension). Absolute paths will be respected.</param>
		/// <param name="async">If the policy should use asynchronous file writes.</param>
		/// <param name="timestamp">If a timestamp should be added to the file name.</param>
		/// <param name="archive">If an old log file of the same name exists, <c>true</c> will rename and not delete it.</param>
		public FileLogPolicy(string file, bool async = false, bool timestamp = false, bool archive = true)
		{
			// Calculate the full path
			if (!Uri.IsWellFormedUriString(file, UriKind.RelativeOrAbsolute))
				throw new ArgumentException($"Invalid path for log file '{file}'.", nameof(file));
			var fpath = Path.GetFullPath(Path.IsPathRooted(file) ? file : Path.Combine(Directory.GetCurrentDirectory(), file));

			// Edit the file name (depending on the settings)
			var dir = Path.GetDirectoryName(fpath);
			var fname = Path.GetFileName(fpath);
			var ext = Path.GetExtension(fpath);
			if (timestamp)
				fname += DateTime.Now.ToString(".yyMMdd_HHmmss");
			FilePath = Path.Combine(dir, fname + (String.IsNullOrEmpty(ext) ? ".log" : ext));
			_archive = archive && File.Exists(FilePath);

			// Prepare the other objects
			if (async)
			{
				_thread = new Thread(thread_func);
				_thread.Name = "LoggerThread";
				_msgQueue = new Queue<string>(QUEUE_SIZE);
				_waitEvent = new ManualResetEvent(false);
				_queueLock = new object();
			}
		}

		void ILogPolicy.Initialize()
		{
			if (_fileWriter != null)
				return;

			// Perform directory creation and archiving
			var oldfi = new FileInfo(FilePath);
			if (_archive)
			{
				var fname = oldfi.FullName.Substring(0, oldfi.FullName.LastIndexOf('.')) +
					oldfi.CreationTime.ToString(".yyMMdd_HHmmss") + oldfi.Extension;
				oldfi.MoveTo(fname, true);
			}
			else if (!oldfi.Directory.Exists)
				oldfi.Directory.Create();
			
			// Open the file, launch the thread if needed
			_fileWriter = new StreamWriter(File.Open(FilePath, FileMode.Create, FileAccess.Write, FileShare.None));
			_threadShouldExit = false;
			_thread?.Start();
		}

		void ILogPolicy.Terminate()
		{
			_threadShouldExit = true;
			_waitEvent?.Set(); // Signal the thread early to wake up and check the exit condition
			_thread?.Join();

			_fileWriter.Flush();
			_fileWriter.Close();
			_fileWriter.Dispose();
		}

		void ILogPolicy.Write(Logger logger, MessageLevel ml, ReadOnlySpan<char> msg)
		{
			if (_thread != null)
			{
				lock (_queueLock)
					_msgQueue.Enqueue(msg.ToString());
			}
			else
				_fileWriter.WriteLine(msg);
		}

		// Wait loop and file flush thread function
		private void thread_func()
		{
			var writeQueue = new Queue<string>(QUEUE_SIZE);

			void flush()
			{
				// Swap the queues to allow reading without locking
				lock (_queueLock)
				{
					var tmp = _msgQueue;
					_msgQueue = writeQueue;
					writeQueue = tmp;
				}

				// Log each message
				string msg;
				while (writeQueue.TryDequeue(out msg))
					_fileWriter.WriteLine(msg);
				_fileWriter.Flush();
			}

			while (!_threadShouldExit)
			{
				_waitEvent.WaitOne(THREAD_SLEEP);
				flush();
			}

			// Perform a final flush to finish writing any queued messages
			flush();
		}

		/// <summary>
		/// Sets the level mask for the policy.
		/// </summary>
		/// <param name="mask">The mask to select messages with.</param>
		public void SetMask(MessageLevel mask) => _mask = mask;
	}
}
