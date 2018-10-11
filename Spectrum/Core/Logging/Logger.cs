using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Spectrum
{
	/// <summary>
	/// Represents a named source of messages, with an optional pre-formatter attached. Also statically controls
	/// the lifetimes of loggers, as well as the forwarding of messages onto the registered policies.
	/// 
	/// Logging with Spectrum works by collecting messages logged from multiple named loggers, processing them,
	/// and then forwarding them to logging policies which take care of the message output. There is a base
	/// formatter which formats all messages in the library before processing, and individual logger instances
	/// are allowed to register their own formatter which preprocesses messaged before they are seen by the central
	/// formatter. There is a logger that is internal to the library, which only the library can use, and there is
	/// a default logger for the application that is created automatically. The library can also create default
	/// formatters and policies that can be controlled using the parameters passed to the application.
	/// </summary>
	public class Logger
	{
		private static readonly Dictionary<string, Logger> s_loggers = new Dictionary<string, Logger>();
		private static readonly Dictionary<uint, ILogPolicy> s_policies = new Dictionary<uint, ILogPolicy>();
		private static uint s_policyId = 0;
		private static readonly object s_policyLock = new object();
		/// <summary>
		/// The formatter used to format all logged messages before being sent onto the registered policies.
		/// </summary>
		public static ILogFormatter BaseFormatter { get; private set; } = null;
		// String builder to create log messages, with arbitrary initial size
		private static readonly StringBuilder s_logBuilder = new StringBuilder(512);

		/// <summary>
		/// The default logger for the application.
		/// </summary>
		public static Logger DefaultLogger { get; private set; } = null;

		#region Fields
		/// <summary>
		/// The name of the logger, uniquely identifying it in the application list of loggers.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// The tag for the logger, as it will appear in the default message formatter. Will always have a length of 8,
		/// and by default is the name either truncated or left-filled with spaces.
		/// </summary>
		public readonly string Tag;

		/// <summary>
		/// The optional preformatter to transform the logged messages before being sent to the logging system.
		/// This preformatter will not be called for exceptions (i.e. only <see cref="ILogFormatter.FormatMessage"/>
		/// will be called).
		/// </summary>
		public readonly ILogFormatter PreFormatter;
		// If there is a preformatter, this are used to preformat the message
		private readonly StringBuilder _logBuilder;
		#endregion // Fields

		private Logger(string name, string tag, ILogFormatter pref)
		{
			Name = name;
			tag = tag ?? name;
			Tag = (tag.Length >= 8) ? tag.Substring(0, 8) : new string(' ', 8 - tag.Length) + tag;
			PreFormatter = pref;
			if (pref != null)
				_logBuilder = new StringBuilder(512);
		}

		#region Instance Logging
		/// <summary>
		/// Logs a message with a level of <see cref="LoggingLevel.Debug"/>.
		/// </summary>
		/// <param name="msg">The message to log.</param>
		public void Debug(string msg)
		{
			if (PreFormatter != null)
			{
				lock (_logBuilder)
				{
					_logBuilder.Clear();
					PreFormatter.FormatMessage(_logBuilder, this, LoggingLevel.Debug, msg);
					msg = _logBuilder.ToString();
				}
			}

			LogMessage(this, LoggingLevel.Debug, msg);
		}

		/// <summary>
		/// Logs a message with a level of <see cref="LoggingLevel.Info"/>.
		/// </summary>
		/// <param name="msg">The message to log.</param>
		public void Info(string msg)
		{
			if (PreFormatter != null)
			{
				lock (_logBuilder)
				{
					_logBuilder.Clear();
					PreFormatter.FormatMessage(_logBuilder, this, LoggingLevel.Info, msg);
					msg = _logBuilder.ToString();
				}
			}

			LogMessage(this, LoggingLevel.Info, msg);
		}

		/// <summary>
		/// Logs a message with a level of <see cref="LoggingLevel.Warn"/>.
		/// </summary>
		/// <param name="msg">The message to log.</param>
		public void Warn(string msg)
		{
			if (PreFormatter != null)
			{
				lock (_logBuilder)
				{
					_logBuilder.Clear();
					PreFormatter.FormatMessage(_logBuilder, this, LoggingLevel.Warn, msg);
					msg = _logBuilder.ToString();
				}
			}

			LogMessage(this, LoggingLevel.Warn, msg);
		}

		/// <summary>
		/// Logs a message with a level of <see cref="LoggingLevel.Error"/>.
		/// </summary>
		/// <param name="msg">The message to log.</param>
		public void Error(string msg)
		{
			if (PreFormatter != null)
			{
				lock (_logBuilder)
				{
					_logBuilder.Clear();
					PreFormatter.FormatMessage(_logBuilder, this, LoggingLevel.Error, msg);
					msg = _logBuilder.ToString();
				}
			}

			LogMessage(this, LoggingLevel.Error, msg);
		}

		/// <summary>
		/// Logs a message with a level of <see cref="LoggingLevel.Fatal"/>.
		/// </summary>
		/// <param name="msg">The message to log.</param>
		public void Fatal(string msg)
		{
			if (PreFormatter != null)
			{
				lock (_logBuilder)
				{
					_logBuilder.Clear();
					PreFormatter.FormatMessage(_logBuilder, this, LoggingLevel.Fatal, msg);
					msg = _logBuilder.ToString();
				}
			}

			LogMessage(this, LoggingLevel.Fatal, msg);
		}

		/// <summary>
		/// Logs a formatted exception.
		/// </summary>
		/// <param name="e">The exception for format and log.</param>
		public void Exception(Exception e)
		{
			LogException(this, e);
		}
		#endregion // Instance Logging

		#region Loggers/Policies
		/// <summary>
		/// Gets if a logger with the passed name already exists.
		/// </summary>
		/// <param name="name">The name to check for.</param>
		/// <returns>If the named logger already exists.</returns>
		public static bool HasLogger(string name) => s_loggers.ContainsKey(name);

		/// <summary>
		/// Gets the logger with the passed name.
		/// </summary>
		/// <param name="name">The name to get.</param>
		/// <returns>The named logger, or null if it doesn't exist.</returns>
		public static Logger GetLogger(string name) => s_loggers.ContainsKey(name) ? s_loggers[name] : null;

		/// <summary>
		/// Creates a new named logger with the passed parameters. Throws an exception if a logger with the passed
		/// name already exists.
		/// </summary>
		/// <param name="name">The name of the logger to use.</param>
		/// <param name="tag">The optional tag for the default formatter, null defaults to adjusted name.</param>
		/// <param name="pref">The optional pre-formatter to use to format messages before they are forwarded.</param>
		/// <returns>The new logger instance.</returns>
		public static Logger CreateLogger(string name, string tag = null, ILogFormatter pref = null)
		{
			if (String.IsNullOrWhiteSpace(name))
				throw new ArgumentException("The logger name cannot be null or whitespace.", nameof(name));
			if (s_loggers.ContainsKey(name))
				throw new ArgumentException($"A logger with the name '{name}' already exists", nameof(name));

			Logger l = new Logger(name, tag, pref);
			s_loggers.Add(name, l);
			return l;
		}

		/// <summary>
		/// Removes a named logger.
		/// </summary>
		/// <param name="name">The name of the logger to remove.</param>
		/// <returns>If a logger with the passed name was found and removed.</returns>
		public static bool RemoveLogger(string name)
		{
			if (!s_loggers.ContainsKey(name))
				return false;

			s_loggers.Remove(name);
			return true;
		}

		/// <summary>
		/// Creates a new policy of the given type, with the passed args, and opens it. All messages logged after 
		/// a policy is registered will be passed to it. Throws an exception if there is no constructor for the type
		/// that matches the passed arguments.
		/// </summary>
		/// <typeparam name="T">The type of policy to create and open.</typeparam>
		/// <param name="args">The args to pass to the policy constructor.</param>
		/// <returns>A unique identifier for the policy.</returns>
		public static uint RegisterPolicy<T>(params object[] args)
			where T : class, ILogPolicy
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
			ILogPolicy policy = null;
			try
			{
				policy = cinfo.Invoke(args) as ILogPolicy;
			}
			catch (Exception e)
			{
				throw new InvalidOperationException($"Unable to create ILogPolicy of type {typeof(T)}, reason: " +
					$"'{(e.InnerException == null ? e.Message : e.InnerException.Message)}'");
			}

			// Open, save, return
			policy.Open();
			lock (s_policyLock)
			{
				s_policies.Add(s_policyId, policy);
				return s_policyId++;
			}
		}

		/// <summary>
		/// Removes the policy with the passed id from the logging system. Cannot pass 0, as that is the default log
		/// policy which cannot be removed. The removed policy has <see cref="ILogPolicy.Close"/> called on removal.
		/// </summary>
		/// <param name="id">The id of the policy to remove.</param>
		/// <returns>If a policy with the id, that is not the default policy, was found and removed.</returns>
		public static bool RemovePolicy(uint id)
		{
			if (id == 0)
				return false;

			if (s_policies.ContainsKey(id))
			{
				lock (s_policyLock)
				{
					s_policies[id].Close();
					s_policies.Remove(id);
					return true; 
				}
			}
			return false;
		}
		#endregion // Loggers/Policies

		#region Static Logging
		// Performs formatting, then posts the message to the registered logging policies
		// If the logger has a preformatter, it will have already been called
		private static void LogMessage(Logger logger, LoggingLevel ll, string msg)
		{
			string fmsg = null;
			lock (s_logBuilder)
			{
				s_logBuilder.Clear();
				BaseFormatter.FormatMessage(s_logBuilder, logger, ll, msg);
				fmsg = s_logBuilder.ToString();
			}

			lock (s_policyLock)
			{
				foreach (var pol in s_policies.Values)
				{
					if ((pol.LevelMask & ll) > 0)
						pol.Write(logger, ll, fmsg);
				}
			}
		}

		// Note: The preformatter for the logger is never run on exceptions
		private static void LogException(Logger logger, Exception e)
		{
			string fmsg = null;
			lock (s_logBuilder)
			{
				s_logBuilder.Clear();
				BaseFormatter.FormatException(s_logBuilder, logger, e);
				fmsg = s_logBuilder.ToString();
			}

			lock (s_policyLock)
			{
				foreach (var pol in s_policies.Values)
				{
					if ((pol.LevelMask & LoggingLevel.Exception) > 0)
						pol.Write(logger, LoggingLevel.Exception, fmsg);
				}
			}
		}
		#endregion Static Logging

		internal static void Initialize(in AppParameters pars)
		{
			BaseFormatter = pars.DefaultLoggingFormatter ?? new DefaultLogFormatter();
			if (pars.DefaultLoggingPolicy != null)
			{
				pars.DefaultLoggingPolicy.Open();
				s_policies.Add(s_policyId++, pars.DefaultLoggingPolicy);
			}
			else
			{
				RegisterPolicy<FileLogPolicy>(
					Path.Combine(pars.LogFileDirectory, pars.LogFileBaseName),
					LoggingLevel.All,
					pars.UseThreadedLogging,
					pars.LogFileTimestamp,
					pars.LogFileHistorySize
				);
			}

			DefaultLogger = CreateLogger("default", pars.DefaultLoggerTag);
			InternalLog.Prepare(new Logger("internal", "spectrum", null), pars.LibraryMessageMask);
		}

		internal static void Shutdown()
		{
			lock (s_logBuilder)
			{
				lock (s_policyLock)
				{
					s_loggers.Clear();
					foreach (var pol in s_policies.Values)
						pol.Close();
					s_policies.Clear();
					DefaultLogger = null;
				}
			}
		}
	}
}
