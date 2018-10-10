using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Spectrum
{
	/// <summary>
	/// Core class for handling message logging functionality in the library. Creates and manages logger instances, and
	/// allows for easy and direct access to the default logger. Functions in this class are thread-safe.
	/// </summary>
	public static class Logging
	{
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
