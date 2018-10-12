using System;
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
		#endregion // Constants

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
		#endregion // Comparisons
	}
}
