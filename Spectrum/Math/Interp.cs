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
		/// <summary>
		/// Linear interpolation of 2-component vectors.
		/// </summary>
		/// <param name="f1">The first value (amt == 0).</param>
		/// <param name="f2">The second value (amt == 1).</param>
		/// <param name="amt">The interpolation weight value.</param>
		/// <param name="val">The output interpolated value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Lerp(in Vec2 f1, in Vec2 f2, float amt, out Vec2 val) => val = f1 + ((f2 - f1) * amt);
		/// <summary>
		/// Linear interpolation of 3-component vectors.
		/// </summary>
		/// <param name="f1">The first value (amt == 0).</param>
		/// <param name="f2">The second value (amt == 1).</param>
		/// <param name="amt">The interpolation weight value.</param>
		/// <param name="val">The output interpolated value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Lerp(in Vec3 f1, in Vec3 f2, float amt, out Vec3 val) => val = f1 + ((f2 - f1) * amt);
		/// <summary>
		/// Linear interpolation of 4-component vectors.
		/// </summary>
		/// <param name="f1">The first value (amt == 0).</param>
		/// <param name="f2">The second value (amt == 1).</param>
		/// <param name="amt">The interpolation weight value.</param>
		/// <param name="val">The output interpolated value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Lerp(in Vec4 f1, in Vec4 f2, float amt, out Vec4 val) => val = f1 + ((f2 - f1) * amt);
		/// <summary>
		/// Component-wise linear interpolation between the matrices.
		/// </summary>
		/// <param name="m1">The source matrix.</param>
		/// <param name="m2">The destination matrix.</param>
		/// <param name="amt">The interpolation weight.</param>
		/// <param name="o">The output matrix.</param>
		public static void Lerp(in Matrix m1, in Matrix m2, float amt, out Matrix o)
		{
			o.M00 = m1.M00 + ((m2.M00 - m1.M00) * amt); o.M01 = m1.M01 + ((m2.M01 - m1.M01) * amt);
			o.M02 = m1.M02 + ((m2.M02 - m1.M02) * amt); o.M03 = m1.M03 + ((m2.M03 - m1.M03) * amt);
			o.M10 = m1.M10 + ((m2.M10 - m1.M10) * amt); o.M11 = m1.M11 + ((m2.M11 - m1.M11) * amt);
			o.M12 = m1.M12 + ((m2.M12 - m1.M12) * amt); o.M13 = m1.M13 + ((m2.M13 - m1.M13) * amt);
			o.M20 = m1.M20 + ((m2.M20 - m1.M20) * amt); o.M21 = m1.M21 + ((m2.M21 - m1.M21) * amt);
			o.M22 = m1.M22 + ((m2.M22 - m1.M22) * amt); o.M23 = m1.M23 + ((m2.M23 - m1.M23) * amt);
			o.M30 = m1.M30 + ((m2.M30 - m1.M30) * amt); o.M31 = m1.M31 + ((m2.M31 - m1.M31) * amt);
			o.M32 = m1.M32 + ((m2.M32 - m1.M32) * amt); o.M33 = m1.M33 + ((m2.M33 - m1.M33) * amt);
		}

		/// <summary>
		/// Performs a linear interpolation between the rotations represented by the quaternions.
		/// </summary>
		/// <param name="q1">The source quaternion.</param>
		/// <param name="q2">The destination quaternion.</param>
		/// <param name="amt">The interpolation weight.</param>
		/// <param name="o">The output quaternion.</param>
		public static void Lerp(in Quaternion q1, in Quaternion q2, float amt, out Quaternion o)
		{
			float a2 = 1 - amt;
			float dot = (q1.X * q2.X) + (q1.Y * q2.Y) + (q1.Z * q2.Z) + (q1.W * q2.W);

			if (dot >= 0)
			{
				o.X = (a2 * q1.X) + (amt * q2.X);
				o.Y = (a2 * q1.Y) + (amt * q2.Y);
				o.Z = (a2 * q1.Z) + (amt * q2.Z);
				o.W = (a2 * q1.W) + (amt * q2.W);
			}
			else
			{
				o.X = (a2 * q1.X) - (amt * q2.X);
				o.Y = (a2 * q1.Y) - (amt * q2.Y);
				o.Z = (a2 * q1.Z) - (amt * q2.Z);
				o.W = (a2 * q1.W) - (amt * q2.W);
			}

			dot = MathF.Sqrt((o.X * o.X) + (o.Y * o.Y) + (o.Z * o.Z) + (o.W * o.W));
			o.X /= dot;
			o.Y /= dot;
			o.Z /= dot;
			o.W /= dot;
		}
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
		public static void LerpPrecise(float f1, float f2, float amt, out float val) => val = 
			((1 - amt) * f1) + (f2 * amt);
		/// <summary>
		/// Precise linear interpolation of 2-component vectors. More expensive than standard Lerp, but can handle
		/// values of widely different scales more accurately.
		/// </summary>
		/// <param name="f1">The first value (amt == 0).</param>
		/// <param name="f2">The second value (amt == 1).</param>
		/// <param name="amt">The interpolation weight value.</param>
		/// <param name="val">The output interpolated value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LerpPrecise(in Vec2 f1, in Vec2 f2, float amt, out Vec2 val) => val =
			((1 - amt) * f1) + (f2 * amt);
		/// <summary>
		/// Precise linear interpolation of 3-component vectors. More expensive than standard Lerp, but can handle
		/// values of widely different scales more accurately.
		/// </summary>
		/// <param name="f1">The first value (amt == 0).</param>
		/// <param name="f2">The second value (amt == 1).</param>
		/// <param name="amt">The interpolation weight value.</param>
		/// <param name="val">The output interpolated value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LerpPrecise(in Vec3 f1, in Vec3 f2, float amt, out Vec3 val) => val =
			((1 - amt) * f1) + (f2 * amt);
		/// <summary>
		/// Precise linear interpolation of 4-component vectors. More expensive than standard Lerp, but can handle
		/// values of widely different scales more accurately.
		/// </summary>
		/// <param name="f1">The first value (amt == 0).</param>
		/// <param name="f2">The second value (amt == 1).</param>
		/// <param name="amt">The interpolation weight value.</param>
		/// <param name="val">The output interpolated value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LerpPrecise(in Vec4 f1, in Vec4 f2, float amt, out Vec4 val) => val =
			((1 - amt) * f1) + (f2 * amt);

		/// <summary>
		/// Component-wise linear interpolation between the matrices, with better edge case handling for values that are
		/// greatly mismatched in magnitude.
		/// </summary>
		/// <param name="m1">The source matrix.</param>
		/// <param name="m2">The destination matrix.</param>
		/// <param name="amt">The interpolation weight.</param>
		/// <param name="o">The output matrix.</param>
		public static void LerpPrecise(in Matrix m1, in Matrix m2, float amt, out Matrix o)
		{
			float amt2 = 1 - amt;
			o.M00 = (amt2 * m1.M00) + (m2.M00 * amt); o.M01 = (amt2 * m1.M01) + (m2.M01 * amt);
			o.M02 = (amt2 * m1.M02) + (m2.M02 * amt); o.M03 = (amt2 * m1.M03) + (m2.M03 * amt);
			o.M10 = (amt2 * m1.M10) + (m2.M10 * amt); o.M11 = (amt2 * m1.M11) + (m2.M11 * amt);
			o.M12 = (amt2 * m1.M12) + (m2.M12 * amt); o.M13 = (amt2 * m1.M13) + (m2.M13 * amt);
			o.M20 = (amt2 * m1.M20) + (m2.M20 * amt); o.M21 = (amt2 * m1.M21) + (m2.M21 * amt);
			o.M22 = (amt2 * m1.M22) + (m2.M22 * amt); o.M23 = (amt2 * m1.M23) + (m2.M23 * amt);
			o.M30 = (amt2 * m1.M30) + (m2.M30 * amt); o.M31 = (amt2 * m1.M31) + (m2.M31 * amt);
			o.M32 = (amt2 * m1.M32) + (m2.M32 * amt); o.M33 = (amt2 * m1.M33) + (m2.M33 * amt);
		}
		#endregion // LerpPrecise

		#region Barycentric
		/// <summary>
		/// Calculates the barycentric coordinate from the three axis values and two weights.
		/// </summary>
		/// <param name="f1">The first coordinate on the axis.</param>
		/// <param name="f2">The second coordinate on the axis.</param>
		/// <param name="f3">The third coordinate on the axis.</param>
		/// <param name="amt1">The normalized weight of the second coordinate.</param>
		/// <param name="amt2">The normalized weight of the third coordinate.</param>
		/// <param name="val">The output barycentric cooordinate.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Barycentric(float f1, float f2, float f3, float amt1, float amt2, out float val) =>
			val = f1 + ((f2 - f1) * amt1) + ((f3 - f1) * amt2);
		/// <summary>
		/// Calculates the component-wise barycentric coordinates for the vector.
		/// </summary>
		/// <param name="f1">The first vector of coordinates.</param>
		/// <param name="f2">The second vector of coordinates.</param>
		/// <param name="f3">The third vector of coordinates.</param>
		/// <param name="amt1">The normalized weight of the second coordinate.</param>
		/// <param name="amt2">The normalized weight of the third coordinate.</param>
		/// <param name="val">The output barycentric cooordinates.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Barycentric(Vec2 f1, Vec2 f2, Vec2 f3, float amt1, float amt2, out Vec2 val) =>
			val = f1 + ((f2 - f1) * amt1) + ((f3 - f1) * amt2);
		/// <summary>
		/// Calculates the component-wise barycentric coordinates for the vector.
		/// </summary>
		/// <param name="f1">The first vector of coordinates.</param>
		/// <param name="f2">The second vector of coordinates.</param>
		/// <param name="f3">The third vector of coordinates.</param>
		/// <param name="amt1">The normalized weight of the second coordinate.</param>
		/// <param name="amt2">The normalized weight of the third coordinate.</param>
		/// <param name="val">The output barycentric cooordinates.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Barycentric(Vec3 f1, Vec3 f2, Vec3 f3, float amt1, float amt2, out Vec3 val) =>
			val = f1 + ((f2 - f1) * amt1) + ((f3 - f1) * amt2);
		/// <summary>
		/// Calculates the component-wise barycentric coordinates for the vector.
		/// </summary>
		/// <param name="f1">The first vector of coordinates.</param>
		/// <param name="f2">The second vector of coordinates.</param>
		/// <param name="f3">The third vector of coordinates.</param>
		/// <param name="amt1">The normalized weight of the second coordinate.</param>
		/// <param name="amt2">The normalized weight of the third coordinate.</param>
		/// <param name="val">The output barycentric cooordinates.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Barycentric(Vec4 f1, Vec4 f2, Vec4 f3, float amt1, float amt2, out Vec4 val) =>
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
		public static void CatmullRom(in Vec2 f1, in Vec2 f2, in Vec2 f3, in Vec2 f4, float amt, out Vec2 val)
		{
			CatmullRom(f1.X, f2.X, f3.X, f4.X, amt, out val.X);
			CatmullRom(f1.Y, f2.Y, f3.Y, f4.Y, amt, out val.Y);
		}
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
		public static void CatmullRom(in Vec3 f1, in Vec3 f2, in Vec3 f3, in Vec3 f4, float amt, out Vec3 val)
		{
			CatmullRom(f1.X, f2.X, f3.X, f4.X, amt, out val.X);
			CatmullRom(f1.Y, f2.Y, f3.Y, f4.Y, amt, out val.Y);
			CatmullRom(f1.Z, f2.Z, f3.Z, f4.Z, amt, out val.Z);
		}
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
		public static void CatmullRom(in Vec4 f1, in Vec4 f2, in Vec4 f3, in Vec4 f4, float amt, out Vec4 val)
		{
			CatmullRom(f1.X, f2.X, f3.X, f4.X, amt, out val.X);
			CatmullRom(f1.Y, f2.Y, f3.Y, f4.Y, amt, out val.Y);
			CatmullRom(f1.Z, f2.Z, f3.Z, f4.Z, amt, out val.Z);
			CatmullRom(f1.W, f2.W, f3.W, f4.W, amt, out val.W);
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
		public static void Hermite(in Vec2 f1, in Vec2 t1, in Vec2 f2, in Vec2 t2, float amt, out Vec2 val)
		{
			Hermite(f1.X, t1.X, f2.X, t2.X, amt, out val.X);
			Hermite(f1.Y, t1.Y, f2.Y, t2.Y, amt, out val.Y);
		}
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
		public static void Hermite(in Vec3 f1, in Vec3 t1, in Vec3 f2, in Vec3 t2, float amt, out Vec3 val)
		{
			Hermite(f1.X, t1.X, f2.X, t2.X, amt, out val.X);
			Hermite(f1.Y, t1.Y, f2.Y, t2.Y, amt, out val.Y);
			Hermite(f1.Z, t1.Z, f2.Z, t2.Z, amt, out val.Z);
		}
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
		public static void Hermite(in Vec4 f1, in Vec4 t1, in Vec4 f2, in Vec4 t2, float amt, out Vec4 val)
		{
			Hermite(f1.X, t1.X, f2.X, t2.X, amt, out val.X);
			Hermite(f1.Y, t1.Y, f2.Y, t2.Y, amt, out val.Y);
			Hermite(f1.Z, t1.Z, f2.Z, t2.Z, amt, out val.Z);
			Hermite(f1.W, t1.W, f2.W, t2.W, amt, out val.W);
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
		/// <summary>
		/// Performs a smooth (tangent == 0) cubic interpolation between two control points.
		/// </summary>
		/// <param name="f1">The first control point.</param>
		/// <param name="f2">The second control point.</param>
		/// <param name="amt">The interpolation weight.</param>
		/// <param name="val">The output value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static void SmoothLerp(in Vec2 f1, in Vec2 f2, float amt, out Vec2 val)
		{
			SmoothLerp(f1.X, f2.X, amt, out val.X);
			SmoothLerp(f1.Y, f2.Y, amt, out val.Y);
		}
		/// <summary>
		/// Performs a smooth (tangent == 0) cubic interpolation between two control points.
		/// </summary>
		/// <param name="f1">The first control point.</param>
		/// <param name="f2">The second control point.</param>
		/// <param name="amt">The interpolation weight.</param>
		/// <param name="val">The output value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static void SmoothLerp(in Vec3 f1, in Vec3 f2, float amt, out Vec3 val)
		{
			SmoothLerp(f1.X, f2.X, amt, out val.X);
			SmoothLerp(f1.Y, f2.Y, amt, out val.Y);
			SmoothLerp(f1.Z, f2.Z, amt, out val.Z);
		}
		/// <summary>
		/// Performs a smooth (tangent == 0) cubic interpolation between two control points.
		/// </summary>
		/// <param name="f1">The first control point.</param>
		/// <param name="f2">The second control point.</param>
		/// <param name="amt">The interpolation weight.</param>
		/// <param name="val">The output value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static void SmoothLerp(in Vec4 f1, in Vec4 f2, float amt, out Vec4 val)
		{
			SmoothLerp(f1.X, f2.X, amt, out val.X);
			SmoothLerp(f1.Y, f2.Y, amt, out val.Y);
			SmoothLerp(f1.Z, f2.Z, amt, out val.Z);
			SmoothLerp(f1.W, f2.W, amt, out val.W);
		}

		/// <summary>
		/// Performs a smooth cubic-interpolation between the matrices.
		/// </summary>
		/// <param name="m1">The source matrix.</param>
		/// <param name="m2">The destination matrix.</param>
		/// <param name="amt">The interpolation weight.</param>
		/// <param name="o">The output matrix.</param>
		public static void SmoothLerp(in Matrix m1, in Matrix m2, float amt, out Matrix o)
		{
			SmoothLerp(m1.M00, m2.M00, amt, out o.M00);
			SmoothLerp(m1.M01, m2.M01, amt, out o.M01);
			SmoothLerp(m1.M02, m2.M02, amt, out o.M02);
			SmoothLerp(m1.M03, m2.M03, amt, out o.M03);
			SmoothLerp(m1.M10, m2.M10, amt, out o.M10);
			SmoothLerp(m1.M11, m2.M11, amt, out o.M11);
			SmoothLerp(m1.M12, m2.M12, amt, out o.M12);
			SmoothLerp(m1.M13, m2.M13, amt, out o.M13);
			SmoothLerp(m1.M20, m2.M20, amt, out o.M20);
			SmoothLerp(m1.M21, m2.M21, amt, out o.M21);
			SmoothLerp(m1.M22, m2.M22, amt, out o.M22);
			SmoothLerp(m1.M23, m2.M23, amt, out o.M23);
			SmoothLerp(m1.M30, m2.M30, amt, out o.M30);
			SmoothLerp(m1.M31, m2.M31, amt, out o.M31);
			SmoothLerp(m1.M32, m2.M32, amt, out o.M32);
			SmoothLerp(m1.M33, m2.M33, amt, out o.M33);
		}
		#endregion // SmoothLerp

		#region Slerp
		/// <summary>
		/// Performs a spherical linear interpolation between the rotations represented by the quaternions.
		/// </summary>
		/// <param name="q1">The source quaternion.</param>
		/// <param name="q2">The destination quaternion.</param>
		/// <param name="amt">The interpolation weight.</param>
		/// <param name="o">The output quaternion.</param>
		public static void Slerp(in Quaternion q1, in Quaternion q2, float amt, out Quaternion o)
		{
			float dot = (q1.X * q2.X) + (q1.Y * q2.Y) + (q1.Z * q2.Z) + (q1.W * q2.W);
			bool ltz = (dot < 0);
			if (ltz) dot = -dot;
			float a1, a2;

			if (dot >= 1)
			{
				a2 = 1 - amt;
				a1 = ltz ? -amt : amt;
			}
			else
			{
				float acos = MathF.Acos(dot);
				float isin = 1 / MathF.Sin(acos);
				a2 = MathF.Sin((1 - amt) * acos) * isin;
				a1 = ltz ? (-MathF.Sin(amt * acos) * isin) : (MathF.Sin(amt * acos) * isin);
			}

			o.X = (a2 * q1.X) + (a1 * q2.X);
			o.Y = (a2 * q1.Y) + (a1 * q2.Y);
			o.Z = (a2 * q1.Z) + (a1 * q2.Z);
			o.W = (a2 * q1.W) + (a1 * q2.W);
		}
		#endregion // Slerp
	}
}
