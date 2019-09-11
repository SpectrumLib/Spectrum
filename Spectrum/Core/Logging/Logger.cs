/*
 * GNU LGPLv3 License - Copyright (c) The Spectrum Team
 * This file is subject to the terms and conditions of the GNU LGPLv3 license, the text of which can be found in the
 * 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/LGPL-3.0>.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Spectrum
{
	/// <summary>
	/// Represents a named source of messages, with an optional formatter attached. Also statically manages Logger
	/// instances and message forwarding to <see cref="ILogPolicy"/> instances. Logging policies are given unique
	/// integer identifiers to allow for filtering in loggers, and policy zero (0) is the default policy.
	/// </summary>
	public sealed class Logger
	{
		private static readonly Dictionary<string, Logger> _Loggers = new Dictionary<string, Logger>();
		private static readonly Dictionary<uint, ILogPolicy> _Policies = new Dictionary<uint, ILogPolicy>();
		private static uint _PolicyId = 0;
		private static readonly object _PolicyLock = new object();

		// The default logger for the application
		internal static Logger DefaultLogger { get; private set; } = null;

		#region Fields
		/// <summary>
		/// The logger name, unique among all Logger instances in the application.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// An 8-character tag for the logger.
		/// </summary>
		public ReadOnlySpan<char> Tag => _tag;
		private readonly char[] _tag;
		/// <summary>
		/// The optional message formatter to use for messages sent through this logger.
		/// </summary>
		public readonly IMessageFormatter Formatter;

		private readonly StringBuilder _buffer = new StringBuilder(256);
		private bool _attached = true;
		#endregion // Fields

		private Logger(string name, ReadOnlySpan<char> tag, IMessageFormatter formatter)
		{
			Name = name;
			tag.CopyTo((_tag = new char[8]).AsSpan());
			Formatter = formatter;
		}

		// Marks the logger as being detached (removed) from the logging system
		// While removal doesn't stop the logger from working, this is used to enforce a paradigm of RemoveLogger()
		//   calls being final
		private void detach() => _attached = false;

		#region Logging Functions
		/// <summary>
		/// Log a message using <see cref="MessageLevel.Info"/>.
		/// </summary>
		/// <param name="msg">The message text to log.</param>
		public void Info(string msg)
		{
			if (!_attached)
				throw new InvalidOperationException($"The logger '{Name}' is detached and cannot be used.");

			if (Formatter != null)
			{
				lock (_buffer)
				{
					_buffer.Clear();
					Formatter.Format(_buffer, this, MessageLevel.Info, DateTime.Now, msg.AsSpan());
					msg = _buffer.ToString();
				}
			}

			LogToPolicies(this, MessageLevel.Info, msg.AsSpan());
		}

		/// <summary>
		/// Log a message using <see cref="MessageLevel.Warn"/>.
		/// </summary>
		/// <param name="msg">The message text to log.</param>
		public void Warn(string msg)
		{
			if (!_attached)
				throw new InvalidOperationException($"The logger '{Name}' is detached and cannot be used.");

			if (Formatter != null)
			{
				lock (_buffer)
				{
					_buffer.Clear();
					Formatter.Format(_buffer, this, MessageLevel.Warn, DateTime.Now, msg.AsSpan());
					msg = _buffer.ToString();
				}
			}

			LogToPolicies(this, MessageLevel.Warn, msg.AsSpan());
		}

		/// <summary>
		/// Log a message using <see cref="MessageLevel.Error"/>.
		/// </summary>
		/// <param name="msg">The message text to log.</param>
		public void Error(string msg)
		{
			if (!_attached)
				throw new InvalidOperationException($"The logger '{Name}' is detached and cannot be used.");

			if (Formatter != null)
			{
				lock (_buffer)
				{
					_buffer.Clear();
					Formatter.Format(_buffer, this, MessageLevel.Error, DateTime.Now, msg.AsSpan());
					msg = _buffer.ToString();
				}
			}

			LogToPolicies(this, MessageLevel.Error, msg.AsSpan());
		}

		/// <summary>
		/// Log an exception.
		/// </summary>
		/// <param name="e">The exception to log.</param>
		public void Exception(Exception e)
		{
			if (!_attached)
				throw new InvalidOperationException($"The logger '{Name}' is detached and cannot be used.");

			string msg = "";
			if (Formatter != null)
			{
				lock (_buffer)
				{
					_buffer.Clear();
					Formatter.Format(_buffer, this, DateTime.Now, e);
					msg = _buffer.ToString();
				}
			}

			LogToPolicies(this, MessageLevel.Warn, msg.AsSpan());
		}
		#endregion // Logging Functions

		#region Static Logging
		private static void LogToPolicies(Logger logger, MessageLevel ml, ReadOnlySpan<char> msg)
		{
			lock (_PolicyLock)
			{
				foreach (var pol in _Policies.Values)
				{
					if ((pol.LevelMask & ml) > 0)
						pol.Write(logger, ml, msg);
				} 
			}
		}
		#endregion // Static Logging

		#region Loggers
		/// <summary>
		/// Checks if a logger with the given name exists in the registered logger list.
		/// </summary>
		/// <param name="name">The name to check for.</param>
		/// <returns>If the named logger exists.</returns>
		public static bool HasLogger(string name) => _Loggers.ContainsKey(name);

		/// <summary>
		/// Gets the named and registered logger.
		/// </summary>
		/// <param name="name">The name of the logger to get.</param>
		/// <returns>The named logger, or null if it doesn't exist.</returns>
		public static Logger GetLogger(string name) => _Loggers.TryGetValue(name, out var o) ? o : null;

		/// <summary>
		/// Creates a new named logger, optionally with a custom tag and formatter.
		/// </summary>
		/// <exception cref="InvalidOperationException">A logger with the name already exists.</exception>
		/// <param name="name">The name of the new logger.</param>
		/// <param name="tag">The 8 character tag, defaults to the name. Will be made 8 characters if needed.</param>
		/// <param name="fmtr">The optional formatter for messages logged with the new logger.</param>
		/// <returns>The new Logger instance.</returns>
		public static Logger CreateLogger(string name, string tag = null, IMessageFormatter fmtr = null)
		{
			if (String.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Cannot pass a null or empty logger name.", nameof(name));
			if (_Loggers.ContainsKey(name))
				throw new InvalidOperationException($"Cannot create duplicate Logger name '{name}'.");

			tag = tag ?? name;
			if (tag.Length < 8)
				tag = new string(' ', 8 - tag.Length) + tag;

			var lg = new Logger(name, tag.AsSpan(), fmtr);
			_Loggers.Add(name, lg);
			return lg;
		}

		/// <summary>
		/// Attempts to remove a named logger from the registered logger list. Attempting to log to a Logger after
		/// it has been removed will generate an exception.
		/// </summary>
		/// <param name="name">The name of the logger to remove.</param>
		/// <returns>If a logger with the given name was found an removed.</returns>
		public static bool RemoveLogger(string name)
		{
			if (_Loggers.Remove(name, out var lg))
			{
				lg.detach();
				return true;
			}
			return false;
		}
		#endregion // Loggers

		#region Policies
		/// <summary>
		/// Creates a new logging policy to handle the actual output operations on logged messages.
		/// </summary>
		/// <exception cref="ArgumentException">The arguments do not match any type constructor.</exception>
		/// <exception cref="InvalidOperationException">The type could not be constructed.</exception>
		/// <typeparam name="T">The logging policy type to instantiate.</typeparam>
		/// <param name="args">The arguments to pass to the policy constructor.</param>
		/// <returns>A unique identifier for the new policy.</returns>
		public static uint RegisterPolicy<T>(params object[] args)
			where T: class, ILogPolicy
		{
			// Find a valid constructor
			Type[] argtypes = args.Select(o => o.GetType()).ToArray();
			var cinfo = typeof(T).GetConstructor(argtypes);
			if (cinfo == null)
			{
				throw new ArgumentException($"There is no public constructor for the type '{typeof(T).Name}' that " +
					$"matches the passed argument list.", nameof(args));
			}

			// Attempt to call the constructor
			ILogPolicy pol = null;
			try
			{
				pol = cinfo.Invoke(args) as ILogPolicy;
			}
			catch (Exception e)
			{
				throw new InvalidOperationException($"Unable to create ILogPolicy of type {typeof(T)}, reason: " +
					$"'{(e.InnerException == null ? e.Message : e.InnerException.Message)}'");
			}

			// Initialize and save
			pol.Initialize();
			lock (_PolicyLock)
				_Policies.Add(_PolicyId++, pol);
			return _PolicyId - 1;
		}

		/// <summary>
		/// Removes the logging policy with the passed id.
		/// </summary>
		/// <param name="id">The id of the policy to remove.</param>
		/// <returns>If the policy with the id was found and removed.</returns>
		public static bool RemovePolicy(uint id)
		{
			if (id == 0)
				return false;

			lock (_PolicyLock)
			{
				if (_Policies.Remove(id, out var pol))
				{
					pol.Terminate();
					return true;
				}
				return false;
			}
		}
		#endregion // Policies
	}

	/// <summary>
	/// Provides direct access to the default <see cref="Logger"/> instance for the application. Designed to be used
	/// as <c>using static Spectrum.Log;</c>, to import the logging functions directly into the file scope.
	/// </summary>
	public static class Log
	{
		/// <summary>
		/// Logs a message to the default logger using <see cref="MessageLevel.Info"/>.
		/// </summary>
		/// <param name="msg">The message text to log.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void INFO(string msg) => Logger.DefaultLogger?.Info(msg);

		/// <summary>
		/// Logs a message to the default logger using <see cref="MessageLevel.Warn"/>.
		/// </summary>
		/// <param name="msg">The message text to log.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WARN(string msg) => Logger.DefaultLogger?.Warn(msg);

		/// <summary>
		/// Logs a message to the default logger using <see cref="MessageLevel.Error"/>.
		/// </summary>
		/// <param name="msg">The message text to log.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ERROR(string msg) => Logger.DefaultLogger?.Error(msg);

		/// <summary>
		/// Logs an exception to the default logger.
		/// </summary>
		/// <param name="e">The exception to log.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void EXCEPTION(Exception e) => Logger.DefaultLogger?.Exception(e);
	}
}
