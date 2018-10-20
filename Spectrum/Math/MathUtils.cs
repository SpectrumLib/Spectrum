using System;
using System.Runtime.CompilerServices;

namespace Spectrum
{
	/// <summary>
	/// Contains general mathematical utilities that are not otherwise found in C# or its runtime library.
	/// </summary>
	public static class MathUtils
	{
		#region Clamping
		/// <summary>
		/// Clamps the value into the given range.
		/// </summary>
		/// <param name="val">The value to clamp.</param>
		/// <param name="lo">The low end of the clamp range.</param>
		/// <param name="hi">The high end of the clamp range.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte Clamp(byte val, byte lo, byte hi = byte.MaxValue) => val < lo ? lo : val > hi ? hi : val;
		/// <summary>
		/// Clamps the value into the given range.
		/// </summary>
		/// <param name="val">The value to clamp.</param>
		/// <param name="lo">The low end of the clamp range.</param>
		/// <param name="hi">The high end of the clamp range.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static sbyte Clamp(sbyte val, sbyte lo, sbyte hi = sbyte.MaxValue) => val < lo ? lo : val > hi ? hi : val;
		/// <summary>
		/// Clamps the value into the given range.
		/// </summary>
		/// <param name="val">The value to clamp.</param>
		/// <param name="lo">The low end of the clamp range.</param>
		/// <param name="hi">The high end of the clamp range.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static short Clamp(short val, short lo, short hi = short.MaxValue) => val < lo ? lo : val > hi ? hi : val;
		/// <summary>
		/// Clamps the value into the given range.
		/// </summary>
		/// <param name="val">The value to clamp.</param>
		/// <param name="lo">The low end of the clamp range.</param>
		/// <param name="hi">The high end of the clamp range.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ushort Clamp(ushort val, ushort lo, ushort hi = ushort.MaxValue) => val < lo ? lo : val > hi ? hi : val;
		/// <summary>
		/// Clamps the value into the given range.
		/// </summary>
		/// <param name="val">The value to clamp.</param>
		/// <param name="lo">The low end of the clamp range.</param>
		/// <param name="hi">The high end of the clamp range.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Clamp(int val, int lo, int hi = int.MaxValue) => val < lo ? lo : val > hi ? hi : val;
		/// <summary>
		/// Clamps the value into the given range.
		/// </summary>
		/// <param name="val">The value to clamp.</param>
		/// <param name="lo">The low end of the clamp range.</param>
		/// <param name="hi">The high end of the clamp range.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint Clamp(uint val, uint lo, uint hi = uint.MaxValue) => val < lo ? lo : val > hi ? hi : val;
		/// <summary>
		/// Clamps the value into the given range.
		/// </summary>
		/// <param name="val">The value to clamp.</param>
		/// <param name="lo">The low end of the clamp range.</param>
		/// <param name="hi">The high end of the clamp range.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long Clamp(long val, long lo, long hi = long.MaxValue) => val < lo ? lo : val > hi ? hi : val;
		/// <summary>
		/// Clamps the value into the given range.
		/// </summary>
		/// <param name="val">The value to clamp.</param>
		/// <param name="lo">The low end of the clamp range.</param>
		/// <param name="hi">The high end of the clamp range.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong Clamp(ulong val, ulong lo, ulong hi = ulong.MaxValue) => val < lo ? lo : val > hi ? hi : val;
		/// <summary>
		/// Clamps the value into the given range.
		/// </summary>
		/// <param name="val">The value to clamp.</param>
		/// <param name="lo">The low end of the clamp range.</param>
		/// <param name="hi">The high end of the clamp range.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Clamp(float val, float lo, float hi = float.MaxValue) => val < lo ? lo : val > hi ? hi : val;
		/// <summary>
		/// Clamps the value into the given range.
		/// </summary>
		/// <param name="val">The value to clamp.</param>
		/// <param name="lo">The low end of the clamp range.</param>
		/// <param name="hi">The high end of the clamp range.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double Clamp(double val, double lo, double hi = double.MaxValue) => val < lo ? lo : val > hi ? hi : val;
		/// <summary>
		/// Clamps the value into the given range.
		/// </summary>
		/// <param name="val">The value to clamp.</param>
		/// <param name="lo">The low end of the clamp range.</param>
		/// <param name="hi">The high end of the clamp range.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static decimal Clamp(decimal val, decimal lo, decimal hi = decimal.MaxValue) => val < lo ? lo : val > hi ? hi : val;
		#endregion // Clamping

		#region Comparisons
		/// <summary>
		/// Checks if two numbers are equal to each other within a small error.
		/// </summary>
		/// <param name="l">The first number to compare.</param>
		/// <param name="r">The second number to compare.</param>
		/// <param name="eps">The maximum difference to be considered equal.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool NearlyEqual(float l, float r, float eps = 1e-5f)
		{
			float diff = l - r;
			return (diff < 0 ? -diff : diff) <= eps;
		}
		/// <summary>
		/// Checks if two numbers are equal to each other within a small error.
		/// </summary>
		/// <param name="l">The first number to compare.</param>
		/// <param name="r">The second number to compare.</param>
		/// <param name="eps">The maximum difference to be considered equal.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool NearlyEqual(double l, double r, double eps = 1e-5)
		{
			double diff = l - r;
			return (diff < 0 ? -diff : diff) <= eps;
		}

		/// <summary>
		/// Checks if the passed integer is a power of two. Works for negative integers.
		/// </summary>
		/// <param name="l">The integer value to check.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPowerOfTwo(long l)
		{
			l = Math.Abs(l);
			return (l & (l - 1)) == 0;
		}
		/// <summary>
		/// Checks if a passed integer value is a power of two.
		/// </summary>
		/// <param name="l">The integer value to check.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPowerOfTwo(ulong l)
		{
			return (l & (l - 1)) == 0;
		}

		/// <summary>
		/// General version to check if an integer value is a power of another integer value.
		/// </summary>
		/// <param name="l">The integer value to check.</param>
		/// <param name="power">The integer value base power to check against.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPowerOf(long l, long power)
		{
			if (power == 2)
			{
				l = Math.Abs(l);
				return (l & (l - 1)) == 0;
			}
			else
			{
				double log = Math.Log10(l) / Math.Log10(power);
				return (log == Math.Floor(log));
			}
		}
		/// <summary>
		/// General version to check if an integer value is a power of another integer value.
		/// </summary>
		/// <param name="l">The integer value to check.</param>
		/// <param name="power">The integer value base power to check against.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPowerOf(ulong l, ulong power)
		{
			if (power == 2)
				return (l & (l - 1)) == 0;
			else
			{
				double log = Math.Log10(l) / Math.Log10(power);
				return (log == Math.Floor(log));
			}
		}
		#endregion // Comparisons
	}
}
