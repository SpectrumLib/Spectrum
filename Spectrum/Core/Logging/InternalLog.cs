/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Spectrum
{
	// Manages the internal logging functionality, if requested by the application
	internal static class InternalLog
	{
		private delegate void LogCallback(MessageLevel ml, ReadOnlySpan<char> message);

		#region Logging
		// Logging objects
		private static IMessageFormatter _Formatter;
		private static StringBuilder _Buffer;
		private static object _BufferLock;

		// Callback that is registered to perform logging -IF- the internal logger is attached
		private static LogCallback _LogCallback = null;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void IINFO(string msg) => _LogCallback?.Invoke(MessageLevel.Info, msg.AsSpan());
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void IWARN(string msg) => _LogCallback?.Invoke(MessageLevel.Warn, msg.AsSpan());
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void IERROR(string msg) => _LogCallback?.Invoke(MessageLevel.Error, msg.AsSpan());
		#endregion // Logging

		internal static void Initialize(IMessageFormatter fmt)
		{
			_Formatter = fmt ?? new DefaultMessageFormatter();
			_Buffer = new StringBuilder(256);
			_BufferLock = new object();

			_LogCallback = (ml, msg) =>
			{
				lock (_BufferLock)
				{
					_Buffer.Clear();
					_Formatter.FormatInternal(_Buffer, ml, DateTime.Now, msg);
					Logger.LogInternal(ml, _Buffer.ToString().AsSpan());
				}
			};
		}
	}
}
