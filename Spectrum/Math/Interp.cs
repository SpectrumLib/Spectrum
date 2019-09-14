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
	/// Contains functionality for interpolation between mathematical types.
	/// </summary>
	public static class Interp
	{
		#region Lerp
		/// <summary>
		/// Linear interpolation of scalar floats.
		/// </summary>
		/// <param name="f1">The first value (amt == 0).</param>
		/// <param name="f2">The second value (amt == 1).</param>
		/// <param name="amt">The interpolation weight value.</param>
		/// <param name="val">The output interpolated value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Lerp(float f1, float f2, float amt, out float val) => val = f1 + ((f2 - f1) * amt);
		#endregion // Lerp

		#region LerpPrecise
		/// <summary>
		/// Precise linear interpolation of scalar floats. More expensive than standard Lerp, but can handle
		/// values of widely different scales more accurately.
		/// </summary>
		/// <param name="f1">The first value (amt == 0).</param>
		/// <param name="f2">The second value (amt == 1).</param>
		/// <param name="amt">The interpolation weight value.</param>
		/// <param name="val">The output interpolated value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LerpPrecise(float f1, float f2, float amt, out float val) => 
			val = ((1 - amt) * f1) + (f2 * amt);
		#endregion // LerpPrecise

		#region Barycentric
		/// <summary>
		/// Calculates the barycentric coordinate from the three axis values and two weights.
		/// </summary>
		/// <param name="f1">The first coordinate on the axis.</param>
		/// <param name="f2">The second coordinate on the axis.</param>
		/// <param name="f3">The third coordinate on the axis.</param>
		/// <param name="amt1">The normalized weight of the second coordiate.</param>
		/// <param name="amt2">The normalized weight of the third coordinate.</param>
		/// <param name="val">The output barycentric cooordinate.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Barycentric(float f1, float f2, float f3, float amt1, float amt2, out float val) =>
			val = f1 + ((f2 - f1) * amt1) + ((f3 - f1) * amt2);
		#endregion // Barycentric

		#region CatmullRom
		/// <summary>
		/// Calculates Catmull-Rom spline interpolation between f2 and f3 using f1 and f4 as control points.
		/// </summary>
		/// <param name="f1">The first control point.</param>
		/// <param name="f2">The second control point (amt == 0).</param>
		/// <param name="f3">The third control point (amt == 1).</param>
		/// <param name="f4">The fourth control point.</param>
		/// <param name="amt">The normalized spline weight.</param>
		/// <param name="val">The output spline value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static void CatmullRom(float f1, float f2, float f3, float f4, float amt, out float val)
		{
			// Using formula from http://www.mvps.org/directx/articles/catmull/, pointed to from MonoGame
			double a2 = amt * amt, a3 = a2 * amt;
			double i = (2 * f2) + ((f3 - f1) * amt) + (((2 * f1) - (5 * f2) + (4 * f3) - f4) * a2) +
				(((3 * f2) - f1 - (3 * f3) + f4) * a3);
			val = (float)(i * 0.5);
		}
		#endregion // CatmullRom

		#region Hermite
		/// <summary>
		/// Calculates a cubic Hermite spline interpolation using two control points, and their tangents.
		/// </summary>
		/// <param name="f1">The value of the first control point.</param>
		/// <param name="t1">The tangent of the first control point.</param>
		/// <param name="f2">The value of the second control point.</param>
		/// <param name="t2">The tangent of the second control point.</param>
		/// <param name="amt">The normalized spline weight.</param>
		/// <param name="val">The output spline value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static void Hermite(float f1, float t1, float f2, float t2, float amt, out float val)
		{
			double a2 = amt * amt, a3 = a2 * amt;
			double i = f1 + (t1 * amt) + (((3 * f2) - (3 * f1) - (2 * t1) - t2) * a2) +
				(((2 * f1) - (2 * f2) + t2 + t1) * a3);
			val = (float)i;
		}
		#endregion // Hermite

		#region SmoothLerp
		/// <summary>
		/// Performs a smooth (tangent == 0) cubic interpolation between two control points.
		/// </summary>
		/// <param name="f1">The first control point.</param>
		/// <param name="f2">The second control point.</param>
		/// <param name="amt">The interpolation weight.</param>
		/// <param name="val">The output value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static void SmoothLerp(float f1, float f2, float amt, out float val)
		{
			double a2 = amt * amt, a3 = a2 * amt;
			double i = f1 + (((3 * f2) - (3 * f1)) * a2) + (((2 * f1) - (2 * f2)) * a3);
			val = (float)i;
		}
		#endregion // SmoothLerp
	}
}
