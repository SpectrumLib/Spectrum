using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Spectrum
{
	/// <summary>
	/// Single-precision floating point math functions and constants, and other math utilities.
	/// </summary>
	public static class Mathf
	{
		#region Constants
		/// <summary>
		/// The value of pi devided by four, represents 1/8 of a circle in radians.
		/// </summary>
		public const float PI_4 = (float)(Math.PI / 4.0);
		/// <summary>
		/// The value of pi devided by two, represents 1/4 of a circle in radians.
		/// </summary>
		public const float PI_2 = (float)(Math.PI / 2.0);
		/// <summary>
		/// The mathematical constant pi, the ratio of a circle's circumference to its diameter.
		/// </summary>
		public const float PI = (float)Math.PI;
		/// <summary>
		/// The mathematical constant tau, the ratio of a circle's circumference to its radius, equal to 2*PI.
		/// </summary>
		public const float TAU = (float)(Math.PI * 2.0);
		/// <summary>
		/// The mathematical constant representing the base of the natural logarithm.
		/// </summary>
		public const float E = (float)Math.E;
		/// <summary>
		/// The mathematical constant representing the golden ratio.
		/// </summary>
		public const float PHI = 1.6180339887498948482f;
		/// <summary>
		/// Multiplicitive constant for converting degrees to radians.
		/// </summary>
		public const float Deg2Rad = (float)(Math.PI / 180);
		/// <summary>
		/// Multiplicitive constant for converting radians to degrees.
		/// </summary>
		public const float Rad2Deg = (float)(180 / Math.PI);
		#endregion // Constants

		#region Single Precision Math
		/// <summary>
		/// Gives the angle whose cosine is the specified number.
		/// </summary>
		/// <param name="f">The cosine of the angle to calculate, must be between -1 and 1.</param>
		/// <returns>An angle in radians between 0 and pi.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Acos(float f) => (float)Math.Acos(f);
		/// <summary>
		/// Gives the angle whose sine is the specified number.
		/// </summary>
		/// <param name="f">The sine of the angle to calculate, must be between -1 and 1.</param>
		/// <returns>An angle in radians between -pi/2 and pi/2.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Asin(float f) => (float)Math.Asin(f);
		/// <summary>
		/// Gives the angle whose tangent is the specified number.
		/// </summary>
		/// <param name="f">The tangent of the angle to calculate.</param>
		/// <returns>An angle in radians between -pi/2 and pi/2.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Atan(float f) => (float)Math.Atan(f);
		/// <summary>
		/// Gives the angle whose tangent is the quotient of the specified numbers. This version of the Atan function
		/// is aware of the quadrants, and will return an angle in the correct quadrant of the 2D cartesian plane.
		/// </summary>
		/// <param name="x">The x-coordinate of the angle point.</param>
		/// <param name="y">The y-coordinate of the angle point.</param>
		/// <returns>An angle in the quadrant in which (x, y) exists, always between -pi and pi.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Atan2(float x, float y) => (float)Math.Atan2(x, y);
		/// <summary>
		/// Calculates the cube root of the passed value. Note that this is implemented using the power function, as
		/// .NET Standard 2.0 does not specify the Math.Cbrt function found in many implementations.
		/// </summary>
		/// <param name="f">The value to calculate the cube root of.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Cbrt(float f) => (float)Math.Pow(f, 1/3f);
		/// <summary>
		/// Rounds the argument towards the smallest integral value greater than or equal to it.
		/// </summary>
		/// <param name="f">The value to round towards positive infinity.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Ceiling(float f) => (float)Math.Ceiling((double)f);
		/// <summary>
		/// Returns the value clamped into the specified range.
		/// </summary>
		/// <param name="f">The value to clamp.</param>
		/// <param name="lo">The low end of the clamp range, must be less than hi.</param>
		/// <param name="hi">The high end of the clamp range, must be greater than lo.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Clamp(float f, float lo, float hi) => f < lo ? lo : f > hi ? hi : f;
		/// <summary>
		/// Calculates the cosine of the passed value.
		/// </summary>
		/// <param name="f">The angle, in radians.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Cos(float f) => (float)Math.Cos(f);
		/// <summary>
		/// Calculates the hyperbolic cosine of the passed value.
		/// </summary>
		/// <param name="f">The angle, in radians.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Cosh(float f) => (float)Math.Cosh(f);
		/// <summary>
		/// Raises <see cref="Mathf.E"/> to the value of the argument.
		/// </summary>
		/// <param name="f">The power of the exponent to calculate.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Exp(float f) => (float)Math.Pow(E, f);
		/// <summary>
		/// Rounds the argument towards the smallest integral value less than or equal to it.
		/// </summary>
		/// <param name="f">The value to round towards negative infinity.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Floor(float f) => (float)Math.Floor((double)f);
		/// <summary>
		/// Calculates the IEEE remainder of the division of x by y. Note that this is not the same function as the
		/// modulus operator on the values. See <see cref="Math.IEEERemainder"/> for a description of the differences.
		/// </summary>
		/// <param name="x">The numerator of the quotient.</param>
		/// <param name="y">The denominator of the quotient.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float IEEERemainder(float x, float y) => (float)Math.IEEERemainder(x, y);
		/// <summary>
		/// Calculate the natural logarithm (base <see cref="E"/>) of the value.
		/// </summary>
		/// <param name="f">The value to caluclate the logarithm of.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Log(float f) => (float)Math.Log(f);
		/// <summary>
		/// Calculate the logarithm of the value in the passed base.
		/// </summary>
		/// <param name="f">The value to caluclate the logarithm of.</param>
		/// <param name="b">The base of the logarithm to calculate.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Log(float f, float b) => (float)Math.Log(f, b);
		/// <summary>
		/// Calculate the decimal logarithm (base 10) of the value.
		/// </summary>
		/// <param name="f">The value to caluclate the logarithm of.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Log10(float f) => (float)Math.Log10(f);
		/// <summary>
		/// Calculate the maximum (closer to positive infinity) of the two values.
		/// </summary>
		/// <param name="x">The first value to compare.</param>
		/// <param name="y">The second value to compare.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Max(float x, float y) => x > y ? x : y;
		/// <summary>
		/// Calculate the maximum (closer to positive infinity) of the three values.
		/// </summary>
		/// <param name="x">The first value to compare.</param>
		/// <param name="y">The second value to compare.</param>
		/// <param name="z">The third value to compare.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Max(float x, float y, float z) => x > y ? (x > z ? x : z) : (z > y ? z : y);
		/// <summary>
		/// Calculate the maximum (closer to positive infinity) of the values.
		/// </summary>
		/// <param name="vals">The values to calculate the maximum of.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Max(params float[] vals) => vals.Max();
		/// <summary>
		/// Calculate the minimum (closer to negative infinity) of the two values.
		/// </summary>
		/// <param name="x">The first value to compare.</param>
		/// <param name="y">The second value to compare.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Min(float x, float y) => x < y ? x : y;
		/// <summary>
		/// Calculate the minimum (closer to negative infinity) of the three values.
		/// </summary>
		/// <param name="x">The first value to compare.</param>
		/// <param name="y">The second value to compare.</param>
		/// <param name="z">The third value to compare.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Min(float x, float y, float z) => x < y ? (x < z ? x : z) : (z < y ? z : y);
		/// <summary>
		/// Calculate the minimum (closer to negative infinity) of the values.
		/// </summary>
		/// <param name="vals">The values to calculate the minimum of.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Min(params float[] vals) => vals.Min();
		/// <summary>
		/// Raises the first argument to the power of the second argument.
		/// </summary>
		/// <param name="b">The base value.</param>
		/// <param name="e">The exponent value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Pow(float b, float e) => (float)Math.Pow(b, e);
		/// <summary>
		/// Rounds the value to the nearest integral value.
		/// </summary>
		/// <param name="f">The value to round.</param>
		/// <param name="mode">The optional midpoint rounding mode, defaults to away from zero.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Round(float f, MidpointRounding mode = MidpointRounding.AwayFromZero) => (float)Math.Round((double)f, mode);
		/// <summary>
		/// Rounds the value to the specified number of decimal places.
		/// </summary>
		/// <param name="f">The value to round.</param>
		/// <param name="d">The number of decimal places to round to.</param>
		/// <param name="mode">The optional midpoint rounding mode, defaults to away from zero.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Round(float f, byte d, MidpointRounding mode = MidpointRounding.AwayFromZero) => (float)Math.Round((double)f, d, mode);
		/// <summary>
		/// Calculates the sine of the passed value.
		/// </summary>
		/// <param name="f">The angle, in radians.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Sin(float f) => (float)Math.Sin(f);
		/// <summary>
		/// Calculates the hyperbolic sine of the passed value.
		/// </summary>
		/// <param name="f">The angle, in radians.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Sinh(float f) => (float)Math.Sinh(f);
		/// <summary>
		/// Calculates the square root of the value.
		/// </summary>
		/// <param name="f">The value to calculate the square root of.</param>
		public static float Sqrt(float f) => (float)Math.Sqrt(f);
		/// <summary>
		/// Calculates the tangent of the passed value.
		/// </summary>
		/// <param name="f">The angle, in radians.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Tan(float f) => (float)Math.Tan(f);
		/// <summary>
		/// Calculates the hyperbolic tangent of the passed value.
		/// </summary>
		/// <param name="f">The angle, in radians.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Tanh(float f) => (float)Math.Tanh(f);
		/// <summary>
		/// Calculates the integral part of the passed value.
		/// </summary>
		/// <param name="f">The value to truncate.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Truncate(float f) => (float)Math.Truncate(f);
		/// <summary>
		/// Clamps the value into the range [0, 1].
		/// </summary>
		/// <param name="f">The value to clamp.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float UnitClamp(float f) => f < 0 ? 0 : f > 1 ? 1 : f;
		#endregion // Single Precision Math
	}
}
