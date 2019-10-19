/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Spectrum
{
	/// <summary>
	/// Base type for implementing logging output operations. They do not have to be externally threadsafe, as access 
	/// to all policies registered with <see cref="Logger"/> are externally locked.
	/// </summary>
	public abstract class LogPolicy
	{
		#region Fields
		/// <summary>
		/// The mask of logging levels that this policy should handle.
		/// </summary>
		public virtual MessageLevel LevelMask => MessageLevel.All;
		/// <summary>
		/// The unique id value assigned to this policy when it is registered with <see cref="Logger"/>. It will be
		/// a power of two, since it is meant to be used in a bit mask. If it is zero, then the policy has not been
		/// registered.
		/// </summary>
		public uint Id { get; internal set; } = 0;
		#endregion // Fields

		/// <summary>
		/// Called before the policy is first used to perform initialization.
		/// </summary>
		public abstract void Initialize();
		/// <summary>
		/// Called when the policy is being destroyed to perform cleanup.
		/// </summary>
		public abstract void Terminate();

		/// <summary>
		/// Log the provided message to the policy specific output. The level is checked against the mask before this
		/// function is called.
		/// </summary>
		/// <param name="logger">The logger that generated the message.</param>
		/// <param name="ml">The message level for the message.</param>
		/// <param name="msg">The message text to log.</param>
		public abstract void Write(Logger logger, MessageLevel ml, ReadOnlySpan<char> msg);

		/// <summary>
		/// Log the internal message to the policy specific output. This function is only called if the application
		/// has an attached internal logger.
		/// </summary>
		/// <param name="ml">The message level for the message.</param>
		/// <param name="msg">The message text to log.</param>
		public abstract void WriteInternal(MessageLevel ml, ReadOnlySpan<char> msg);
	}

	/// <summary>
	/// Represents an id mask of registered <see cref="LogPolicy"/> instances. Supports overloaded bitwise operations.
	/// </summary>
	public struct PolicyMask
	{
		/// <summary>
		/// A mask of all available policy ids.
		/// </summary>
		public static readonly PolicyMask All = new PolicyMask(~0u);

		/// <summary>
		/// The mask value.
		/// </summary>
		public uint Value { get; private set; }

		/// <summary>
		/// Creates a new mask from the given mask value.
		/// </summary>
		/// <param name="mask">The mask value.</param>
		public PolicyMask(uint mask)
		{
			Value = mask;
		}

		/// <summary>
		/// Adds a policy id to the mask.
		/// </summary>
		/// <param name="id">The value of <see cref="LogPolicy.Id"/> to add to the mask.</param>
		public void Add(uint id) => Value |= id;

		/// <summary>
		/// Removes a policy id from the mask.
		/// </summary>
		/// <param name="id">The value of <see cref="LogPolicy.Id"/> to remove from the mask.</param>
		public void Remove(uint id) => Value &= ~id;

		/// <summary>
		/// Sets the value of the mask.
		/// </summary>
		/// <param name="id">The mask value to set.</param>
		public void Set(uint id) => Value = id;

		/// <summary>
		/// Operator to check if the mask contains the id.
		/// </summary>
		/// <param name="mask">the mask to check.</param>
		/// <param name="id">The policy id to check for.</param>
		/// <returns>If the mask contains the policy id.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator & (in PolicyMask mask, uint id) => (mask.Value & id) == id;
	}

	/// <summary>
	/// Default logging policy, which writes all messages to a log file. Supports optional async writes to file.
	/// </summary>
	public sealed class FileLogPolicy : LogPolicy
	{
		private const int THREAD_SLEEP = 100; // 100 ms
		private const int QUEUE_SIZE = 128;

		#region Fields
		// Logging mask
		private MessageLevel _mask = MessageLevel.All;
		public override MessageLevel LevelMask => _mask;

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
		public FileLogPolicy(string file, bool async = true, bool timestamp = false, bool archive = true)
		{
			// Calculate the full path
			if (!PathUtils.IsValidPath(file))
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

		public override void Initialize()
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

		public override void Terminate()
		{
			_threadShouldExit = true;
			_waitEvent?.Set(); // Signal the thread early to wake up and check the exit condition
			_thread?.Join();

			_fileWriter.Flush();
			_fileWriter.Close();
			_fileWriter.Dispose();
		}

		public override void Write(Logger logger, MessageLevel ml, ReadOnlySpan<char> msg)
		{
			if (_thread != null)
			{
				lock (_queueLock)
					_msgQueue.Enqueue(msg.ToString());
			}
			else
				_fileWriter.WriteLine(msg);
		}

		public override void WriteInternal(MessageLevel ml, ReadOnlySpan<char> msg) => Write(null, ml, msg);

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
