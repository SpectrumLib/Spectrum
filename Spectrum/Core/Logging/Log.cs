using System;
using System.Runtime.CompilerServices;

namespace Spectrum
{
	/// <summary>
	/// Provides direct access to the default application logger without having to deal with <see cref="Logger"/>
	/// instances. Designed to be used with <c>using static Spectrum.Log;</c> to have direct access to the functions.
	/// </summary>
	public static class Log
	{
		/// <summary>
		/// Logs a message to the default logger with the level <see cref="LoggingLevel.Debug"/>.
		/// </summary>
		/// <param name="msg">The message to log.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LDEBUG(string msg)
		{
			Logger.DefaultLogger?.Debug(msg);
		}

		/// <summary>
		/// Logs a message to the default logger with the level <see cref="LoggingLevel.Info"/>.
		/// </summary>
		/// <param name="msg">The message to log.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LINFO(string msg)
		{
			Logger.DefaultLogger?.Info(msg);
		}

		/// <summary>
		/// Logs a message to the default logger with the level <see cref="LoggingLevel.Warn"/>.
		/// </summary>
		/// <param name="msg">The message to log.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LWARN(string msg)
		{
			Logger.DefaultLogger?.Warn(msg);
		}

		/// <summary>
		/// Logs a message to the default logger with the level <see cref="LoggingLevel.Error"/>.
		/// </summary>
		/// <param name="msg">The message to log.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LERROR(string msg)
		{
			Logger.DefaultLogger?.Error(msg);
		}

		/// <summary>
		/// Logs a message to the default logger with the level <see cref="LoggingLevel.Fatal"/>.
		/// </summary>
		/// <param name="msg">The message to log.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LFATAL(string msg)
		{
			Logger.DefaultLogger?.Fatal(msg);
		}

		/// <summary>
		/// Logs a formatted exception to the default logger.
		/// </summary>
		/// <param name="e">The exception to format and log.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LEXCEPTION(Exception e)
		{
			Logger.DefaultLogger?.Exception(e);
		}
	}

	// Just like the Log class above, but directs messages to the library logger
	// This class must be used for all logging from the library
	internal static class InternalLog
	{
		private static Logger s_logger;
		private static LoggingLevel s_mask;

		public static void Prepare(Logger logger, LoggingLevel mask)
		{
			s_logger = logger;
			s_mask = mask;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LDEBUG(string msg)
		{
			if ((s_mask & LoggingLevel.Debug) > 0)
				s_logger?.Debug(msg);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LINFO(string msg)
		{
			if ((s_mask & LoggingLevel.Info) > 0)
				s_logger?.Info(msg);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LWARN(string msg)
		{
			if ((s_mask & LoggingLevel.Warn) > 0)
				s_logger?.Warn(msg);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LERROR(string msg)
		{
			if ((s_mask & LoggingLevel.Error) > 0)
				s_logger?.Error(msg);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LFATAL(string msg)
		{
			if ((s_mask & LoggingLevel.Fatal) > 0)
				s_logger?.Fatal(msg);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LEXCEPTION(Exception e)
		{
			if ((s_mask & LoggingLevel.Exception) > 0)
				s_logger?.Exception(e);
		}
	}
}
