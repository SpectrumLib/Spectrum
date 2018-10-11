using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Spectrum
{
	/// <summary>
	/// Core class for handling message logging functionality in the library. Creates and manages logger instances, and
	/// allows for easy and direct access to the default logger. Functions in this class are thread-safe.
	/// 
	/// The logging in Spectrum works by mapping multiple producers (<see cref="Logger"/>) and multiple consumers
	/// (<see cref="ILogPolicy"/>) to work with a single pool of application-wide messages. However, most applications
	/// will have a single default logger and policy, which works just fine.
	/// </summary>
	public static class Logging
	{
		#region Fields
		// Logging system members
		private static Dictionary<string, Logger> s_loggers;
		private static ILogFormatter s_formatter;
		private static List<ILogPolicy> s_policies;
		private static bool s_allowFormatOverrides;
		#endregion // Fields

		internal static void Initialize(ILogFormatter formatter, ILogPolicy defaultPolicy, bool allowFormatOverrides)
		{
			s_loggers = new Dictionary<string, Logger>();
			s_formatter = formatter;
			s_policies = new List<ILogPolicy>();
			s_policies.Add(defaultPolicy);
			defaultPolicy.Open();
			s_allowFormatOverrides = allowFormatOverrides;
		}
		
		internal static void Shutdown()
		{
			s_loggers.Clear();
			s_formatter = null;
			foreach (var p in s_policies)
				p.Close();
			s_policies.Clear();
		}

		#region Tag Generation
		/// <summary>
		/// Puts the time as a tag into the string builder, in the format <c>HH:MM:SS</c>, using 24-hour time.
		/// </summary>
		/// <param name="sb">The StringBuilder to put the time tag into.</param>
		/// <param name="time">The time to put, or null to use the current time.</param>
		public static void PutTimeTag(StringBuilder sb, DateTime? time = null)
		{
			DateTime dt = time.HasValue ? time.Value : DateTime.Now;

			char[] tag = new char[8] { '0', '0', ':', '0', '0', ':', '0', '0' };
			tag[0] = (char)('0' + (dt.Hour / 10));
			tag[1] = (char)('0' + (dt.Hour % 10));
			tag[3] = (char)('0' + (dt.Minute / 10));
			tag[4] = (char)('0' + (dt.Minute % 10));
			tag[6] = (char)('0' + (dt.Second / 10));
			tag[7] = (char)('0' + (dt.Second % 10));

			sb.Append(tag);
		}

		/// <summary>
		/// Gets a string representation of the logging level.
		/// </summary>
		/// <param name="lvl">The logging level to get the tag for.</param>
		/// <returns>The tag as a single character.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static char GetLevelTag(this LoggingLevel lvl)
		{
			switch (lvl)
			{
				case LoggingLevel.Debug: return 'D';
				case LoggingLevel.Info: return 'I';
				case LoggingLevel.Warn: return 'W';
				case LoggingLevel.Error: return 'E';
				case LoggingLevel.Fatal: return 'F';
				case LoggingLevel.Exception: return 'X';
				default: return '?';
			}
		}
		#endregion
	}
}
