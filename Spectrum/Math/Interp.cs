using System;
using System.Runtime.CompilerServices;

namespace Spectrum
{
	/// <summary>
	/// Contains functionality for multiple interpolation methods for mathematical types.
	/// </summary>
	public static class Interp
	{
		#region Single-Precision
		/// <summary>
		/// Performs a linear-interpolation between two values. See <see cref="LerpPrecise"/> for a slightly more 
		/// expensive version that handles edge cases.
		/// </summary>
		/// <param name="f1">Source value.</param>
		/// <param name="f2">Destination value.</param>
		/// <param name="amt">Normalized weight towards the destination value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Lerp(float f1, float f2, float amt) => f1 + ((f2 - f1) * amt);

		/// <summary>
		/// Performs a linear-interpolation between two values, with better edge case handling for values that are
		/// greatly mismatched in magnitude. See <see cref="Lerp"/> for a slightly cheaper version that does not
		/// handle edge cases.
		/// </summary>
		/// <param name="f1">Source value.</param>
		/// <param name="f2">Destination value.</param>
		/// <param name="amt">Normalized weight towards the destination value.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float LerpPrecise(float f1, float f2, float amt) => ((1 - amt) * f1) + (f2 * amt);

		/// <summary>
		/// Calculates the barycentric coordinate from the defining axis values and normalized weights.
		/// </summary>
		/// <param name="f1">Coordinate 1 on one axis of the defining triangle.</param>
		/// <param name="f2">Coordinate 2 on the same axis of the defining triangle.</param>
		/// <param name="f3">Coordinate 2 on the same axis of the defining triangle.</param>
		/// <param name="amt1">The first normalized barycentric coordinate, the weighting factor for coordinate 2.</param>
		/// <param name="amt2">The second normalized barycentric coordinate, the weighting factor for coordinate 3.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Barycentric(float f1, float f2, float f3, float amt1, float amt2) =>
			f1 + ((f2 - f1) * amt1) + ((f3 - f1) * amt2);

		/// <summary>
		/// Performs a Catmull-Rom spline interpolation between f2 and f3 using f1 and f4 as control points to define
		/// the shape of the spline.
		/// </summary>
		/// <param name="f1">The first control point.</param>
		/// <param name="f2">The second control point.</param>
		/// <param name="f3">The third control point.</param>
		/// <param name="f4">The final control point.</param>
		/// <param name="amt">The normalized interpolation amount between the second and third points.</param>
		public static float CatmullRom(float f1, float f2, float f3, float f4, float amt)
		{
			// Using formula from http://www.mvps.org/directx/articles/catmull/, pointed to from MonoGame
			// Use doubles to not lose precision in the multitude of coming floating point calculations
			double a2 = amt * amt, a3 = a2 * amt;
			double i = (2 * f2) + ((f3 - f1) * amt) + ((2 * f1 - 5 * f2 + 4 * f3 - f4) * a2) +
				((3 * f2 - f1 - 3 * f3 + f4) * a3);
			return (float)(i * 0.5);
		}

		/// <summary>
		/// Performs a cubic Hermite spline between the two control points using their tangents.
		/// </summary>
		/// <param name="f1">The first control point.</param>
		/// <param name="t1">The tangent of the first control point.</param>
		/// <param name="f2">The second control point.</param>
		/// <param name="t2">The tangent of the second control point.</param>
		/// <param name="amt">The normalized interpolation weight.</param>
		public static float Hermite(float f1, float t1, float f2, float t2, float amt)
		{
			// Use doubles to not lose precision in the multitude of coming floating point calculations
			if (amt == 0) return f1;
			if (amt == 1) return f2;

			double a2 = amt * amt, a3 = a2 * amt;
			double i = f1 + (t1 * amt) + ((3 * f2 - 3 * f1 - 2 * t1 - t2) * a2) +
				((2 * f1 - 2 * f2 + t2 + t1) * a3);
			return (float)i;
		}

		/// <summary>
		/// Performs a smooth (tangent = 0) cubic-interpolation between the two values.
		/// </summary>
		/// <param name="f1">The source value.</param>
		/// <param name="f2">The destination value.</param>
		/// <param name="amt">The normalized weight, values outside [0, 1] are clamped.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float SmoothLerp(float f1, float f2, float amt) => Hermite(f1, 0, f2, 0, Mathf.UnitClamp(amt));
		#endregion // Single-Precision

		#region Vec2
		/// <summary>
		/// Performs a linear-interpolation between two vectors. See <see cref="LerpPrecise"/> for a slightly more 
		/// expensive version that handles edge cases.
		/// </summary>
		/// <param name="f1">Source vector.</param>
		/// <param name="f2">Destination vector.</param>
		/// <param name="amt">Normalized weight towards the destination vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 Lerp(in Vec2 f1, in Vec2 f2, float amt) => new Vec2(
			f1.X + ((f2.X - f1.X) * amt),
			f1.Y + ((f2.Y - f1.Y) * amt)
		);

		/// <summary>
		/// Performs a linear-interpolation between two vectors. See <see cref="LerpPrecise"/> for a slightly more 
		/// expensive version that handles edge cases.
		/// </summary>
		/// <param name="f1">Source vector.</param>
		/// <param name="f2">Destination vector.</param>
		/// <param name="amt">Normalized weight towards the destination vector.</param>
		/// <param name="o">The output vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Lerp(in Vec2 f1, in Vec2 f2, float amt, out Vec2 o)
		{
			o.X = f1.X + ((f2.X - f1.X) * amt);
			o.Y = f1.Y + ((f2.Y - f1.Y) * amt);
		}

		/// <summary>
		/// Performs a linear-interpolation between two vectors, with better edge case handling for values that are
		/// greatly mismatched in magnitude. See <see cref="Lerp"/> for a slightly cheaper version that does not
		/// handle edge cases.
		/// </summary>
		/// <param name="f1">Source vector.</param>
		/// <param name="f2">Destination vector.</param>
		/// <param name="amt">Normalized weight towards the destination vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 LerpPrecise(in Vec2 f1, in Vec2 f2, float amt) => new Vec2(
			((1 - amt) * f1.X) + (f2.X * amt),
			((1 - amt) * f1.Y) + (f2.Y * amt)
		);

		/// <summary>
		/// Performs a linear-interpolation between two vectors, with better edge case handling for values that are
		/// greatly mismatched in magnitude. See <see cref="Lerp"/> for a slightly cheaper version that does not
		/// handle edge cases.
		/// </summary>
		/// <param name="f1">Source vector.</param>
		/// <param name="f2">Destination vector.</param>
		/// <param name="amt">Normalized weight towards the destination vector.</param>
		/// <param name="o">The output vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LerpPrecise(in Vec2 f1, in Vec2 f2, float amt, out Vec2 o)
		{
			o.X = ((1 - amt) * f1.X) + (f2.X * amt);
			o.Y = ((1 - amt) * f1.Y) + (f2.Y * amt);
		}

		/// <summary>
		/// Calculates the barycentric coordinate from the defining triangle vertices and normalized weights.
		/// </summary>
		/// <param name="f1">Vertex 1 of the defining triangle.</param>
		/// <param name="f2">Vertex 2 of the defining triangle.</param>
		/// <param name="f3">Vertex 3 of the defining triangle.</param>
		/// <param name="amt1">The first normalized barycentric coordinate, the weighting factor for vertex 2.</param>
		/// <param name="amt2">The second normalized barycentric coordinate, the weighting factor for vertex 3.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 Barycentric(in Vec2 f1, in Vec2 f2, in Vec2 f3, float amt1, float amt2) => new Vec2(
			f1.X + ((f2.X - f1.X) * amt1) + ((f3.X - f1.X) * amt2),
			f1.Y + ((f2.Y - f1.Y) * amt1) + ((f3.Y - f1.Y) * amt2)
		);

		/// <summary>
		/// Calculates the barycentric coordinate from the defining triangle vertices and normalized weights.
		/// </summary>
		/// <param name="f1">Vertex 1 of the defining triangle.</param>
		/// <param name="f2">Vertex 2 of the defining triangle.</param>
		/// <param name="f3">Vertex 3 of the defining triangle.</param>
		/// <param name="amt1">The first normalized barycentric coordinate, the weighting factor for vertex 2.</param>
		/// <param name="amt2">The second normalized barycentric coordinate, the weighting factor for vertex 3.</param>
		/// <param name="o">The output vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Barycentric(in Vec2 f1, in Vec2 f2, in Vec2 f3, float amt1, float amt2, out Vec2 o)
		{
			o.X = f1.X + ((f2.X - f1.X) * amt1) + ((f3.X - f1.X) * amt2);
			o.Y = f1.Y + ((f2.Y - f1.Y) * amt1) + ((f3.Y - f1.Y) * amt2);
		}

		/// <summary>
		/// Performs a Catmull-Rom spline interpolation between f2 and f3 using f1 and f4 as control points to define
		/// the shape of the spline.
		/// </summary>
		/// <param name="f1">The first control point.</param>
		/// <param name="f2">The second control point.</param>
		/// <param name="f3">The third control point.</param>
		/// <param name="f4">The final control point.</param>
		/// <param name="amt">The normalized interpolation amount between the second and third points.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 CatmullRom(in Vec2 f1, in Vec2 f2, in Vec2 f3, in Vec2 f4, float amt) => new Vec2(
			CatmullRom(f1.X, f2.X, f3.X, f4.X, amt),
			CatmullRom(f1.Y, f2.Y, f3.Y, f4.Y, amt)
		);

		/// <summary>
		/// Performs a Catmull-Rom spline interpolation between f2 and f3 using f1 and f4 as control points to define
		/// the shape of the spline.
		/// </summary>
		/// <param name="f1">The first control point.</param>
		/// <param name="f2">The second control point.</param>
		/// <param name="f3">The third control point.</param>
		/// <param name="f4">The final control point.</param>
		/// <param name="amt">The normalized interpolation amount between the second and third points.</param>
		/// <param name="o">The output vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void CatmullRom(in Vec2 f1, in Vec2 f2, in Vec2 f3, in Vec2 f4, float amt, out Vec2 o)
		{
			o.X = CatmullRom(f1.X, f2.X, f3.X, f4.X, amt);
			o.Y = CatmullRom(f1.Y, f2.Y, f3.Y, f4.Y, amt);
		}

		/// <summary>
		/// Performs a cubic Hermite spline between the two control points using their tangents.
		/// </summary>
		/// <param name="f1">The first control point.</param>
		/// <param name="t1">The tangent of the first control point.</param>
		/// <param name="f2">The second control point.</param>
		/// <param name="t2">The tangent of the second control point.</param>
		/// <param name="amt">The normalized interpolation weight.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 Hermite(in Vec2 f1, in Vec2 t1, in Vec2 f2, in Vec2 t2, float amt) => new Vec2(
			Hermite(f1.X, t1.X, f2.X, t2.X, amt),
			Hermite(f1.Y, t1.Y, f2.Y, t2.Y, amt)
		);

		/// <summary>
		/// Performs a cubic Hermite spline between the two control points using their tangents.
		/// </summary>
		/// <param name="f1">The first control point.</param>
		/// <param name="t1">The tangent of the first control point.</param>
		/// <param name="f2">The second control point.</param>
		/// <param name="t2">The tangent of the second control point.</param>
		/// <param name="amt">The normalized interpolation weight.</param>
		/// <param name="o">The output vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Hermite(in Vec2 f1, in Vec2 t1, in Vec2 f2, in Vec2 t2, float amt, out Vec2 o)
		{
			o.X = Hermite(f1.X, t1.X, f2.X, t2.X, amt);
			o.Y = Hermite(f1.Y, t1.Y, f2.Y, t2.Y, amt);
		}

		/// <summary>
		/// Performs a smooth (tangent = 0) cubic-interpolation between the two vectors.
		/// </summary>
		/// <param name="f1">The source vector.</param>
		/// <param name="f2">The destination vector.</param>
		/// <param name="amt">The normalized weight, values outside [0, 1] are clamped.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 SmoothLerp(in Vec2 f1, in Vec2 f2, float amt) => new Vec2(
			Hermite(f1.X, 0, f2.X, 0, amt),
			Hermite(f1.Y, 0, f2.Y, 0, amt)
		);

		/// <summary>
		/// Performs a smooth (tangent = 0) cubic-interpolation between the two vectors.
		/// </summary>
		/// <param name="f1">The source vector.</param>
		/// <param name="f2">The destination vector.</param>
		/// <param name="amt">The normalized weight, values outside [0, 1] are clamped.</param>
		/// <param name="o">The output vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void SmoothLerp(in Vec2 f1, in Vec2 f2, float amt, out Vec2 o)
		{
			o.X = Hermite(f1.X, 0, f2.X, 0, amt);
			o.Y = Hermite(f1.Y, 0, f2.Y, 0, amt);
		}
		#endregion // Vec2

		#region Vec3
		/// <summary>
		/// Performs a linear-interpolation between two vectors. See <see cref="LerpPrecise"/> for a slightly more 
		/// expensive version that handles edge cases.
		/// </summary>
		/// <param name="f1">Source vector.</param>
		/// <param name="f2">Destination vector.</param>
		/// <param name="amt">Normalized weight towards the destination vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 Lerp(in Vec3 f1, in Vec3 f2, float amt) => new Vec3(
			f1.X + ((f2.X - f1.X) * amt),
			f1.Y + ((f2.Y - f1.Y) * amt),
			f1.Z + ((f2.Z - f1.Z) * amt)
		);

		/// <summary>
		/// Performs a linear-interpolation between two vectors. See <see cref="LerpPrecise"/> for a slightly more 
		/// expensive version that handles edge cases.
		/// </summary>
		/// <param name="f1">Source vector.</param>
		/// <param name="f2">Destination vector.</param>
		/// <param name="amt">Normalized weight towards the destination vector.</param>
		/// <param name="o">The output vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Lerp(in Vec3 f1, in Vec3 f2, float amt, out Vec3 o)
		{
			o.X = f1.X + ((f2.X - f1.X) * amt);
			o.Y = f1.Y + ((f2.Y - f1.Y) * amt);
			o.Z = f1.Z + ((f2.Z - f1.Z) * amt);
		}

		/// <summary>
		/// Performs a linear-interpolation between two vectors, with better edge case handling for values that are
		/// greatly mismatched in magnitude. See <see cref="Lerp"/> for a slightly cheaper version that does not
		/// handle edge cases.
		/// </summary>
		/// <param name="f1">Source vector.</param>
		/// <param name="f2">Destination vector.</param>
		/// <param name="amt">Normalized weight towards the destination vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 LerpPrecise(in Vec3 f1, in Vec3 f2, float amt) => new Vec3(
			((1 - amt) * f1.X) + (f2.X * amt),
			((1 - amt) * f1.Y) + (f2.Y * amt),
			((1 - amt) * f1.Z) + (f2.Z * amt)
		);

		/// <summary>
		/// Performs a linear-interpolation between two vectors, with better edge case handling for values that are
		/// greatly mismatched in magnitude. See <see cref="Lerp"/> for a slightly cheaper version that does not
		/// handle edge cases.
		/// </summary>
		/// <param name="f1">Source vector.</param>
		/// <param name="f2">Destination vector.</param>
		/// <param name="amt">Normalized weight towards the destination vector.</param>
		/// <param name="o">The output vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LerpPrecise(in Vec3 f1, in Vec3 f2, float amt, out Vec3 o)
		{
			o.X = ((1 - amt) * f1.X) + (f2.X * amt);
			o.Y = ((1 - amt) * f1.Y) + (f2.Y * amt);
			o.Z = ((1 - amt) * f1.Z) + (f2.Z * amt);
		}

		/// <summary>
		/// Calculates the barycentric coordinate from the defining triangle vertices and normalized weights.
		/// </summary>
		/// <param name="f1">Vertex 1 of the defining triangle.</param>
		/// <param name="f2">Vertex 2 of the defining triangle.</param>
		/// <param name="f3">Vertex 3 of the defining triangle.</param>
		/// <param name="amt1">The first normalized barycentric coordinate, the weighting factor for vertex 2.</param>
		/// <param name="amt2">The second normalized barycentric coordinate, the weighting factor for vertex 3.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 Barycentric(in Vec3 f1, in Vec3 f2, in Vec3 f3, float amt1, float amt2) => new Vec3(
			f1.X + ((f2.X - f1.X) * amt1) + ((f3.X - f1.X) * amt2),
			f1.Y + ((f2.Y - f1.Y) * amt1) + ((f3.Y - f1.Y) * amt2),
			f1.Z + ((f2.Z - f1.Z) * amt1) + ((f3.Z - f1.Z) * amt2)
		);

		/// <summary>
		/// Calculates the barycentric coordinate from the defining triangle vertices and normalized weights.
		/// </summary>
		/// <param name="f1">Vertex 1 of the defining triangle.</param>
		/// <param name="f2">Vertex 2 of the defining triangle.</param>
		/// <param name="f3">Vertex 3 of the defining triangle.</param>
		/// <param name="amt1">The first normalized barycentric coordinate, the weighting factor for vertex 2.</param>
		/// <param name="amt2">The second normalized barycentric coordinate, the weighting factor for vertex 3.</param>
		/// <param name="o">The output vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Barycentric(in Vec3 f1, in Vec3 f2, in Vec3 f3, float amt1, float amt2, out Vec3 o)
		{
			o.X = f1.X + ((f2.X - f1.X) * amt1) + ((f3.X - f1.X) * amt2);
			o.Y = f1.Y + ((f2.Y - f1.Y) * amt1) + ((f3.Y - f1.Y) * amt2);
			o.Z = f1.Z + ((f2.Z - f1.Z) * amt1) + ((f3.Z - f1.Z) * amt2);
		}

		/// <summary>
		/// Performs a Catmull-Rom spline interpolation between f2 and f3 using f1 and f4 as control points to define
		/// the shape of the spline.
		/// </summary>
		/// <param name="f1">The first control point.</param>
		/// <param name="f2">The second control point.</param>
		/// <param name="f3">The third control point.</param>
		/// <param name="f4">The final control point.</param>
		/// <param name="amt">The normalized interpolation amount between the second and third points.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 CatmullRom(in Vec3 f1, in Vec3 f2, in Vec3 f3, in Vec3 f4, float amt) => new Vec3(
			CatmullRom(f1.X, f2.X, f3.X, f4.X, amt),
			CatmullRom(f1.Y, f2.Y, f3.Y, f4.Y, amt),
			CatmullRom(f1.Z, f2.Z, f3.Z, f4.Z, amt)
		);

		/// <summary>
		/// Performs a Catmull-Rom spline interpolation between f2 and f3 using f1 and f4 as control points to define
		/// the shape of the spline.
		/// </summary>
		/// <param name="f1">The first control point.</param>
		/// <param name="f2">The second control point.</param>
		/// <param name="f3">The third control point.</param>
		/// <param name="f4">The final control point.</param>
		/// <param name="amt">The normalized interpolation amount between the second and third points.</param>
		/// <param name="o">The output vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void CatmullRom(in Vec3 f1, in Vec3 f2, in Vec3 f3, in Vec3 f4, float amt, out Vec3 o)
		{
			o.X = CatmullRom(f1.X, f2.X, f3.X, f4.X, amt);
			o.Y = CatmullRom(f1.Y, f2.Y, f3.Y, f4.Y, amt);
			o.Z = CatmullRom(f1.Z, f2.Z, f3.Z, f4.Z, amt);
		}

		/// <summary>
		/// Performs a cubic Hermite spline between the two control points using their tangents.
		/// </summary>
		/// <param name="f1">The first control point.</param>
		/// <param name="t1">The tangent of the first control point.</param>
		/// <param name="f2">The second control point.</param>
		/// <param name="t2">The tangent of the second control point.</param>
		/// <param name="amt">The normalized interpolation weight.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 Hermite(in Vec3 f1, in Vec3 t1, in Vec3 f2, in Vec3 t2, float amt) => new Vec3(
			Hermite(f1.X, t1.X, f2.X, t2.X, amt),
			Hermite(f1.Y, t1.Y, f2.Y, t2.Y, amt),
			Hermite(f1.Z, t1.Z, f2.Z, t2.Z, amt)
		);

		/// <summary>
		/// Performs a cubic Hermite spline between the two control points using their tangents.
		/// </summary>
		/// <param name="f1">The first control point.</param>
		/// <param name="t1">The tangent of the first control point.</param>
		/// <param name="f2">The second control point.</param>
		/// <param name="t2">The tangent of the second control point.</param>
		/// <param name="amt">The normalized interpolation weight.</param>
		/// <param name="o">The output vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Hermite(in Vec3 f1, in Vec3 t1, in Vec3 f2, in Vec3 t2, float amt, out Vec3 o)
		{
			o.X = Hermite(f1.X, t1.X, f2.X, t2.X, amt);
			o.Y = Hermite(f1.Y, t1.Y, f2.Y, t2.Y, amt);
			o.Z = Hermite(f1.Z, t1.Z, f2.Z, t2.Z, amt);
		}

		/// <summary>
		/// Performs a smooth (tangent = 0) cubic-interpolation between the two vectors.
		/// </summary>
		/// <param name="f1">The source vector.</param>
		/// <param name="f2">The destination vector.</param>
		/// <param name="amt">The normalized weight, values outside [0, 1] are clamped.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 SmoothLerp(in Vec3 f1, in Vec3 f2, float amt) => new Vec3(
			Hermite(f1.X, 0, f2.X, 0, amt),
			Hermite(f1.Y, 0, f2.Y, 0, amt),
			Hermite(f1.Z, 0, f2.Z, 0, amt)
		);

		/// <summary>
		/// Performs a smooth (tangent = 0) cubic-interpolation between the two vectors.
		/// </summary>
		/// <param name="f1">The source vector.</param>
		/// <param name="f2">The destination vector.</param>
		/// <param name="amt">The normalized weight, values outside [0, 1] are clamped.</param>
		/// <param name="o">The output vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void SmoothLerp(in Vec3 f1, in Vec3 f2, float amt, out Vec3 o)
		{
			o.X = Hermite(f1.X, 0, f2.X, 0, amt);
			o.Y = Hermite(f1.Y, 0, f2.Y, 0, amt);
			o.Z = Hermite(f1.Z, 0, f2.Z, 0, amt);
		}
		#endregion // Vec3

		#region Vec4
		/// <summary>
		/// Performs a linear-interpolation between two vectors. See <see cref="LerpPrecise"/> for a slightly more 
		/// expensive version that handles edge cases.
		/// </summary>
		/// <param name="f1">Source vector.</param>
		/// <param name="f2">Destination vector.</param>
		/// <param name="amt">Normalized weight towards the destination vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 Lerp(in Vec4 f1, in Vec4 f2, float amt) => new Vec4(
			f1.X + ((f2.X - f1.X) * amt),
			f1.Y + ((f2.Y - f1.Y) * amt),
			f1.Z + ((f2.Z - f1.Z) * amt),
			f1.W + ((f2.W - f1.W) * amt)
		);

		/// <summary>
		/// Performs a linear-interpolation between two vectors. See <see cref="LerpPrecise"/> for a slightly more 
		/// expensive version that handles edge cases.
		/// </summary>
		/// <param name="f1">Source vector.</param>
		/// <param name="f2">Destination vector.</param>
		/// <param name="amt">Normalized weight towards the destination vector.</param>
		/// <param name="o">The output vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Lerp(in Vec4 f1, in Vec4 f2, float amt, out Vec4 o)
		{
			o.X = f1.X + ((f2.X - f1.X) * amt);
			o.Y = f1.Y + ((f2.Y - f1.Y) * amt);
			o.Z = f1.Z + ((f2.Z - f1.Z) * amt);
			o.W = f1.W + ((f2.W - f1.W) * amt);
		}

		/// <summary>
		/// Performs a linear-interpolation between two vectors, with better edge case handling for values that are
		/// greatly mismatched in magnitude. See <see cref="Lerp"/> for a slightly cheaper version that does not
		/// handle edge cases.
		/// </summary>
		/// <param name="f1">Source vector.</param>
		/// <param name="f2">Destination vector.</param>
		/// <param name="amt">Normalized weight towards the destination vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 LerpPrecise(in Vec4 f1, in Vec4 f2, float amt) => new Vec4(
			((1 - amt) * f1.X) + (f2.X * amt),
			((1 - amt) * f1.Y) + (f2.Y * amt),
			((1 - amt) * f1.Z) + (f2.Z * amt),
			((1 - amt) * f1.W) + (f2.W * amt)
		);

		/// <summary>
		/// Performs a linear-interpolation between two vectors, with better edge case handling for values that are
		/// greatly mismatched in magnitude. See <see cref="Lerp"/> for a slightly cheaper version that does not
		/// handle edge cases.
		/// </summary>
		/// <param name="f1">Source vector.</param>
		/// <param name="f2">Destination vector.</param>
		/// <param name="amt">Normalized weight towards the destination vector.</param>
		/// <param name="o">The output vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LerpPrecise(in Vec4 f1, in Vec4 f2, float amt, out Vec4 o)
		{
			o.X = ((1 - amt) * f1.X) + (f2.X * amt);
			o.Y = ((1 - amt) * f1.Y) + (f2.Y * amt);
			o.Z = ((1 - amt) * f1.Z) + (f2.Z * amt);
			o.W = ((1 - amt) * f1.W) + (f2.W * amt);
		}

		/// <summary>
		/// Calculates the barycentric coordinate from the defining triangle vertices and normalized weights.
		/// </summary>
		/// <param name="f1">Vertex 1 of the defining triangle.</param>
		/// <param name="f2">Vertex 2 of the defining triangle.</param>
		/// <param name="f3">Vertex 3 of the defining triangle.</param>
		/// <param name="amt1">The first normalized barycentric coordinate, the weighting factor for vertex 2.</param>
		/// <param name="amt2">The second normalized barycentric coordinate, the weighting factor for vertex 3.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 Barycentric(in Vec4 f1, in Vec4 f2, in Vec4 f3, float amt1, float amt2) => new Vec4(
			f1.X + ((f2.X - f1.X) * amt1) + ((f3.X - f1.X) * amt2),
			f1.Y + ((f2.Y - f1.Y) * amt1) + ((f3.Y - f1.Y) * amt2),
			f1.Z + ((f2.Z - f1.Z) * amt1) + ((f3.Z - f1.Z) * amt2),
			f1.W + ((f2.W - f1.W) * amt1) + ((f3.W - f1.W) * amt2)
		);

		/// <summary>
		/// Calculates the barycentric coordinate from the defining triangle vertices and normalized weights.
		/// </summary>
		/// <param name="f1">Vertex 1 of the defining triangle.</param>
		/// <param name="f2">Vertex 2 of the defining triangle.</param>
		/// <param name="f3">Vertex 3 of the defining triangle.</param>
		/// <param name="amt1">The first normalized barycentric coordinate, the weighting factor for vertex 2.</param>
		/// <param name="amt2">The second normalized barycentric coordinate, the weighting factor for vertex 3.</param>
		/// <param name="o">The output vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Barycentric(in Vec4 f1, in Vec4 f2, in Vec4 f3, float amt1, float amt2, out Vec4 o)
		{
			o.X = f1.X + ((f2.X - f1.X) * amt1) + ((f3.X - f1.X) * amt2);
			o.Y = f1.Y + ((f2.Y - f1.Y) * amt1) + ((f3.Y - f1.Y) * amt2);
			o.Z = f1.Z + ((f2.Z - f1.Z) * amt1) + ((f3.Z - f1.Z) * amt2);
			o.W = f1.W + ((f2.W - f1.W) * amt1) + ((f3.W - f1.W) * amt2);
		}

		/// <summary>
		/// Performs a Catmull-Rom spline interpolation between f2 and f3 using f1 and f4 as control points to define
		/// the shape of the spline.
		/// </summary>
		/// <param name="f1">The first control point.</param>
		/// <param name="f2">The second control point.</param>
		/// <param name="f3">The third control point.</param>
		/// <param name="f4">The final control point.</param>
		/// <param name="amt">The normalized interpolation amount between the second and third points.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 CatmullRom(in Vec4 f1, in Vec4 f2, in Vec4 f3, in Vec4 f4, float amt) => new Vec4(
			CatmullRom(f1.X, f2.X, f3.X, f4.X, amt),
			CatmullRom(f1.Y, f2.Y, f3.Y, f4.Y, amt),
			CatmullRom(f1.Z, f2.Z, f3.Z, f4.Z, amt),
			CatmullRom(f1.W, f2.W, f3.W, f4.W, amt)
		);

		/// <summary>
		/// Performs a Catmull-Rom spline interpolation between f2 and f3 using f1 and f4 as control points to define
		/// the shape of the spline.
		/// </summary>
		/// <param name="f1">The first control point.</param>
		/// <param name="f2">The second control point.</param>
		/// <param name="f3">The third control point.</param>
		/// <param name="f4">The final control point.</param>
		/// <param name="amt">The normalized interpolation amount between the second and third points.</param>
		/// <param name="o">The output vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void CatmullRom(in Vec4 f1, in Vec4 f2, in Vec4 f3, in Vec4 f4, float amt, out Vec4 o)
		{
			o.X = CatmullRom(f1.X, f2.X, f3.X, f4.X, amt);
			o.Y = CatmullRom(f1.Y, f2.Y, f3.Y, f4.Y, amt);
			o.Z = CatmullRom(f1.Z, f2.Z, f3.Z, f4.Z, amt);
			o.W = CatmullRom(f1.W, f2.W, f3.W, f4.W, amt);
		}

		/// <summary>
		/// Performs a cubic Hermite spline between the two control points using their tangents.
		/// </summary>
		/// <param name="f1">The first control point.</param>
		/// <param name="t1">The tangent of the first control point.</param>
		/// <param name="f2">The second control point.</param>
		/// <param name="t2">The tangent of the second control point.</param>
		/// <param name="amt">The normalized interpolation weight.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 Hermite(in Vec4 f1, in Vec4 t1, in Vec4 f2, in Vec4 t2, float amt) => new Vec4(
			Hermite(f1.X, t1.X, f2.X, t2.X, amt),
			Hermite(f1.Y, t1.Y, f2.Y, t2.Y, amt),
			Hermite(f1.Z, t1.Z, f2.Z, t2.Z, amt),
			Hermite(f1.W, t1.W, f2.W, t2.W, amt)
		);

		/// <summary>
		/// Performs a cubic Hermite spline between the two control points using their tangents.
		/// </summary>
		/// <param name="f1">The first control point.</param>
		/// <param name="t1">The tangent of the first control point.</param>
		/// <param name="f2">The second control point.</param>
		/// <param name="t2">The tangent of the second control point.</param>
		/// <param name="amt">The normalized interpolation weight.</param>
		/// <param name="o">The output vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Hermite(in Vec4 f1, in Vec4 t1, in Vec4 f2, in Vec4 t2, float amt, out Vec4 o)
		{
			o.X = Hermite(f1.X, t1.X, f2.X, t2.X, amt);
			o.Y = Hermite(f1.Y, t1.Y, f2.Y, t2.Y, amt);
			o.Z = Hermite(f1.Z, t1.Z, f2.Z, t2.Z, amt);
			o.W = Hermite(f1.W, t1.W, f2.W, t2.W, amt);
		}

		/// <summary>
		/// Performs a smooth (tangent = 0) cubic-interpolation between the two vectors.
		/// </summary>
		/// <param name="f1">The source vector.</param>
		/// <param name="f2">The destination vector.</param>
		/// <param name="amt">The normalized weight, values outside [0, 1] are clamped.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 SmoothLerp(in Vec4 f1, in Vec4 f2, float amt) => new Vec4(
			Hermite(f1.X, 0, f2.X, 0, amt),
			Hermite(f1.Y, 0, f2.Y, 0, amt),
			Hermite(f1.Z, 0, f2.Z, 0, amt),
			Hermite(f1.W, 0, f2.W, 0, amt)
		);

		/// <summary>
		/// Performs a smooth (tangent = 0) cubic-interpolation between the two vectors.
		/// </summary>
		/// <param name="f1">The source vector.</param>
		/// <param name="f2">The destination vector.</param>
		/// <param name="amt">The normalized weight, values outside [0, 1] are clamped.</param>
		/// <param name="o">The output vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void SmoothLerp(in Vec4 f1, in Vec4 f2, float amt, out Vec4 o)
		{
			o.X = Hermite(f1.X, 0, f2.X, 0, amt);
			o.Y = Hermite(f1.Y, 0, f2.Y, 0, amt);
			o.Z = Hermite(f1.Z, 0, f2.Z, 0, amt);
			o.W = Hermite(f1.W, 0, f2.W, 0, amt);
		}
		#endregion // Vec4

		#region Matrix
		/// <summary>
		/// Component-wise linear interpolation between the matrices.
		/// </summary>
		/// <param name="m1">The source matrix.</param>
		/// <param name="m2">The destination matrix.</param>
		/// <param name="amt">The interpolation weight.</param>
		public static Matrix Lerp(in Matrix m1, in Matrix m2, float amt)
		{
			Lerp(m1, m2, amt, out Matrix o);
			return o;
		}

		/// <summary>
		/// Component-wise linear interpolation between the matrices.
		/// </summary>
		/// <param name="m1">The source matrix.</param>
		/// <param name="m2">The destination matrix.</param>
		/// <param name="amt">The interpolation weight.</param>
		/// <param name="o">The output matrix.</param>
		public static void Lerp(in Matrix m1, in Matrix m2, float amt, out Matrix o)
		{
			o.M00 = m1.M00 + ((m2.M00 - m1.M00) * amt);
			o.M10 = m1.M10 + ((m2.M10 - m1.M10) * amt);
			o.M20 = m1.M20 + ((m2.M20 - m1.M20) * amt);
			o.M30 = m1.M30 + ((m2.M30 - m1.M30) * amt);
			o.M01 = m1.M01 + ((m2.M01 - m1.M01) * amt);
			o.M11 = m1.M11 + ((m2.M11 - m1.M11) * amt);
			o.M21 = m1.M21 + ((m2.M21 - m1.M21) * amt);
			o.M31 = m1.M31 + ((m2.M31 - m1.M31) * amt);
			o.M02 = m1.M02 + ((m2.M02 - m1.M02) * amt);
			o.M12 = m1.M12 + ((m2.M12 - m1.M12) * amt);
			o.M22 = m1.M22 + ((m2.M22 - m1.M22) * amt);
			o.M32 = m1.M32 + ((m2.M32 - m1.M32) * amt);
			o.M03 = m1.M03 + ((m2.M03 - m1.M03) * amt);
			o.M13 = m1.M13 + ((m2.M13 - m1.M13) * amt);
			o.M23 = m1.M23 + ((m2.M23 - m1.M23) * amt);
			o.M33 = m1.M33 + ((m2.M33 - m1.M33) * amt);
		}

		/// <summary>
		/// Component-wise linear interpolation between the matrices, with better edge case handling for values that are
		/// greatly mismatched in magnitude.
		/// </summary>
		/// <param name="m1">The source matrix.</param>
		/// <param name="m2">The destination matrix.</param>
		/// <param name="amt">The interpolation weight.</param>
		public static Matrix LerpPrecise(in Matrix m1, in Matrix m2, float amt)
		{
			LerpPrecise(m1, m2, amt, out Matrix o);
			return o;
		}

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

			o.M00 = (amt2 * m1.M00) + (m2.M00 * amt);
			o.M10 = (amt2 * m1.M10) + (m2.M10 * amt);
			o.M20 = (amt2 * m1.M20) + (m2.M20 * amt);
			o.M30 = (amt2 * m1.M30) + (m2.M30 * amt);
			o.M01 = (amt2 * m1.M01) + (m2.M01 * amt);
			o.M11 = (amt2 * m1.M11) + (m2.M11 * amt);
			o.M21 = (amt2 * m1.M21) + (m2.M21 * amt);
			o.M31 = (amt2 * m1.M31) + (m2.M31 * amt);
			o.M02 = (amt2 * m1.M02) + (m2.M02 * amt);
			o.M12 = (amt2 * m1.M12) + (m2.M12 * amt);
			o.M22 = (amt2 * m1.M22) + (m2.M22 * amt);
			o.M32 = (amt2 * m1.M32) + (m2.M32 * amt);
			o.M03 = (amt2 * m1.M03) + (m2.M03 * amt);
			o.M13 = (amt2 * m1.M13) + (m2.M13 * amt);
			o.M23 = (amt2 * m1.M23) + (m2.M23 * amt);
			o.M33 = (amt2 * m1.M33) + (m2.M33 * amt);
		}

		/// <summary>
		/// Performs a smooth cubic-interpolation between the matrices.
		/// </summary>
		/// <param name="m1">The source matrix.</param>
		/// <param name="m2">The destination matrix.</param>
		/// <param name="amt">The interpolation weight.</param>
		public static Matrix SmoothLerp(in Matrix m1, in Matrix m2, float amt)
		{
			SmoothLerp(m1, m2, amt, out Matrix o);
			return o;
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
			float norm = Mathf.UnitClamp(amt);

			o.M00 = Hermite(m1.M00, 0, m2.M00, 0, norm);
			o.M10 = Hermite(m1.M10, 0, m2.M10, 0, norm);
			o.M20 = Hermite(m1.M20, 0, m2.M20, 0, norm);
			o.M30 = Hermite(m1.M30, 0, m2.M30, 0, norm);
			o.M01 = Hermite(m1.M01, 0, m2.M01, 0, norm);
			o.M11 = Hermite(m1.M11, 0, m2.M11, 0, norm);
			o.M21 = Hermite(m1.M21, 0, m2.M21, 0, norm);
			o.M31 = Hermite(m1.M31, 0, m2.M31, 0, norm);
			o.M02 = Hermite(m1.M02, 0, m2.M02, 0, norm);
			o.M12 = Hermite(m1.M12, 0, m2.M12, 0, norm);
			o.M22 = Hermite(m1.M22, 0, m2.M22, 0, norm);
			o.M32 = Hermite(m1.M32, 0, m2.M32, 0, norm);
			o.M03 = Hermite(m1.M03, 0, m2.M03, 0, norm);
			o.M13 = Hermite(m1.M13, 0, m2.M13, 0, norm);
			o.M23 = Hermite(m1.M23, 0, m2.M23, 0, norm);
			o.M33 = Hermite(m1.M33, 0, m2.M33, 0, norm);
		}
		#endregion // Matrix

		#region Quaternion
		/// <summary>
		/// Performs a linear interpolation between the rotations represented by the quaternions.
		/// </summary>
		/// <param name="q1">The source quaternion.</param>
		/// <param name="q2">The destination quaternion.</param>
		/// <param name="amt">The interpolation weight.</param>
		public static Quaternion Lerp(in Quaternion q1, in Quaternion q2, float amt)
		{
			Lerp(q1, q2, amt, out Quaternion o);
			return o;
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

			dot = Mathf.Sqrt((o.X * o.X) + (o.Y * o.Y) + (o.Z * o.Z) + (o.W * o.W));
			o.X /= dot;
			o.Y /= dot;
			o.Z /= dot;
			o.W /= dot;
		}

		/// <summary>
		/// Performs a spherical linear interpolation between the rotations represented by the quaternions.
		/// </summary>
		/// <param name="q1">The source quaternion.</param>
		/// <param name="q2">The destination quaternion.</param>
		/// <param name="amt">The interpolation weight.</param>
		public static Quaternion Slerp(in Quaternion q1, in Quaternion q2, float amt)
		{
			Slerp(q1, q2, amt, out Quaternion o);
			return o;
		}

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
				float acos = Mathf.Acos(dot);
				float isin = 1 / Mathf.Sin(acos);
				a2 = Mathf.Sin((1 - amt) * acos) * isin;
				a1 = ltz ? (-Mathf.Sin(amt * acos) * isin) : (Mathf.Sin(amt * acos) * isin);
			}

			o.X = (a2 * q1.X) + (a1 * q2.X);
			o.Y = (a2 * q1.Y) + (a1 * q2.Y);
			o.Z = (a2 * q1.Z) + (a1 * q2.Z);
			o.W = (a2 * q1.W) + (a1 * q2.W);
		}
		#endregion // Quaternion
	}
}
