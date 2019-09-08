/*
 * GNU LGPLv3 License - Copyright (c) The Spectrum Team
 * This file is subject to the terms and conditions of the GNU LGPLv3 license, the text of which can be found in the
 * 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/LGPL-3.0>.
 */
using System;
using System.Runtime.CompilerServices;

namespace Spectrum
{
	/// <summary>
	/// Represents a named source of messages, with an optional formatter attached. Also statically manages Logger
	/// instances and message forwarding to <see cref="ILogPolicy"/> instances.
	/// </summary>
	public class Logger
	{
		// The default logger for the application
		internal static Logger DefaultLogger { get; private set; } = null;

		#region Logging Functions
		/// <summary>
		/// Log a message using <see cref="MessageLevel.Info"/>.
		/// </summary>
		/// <param name="msg">The message text to log.</param>
		public void Info(string msg) { }

		/// <summary>
		/// Log a message using <see cref="MessageLevel.Warn"/>.
		/// </summary>
		/// <param name="msg">The message text to log.</param>
		public void Warn(string msg) { }

		/// <summary>
		/// Log a message using <see cref="MessageLevel.Error"/>.
		/// </summary>
		/// <param name="msg">The message text to log.</param>
		public void Error(string msg) { }

		/// <summary>
		/// Log an exception.
		/// </summary>
		/// <param name="e">The exception to log.</param>
		public void Exception(Exception e) { }
		#endregion // Logging Functions
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
		public static void INFO(string msg)
		{
			Logger.DefaultLogger?.Info(msg);
		}

		/// <summary>
		/// Logs a message to the default logger using <see cref="MessageLevel.Warn"/>.
		/// </summary>
		/// <param name="msg">The message text to log.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WARN(string msg)
		{
			Logger.DefaultLogger?.Warn(msg);
		}

		/// <summary>
		/// Logs a message to the default logger using <see cref="MessageLevel.Error"/>.
		/// </summary>
		/// <param name="msg">The message text to log.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ERROR(string msg)
		{
			Logger.DefaultLogger?.Error(msg);
		}

		/// <summary>
		/// Logs an exception to the default logger.
		/// </summary>
		/// <param name="e">The exception to log.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void EXCEPTION(Exception e)
		{
			Logger.DefaultLogger?.Exception(e);
		}
	}
}
