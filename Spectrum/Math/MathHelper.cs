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
	/// Contains math utility functionality that does not appear in the standard library math classes.
	/// </summary>
	public static class MathHelper
	{
		internal const float MAX_REL_EPS_F = Single.Epsilon * 10;
		internal const double MAX_REL_EPS_D = Double.Epsilon * 10;

		#region Floating Point Utils
		/// <summary>
		/// Gives the number of Units-of-Last-Place between the two float values (the number of representable floating
		/// point values between the two values).
		/// </summary>
		/// <remarks>This works because adjacent floats have adjacent integer values in IEEE.</remarks>
		/// <param name="f1">The first value.</param>
		/// <param name="f2">The second value.</param>
		/// <returns>The ULP distance of the two values, a negative number implies f1 < f2.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static long ULPDistance(float f1, float f2)
		{
			long i1 = *(int*)&f1, i2 = *(int*)&f2;
			return i1 - i2;
		}

		/// <summary>
		/// Gives the number of Units-of-Last-Place between the two double values (the number of representable floating
		/// point values between the two values).
		/// </summary>
		/// <remarks>This works because adjacent floats have adjacent integer values in IEEE.</remarks>
		/// <param name="f1">The first value.</param>
		/// <param name="f2">The second value.</param>
		/// <returns>The ULP distance of the two values, a negative number implies f1 < f2.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static long ULPDistance(double f1, double f2)
		{
			long i1 = *(long*)&f1, i2 = *(long*)&f2;
			return i1 - i2;
		}

		/// <summary>
		/// Checks if the two floats are adjacent in their bit representations, or the same value. This corresponds to a
		/// ULP distance of 0 or 1.
		/// </summary>
		/// <param name="f1">The first value.</param>
		/// <param name="f2">The second value.</param>
		/// <returns>If the values are the same, or only 1 ULP away.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool AdjacentOrSame(float f1, float f2) => 
			(f1 == f2) || ((Math.Sign(f1) == Math.Sign(f2)) && Math.Abs(ULPDistance(f1, f2)) <= 1);

		/// <summary>
		/// Checks if the two floats are adjacent in their bit representations, or the same value. This corresponds to a
		/// ULP distance of 0 or 1.
		/// </summary>
		/// <param name="f1">The first value.</param>
		/// <param name="f2">The second value.</param>
		/// <returns>If the values are the same, or only 1 ULP away.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool AdjacentOrSame(double f1, double f2) =>
			(f1 == f2) || ((Math.Sign(f1) == Math.Sign(f2)) && Math.Abs(ULPDistance(f1, f2)) <= 1);

		/// <summary>
		/// Checks if the two values are nearly equal to each other given their relative difference.
		/// </summary>
		/// <param name="f1">The first value.</param>
		/// <param name="f2">The second value.</param>
		/// <param name="eps">The maximum relative difference to be considered equal.</param>
		/// <returns>If the values are equal within a relative error.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool NearlyEqual(float f1, float f2, float eps = MAX_REL_EPS_F)
		{
			float d = Math.Abs(f1 - f2);
			float l = Math.Max(Math.Abs(f1), Math.Abs(f2));
			return d <= (l * eps);
		}

		/// <summary>
		/// Checks if the two values are nearly equal to each other given their relative difference.
		/// </summary>
		/// <param name="f1">The first value.</param>
		/// <param name="f2">The second value.</param>
		/// <param name="eps">The maximum relative difference to be considered equal.</param>
		/// <returns>If the values are equal within a relative error.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool NearlyEqual(double f1, double f2, double eps = MAX_REL_EPS_D)
		{
			double d = Math.Abs(f1 - f2);
			double l = Math.Max(Math.Abs(f1), Math.Abs(f2));
			return d <= (l * eps);
		}
		#endregion // Floating Point Utils

		#region Powers
		/// <summary>
		/// Checks if the integer value is a power of two.
		/// </summary>
		/// <param name="l">The value to check.</param>
		/// <returns>If the value is a positive power of two.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPowerOfTwo(long l) => (l >= 0) && ((l & (l - 1)) == 0);

		/// <summary>
		/// Checks if the integer value is a power of two.
		/// </summary>
		/// <param name="l">The value to check.</param>
		/// <returns>If the value is a positive power of two.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPowerOfTwo(ulong l) => (l & (l - 1)) == 0;

		/// <summary>
		/// Checks if the value is exactly a power.
		/// </summary>
		/// <param name="l">The value to check.</param>
		/// <param name="power">The power to check for.</param>
		/// <returns>If the first value is a positive power of the second value.</returns>
		public static bool IsPowerOf(long l, ulong power)
		{
			if (l < 0) return false;
			if (power == 0) return l == 0;
			if (power == 1) return l == 1;
			if (power == 2) return IsPowerOfTwo((ulong)l);

			double log = Math.Log10((ulong)l) / Math.Log10(power);
			return NearlyEqual(log, Math.Floor(log)); // No fractional part for perfect powers with above log10 trick
		}

		/// <summary>
		/// Checks if the value is exactly a power.
		/// </summary>
		/// <param name="l">The value to check.</param>
		/// <param name="power">The power to check for.</param>
		/// <returns>If the first value is a positive power of the second value.</returns>
		public static bool IsPowerOf(ulong l, ulong power)
		{
			if (power == 0) return l == 0;
			if (power == 1) return l == 1;
			if (power == 2) return IsPowerOfTwo(l);

			double log = Math.Log10(l) / Math.Log10(power);
			return NearlyEqual(log, Math.Floor(log)); // No fractional part for perfect powers with above log10 trick
		}
		#endregion // Powers
	}
}
