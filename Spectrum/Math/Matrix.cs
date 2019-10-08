/*
 * GNU LGPLv3 License - Copyright (c) The Spectrum Team
 * This file is subject to the terms and conditions of the GNU LGPLv3 license, the text of which can be found in the
 * 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/LGPL-3.0>.
 */
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Spectrum
{
	/// <summary>
	/// A 4x4 matrix of numbers, stored in and accessed with row-major order.
	/// </summary>
	/// <remarks>
	/// The row-major notation creates a matrix like:
	/// <code>
	///		M00 M01 M02 M03
	///		M10 M11 M12 M13
	///		M20 M21 M22 M23
	///		M30 M31 M32 M33
	/// </code>
	/// The operations in this type have been carefully accelerated with SSE instructions. There was careful testing
	/// to ensure each operation was actually faster with SSE. The SSE speedups are often only noticable in Release
	/// builds.
	/// </remarks>
	[StructLayout(LayoutKind.Explicit, Pack = 0, Size = 16 * sizeof(float))]
	public unsafe struct Matrix : IEquatable<Matrix>
	{
		#region Constant Matrices
		/// <summary>
		/// The identity matrix.
		/// </summary>
		public static readonly Matrix Identity = new Matrix(1);
		/// <summary>
		/// A matrix with all zero entries.
		/// </summary>
		public static readonly Matrix Zero = new Matrix(0);
		#endregion // Constant Matrices

		#region Fields
		/// <summary>
		/// Value at row 0, column 0.
		/// </summary>
		[FieldOffset(0)]
		public float M00;
		/// <summary>
		/// Value at row 0, column 1.
		/// </summary>
		[FieldOffset(1 * sizeof(float))]
		public float M01;
		/// <summary>
		/// Value at row 0, column 2.
		/// </summary>
		[FieldOffset(2 * sizeof(float))]
		public float M02;
		/// <summary>
		/// Value at row 0, column 3.
		/// </summary>
		[FieldOffset(3 * sizeof(float))]
		public float M03;
		/// <summary>
		/// Value at row 1, column 0.
		/// </summary>
		[FieldOffset(4 * sizeof(float))]
		public float M10;
		/// <summary>
		/// Value at row 1, column 1.
		/// </summary>
		[FieldOffset(5 * sizeof(float))]
		public float M11;
		/// <summary>
		/// Value at row 1, column 2.
		/// </summary>
		[FieldOffset(6 * sizeof(float))]
		public float M12;
		/// <summary>
		/// Value at row 1, column 3.
		/// </summary>
		[FieldOffset(7 * sizeof(float))]
		public float M13;
		/// <summary>
		/// Value at row 2, column 0.
		/// </summary>
		[FieldOffset(8 * sizeof(float))]
		public float M20;
		/// <summary>
		/// Value at row 2, column 1.
		/// </summary>
		[FieldOffset(9 * sizeof(float))]
		public float M21;
		/// <summary>
		/// Value at row 2, column 2.
		/// </summary>
		[FieldOffset(10 * sizeof(float))]
		public float M22;
		/// <summary>
		/// Value at row 2, column 3.
		/// </summary>
		[FieldOffset(11 * sizeof(float))]
		public float M23;
		/// <summary>
		/// Value at row 3, column 0.
		/// </summary>
		[FieldOffset(12 * sizeof(float))]
		public float M30;
		/// <summary>
		/// Value at row 3, column 1.
		/// </summary>
		[FieldOffset(13 * sizeof(float))]
		public float M31;
		/// <summary>
		/// Value at row 3, column 2.
		/// </summary>
		[FieldOffset(14 * sizeof(float))]
		public float M32;
		/// <summary>
		/// Value at row 3, column 3.
		/// </summary>
		[FieldOffset(15 * sizeof(float))]
		public float M33;

		// Fixed size buffer for fast access to the values
		[FieldOffset(0)]
		private fixed float _m[16];
		#endregion // Fields

		#region Indexers
		/// <summary>
		/// Gets/Sets a single member of the matrix in order of packing.
		/// </summary>
		/// <param name="i">The index of the member, from 0-15 inclusive.</param>
		public float this[int i]
		{
			readonly get => _m[i];
			set => _m[i] = value;
		}

		/// <summary>
		/// Gets/Sets a single member of the matrix by its row/column position.
		/// </summary>
		/// <param name="r">The row of the member.</param>
		/// <param name="c">The column of the member.</param>
		public float this[int r, int c]
		{
			readonly get => (r >= 0 && r < 4 && c >= 0 && c < 4) ? _m[(r * 4) + c] :
				throw new IndexOutOfRangeException($"Invalid matrix indices [{r}, {c}].");
			set => _m[(r >= 0 && r < 4 && c >= 0 && c < 4) ? (r * 4) + c :
					throw new IndexOutOfRangeException($"Invalid matrix indices [{r}, {c}].")] = value;
		}
		#endregion // Indexers

		#region Ctor
		/// <summary>
		/// Creates a new matrix with the given components.
		/// </summary>
		public Matrix(float m00, float m01, float m02, float m03, float m10, float m11, float m12, float m13,
					  float m20, float m21, float m22, float m23, float m30, float m31, float m32, float m33)
		{
			M00 = m00; M01 = m01; M02 = m02; M03 = m03;
			M10 = m10; M11 = m11; M12 = m12; M13 = m13;
			M20 = m20; M21 = m21; M22 = m22; M23 = m23;
			M30 = m30; M31 = m31; M32 = m32; M33 = m33;
		}

		/// <summary>
		/// Creates a new matrix with the given rows.
		/// </summary>
		public Matrix(in Vec4 r0, in Vec4 r1, in Vec4 r2, in Vec4 r3)
		{
			M00 = r0.X; M01 = r0.Y; M02 = r0.Z; M03 = r0.W;
			M10 = r1.X; M11 = r1.Y; M12 = r1.Z; M13 = r1.W;
			M20 = r2.X; M21 = r2.Y; M22 = r2.Z; M23 = r2.W;
			M30 = r3.X; M31 = r3.Y; M32 = r3.Z; M33 = r3.W;
		}

		/// <summary>
		/// Creates a new diagonal matrix with the given value.
		/// </summary>
		/// <param name="f">The value for the diagonal components.</param>
		public Matrix(float f)
		{
			M00 = f; M01 = 0; M02 = 0; M03 = 0;
			M10 = 0; M11 = f; M12 = 0; M13 = 0;
			M20 = 0; M21 = 0; M22 = f; M23 = 0;
			M30 = 0; M31 = 0; M32 = 0; M33 = f;
		}
		#endregion // Ctor

		#region Overrides
		readonly bool IEquatable<Matrix>.Equals(Matrix m) => m == this;

		public readonly override bool Equals(object obj) => (obj is Matrix) && ((Matrix)obj == this);

		// Not the best hash, but don't use Matrices as keys to begin with
		public unsafe readonly override int GetHashCode()
		{
			fixed (float* m = _m)
			{
				unchecked
				{
					long hash = 17;
					hash = (hash * 23) + *(long*)(m + 0) + *(long*)(m + 2);
					hash = (hash * 23) + *(long*)(m + 4) + *(long*)(m + 6);
					hash = (hash * 23) + *(long*)(m + 8) + *(long*)(m + 10);
					hash = (hash * 23) + *(long*)(m + 12) + *(long*)(m + 14);
					return (int)((hash & 0xFFFFFFFF) ^ (hash >> 32));
				}
			}
		}

		public readonly override string ToString() =>
			$"{{{{{M00} {M01} {M02} {M03}}}" +
			  $"{{{M10} {M11} {M12} {M13}}}" +
			  $"{{{M20} {M21} {M22} {M23}}}" +
			  $"{{{M30} {M31} {M32} {M33}}}}}";
		#endregion // Overrides

		#region Basic Math Operations
		/// <summary>
		/// Adds two matricies together element-wise.
		/// </summary>
		/// <param name="l">The first matrix.</param>
		/// <param name="r">The second matrix.</param>
		/// <returns>The element-wise sum of the two matrices.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix Add(in Matrix l, in Matrix r)
		{
			Matrix.Add(l, r, out var o);
			return o;
		}

		/// <summary>
		/// Adds two matricies together element-wise.
		/// </summary>
		/// <param name="l">The first matrix.</param>
		/// <param name="r">The second matrix.</param>
		/// <param name="o">The element-wise sum of the two matrices.</param>
		public static void Add(in Matrix l, in Matrix r, out Matrix o)
		{
			o = default;
			fixed (float* lp = l._m, rp = r._m, op = o._m)
			{
				Vector128<float>* lvec = (Vector128<float>*)lp,
								  rvec = (Vector128<float>*)rp,
								  ovec = (Vector128<float>*)op;
				ovec[0] = Sse.Add(lvec[0], rvec[0]);
				ovec[1] = Sse.Add(lvec[1], rvec[1]);
				ovec[2] = Sse.Add(lvec[2], rvec[2]);
				ovec[3] = Sse.Add(lvec[3], rvec[3]);
			}
		}

		/// <summary>
		/// Subtracts one matrix from the other element-wise.
		/// </summary>
		/// <param name="l">The first matrix.</param>
		/// <returns>The element-wise difference of the two matrices.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix Subtract(in Matrix l, in Matrix r)
		{
			Matrix.Subtract(l, r, out var o);
			return o;
		}

		/// <summary>
		/// Subtracts one matrix from the other element-wise.
		/// </summary>
		/// <param name="l">The first matrix.</param>
		/// <param name="r">The second matrix.</param>
		/// <param name="o">The element-wise difference of the two matrices.</param>
		public static void Subtract(in Matrix l, in Matrix r, out Matrix o)
		{
			o = default;
			fixed (float* lp = l._m, rp = r._m, op = o._m)
			{
				Vector128<float>* lvec = (Vector128<float>*)lp,
								  rvec = (Vector128<float>*)rp,
								  ovec = (Vector128<float>*)op;
				ovec[0] = Sse.Subtract(lvec[0], rvec[0]);
				ovec[1] = Sse.Subtract(lvec[1], rvec[1]);
				ovec[2] = Sse.Subtract(lvec[2], rvec[2]);
				ovec[3] = Sse.Subtract(lvec[3], rvec[3]);
			}
		}

		/// <summary>
		/// Performs the element-wise negation of the matrix.
		/// </summary>
		/// <param name="m">The matrix to negate.</param>
		/// <returns>The element-wise negated matrix.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix Negate(in Matrix m)
		{
			Matrix.Negate(m, out var o);
			return o;
		}

		/// <summary>
		/// Performs the element-wise negation of the matrix.
		/// </summary>
		/// <param name="m">The matrix to negate.</param>
		/// <param name="o">The element-wise negated matrix.</param>
		public static void Negate(in Matrix m, out Matrix o)
		{
			o = default;
			fixed (float* mp = m._m, op = o._m)
			{
				Vector128<float>* mvec = (Vector128<float>*)mp, ovec = (Vector128<float>*)op;
				Vector128<float> neg = Vector128.Create(-1f);
				ovec[0] = Sse.Multiply(mvec[0], neg);
				ovec[1] = Sse.Multiply(mvec[1], neg);
				ovec[2] = Sse.Multiply(mvec[2], neg);
				ovec[3] = Sse.Multiply(mvec[3], neg);
			}
		}

		/// <summary>
		/// Performs an element-wise multiplication of a matrix by a scalar.
		/// </summary>
		/// <param name="l">The matrix.</param>
		/// <param name="r">The scalar.</param>
		/// <returns>The element-wise multiplication.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix Multiply(in Matrix l, float r)
		{
			Matrix.Multiply(l, r, out var o);
			return o;
		}

		/// <summary>
		/// Performs an element-wise multiplication of a matrix by a scalar.
		/// </summary>
		/// <param name="l">The matrix.</param>
		/// <param name="r">The scalar.</param>
		/// <param name="o">The element-wise multiplication.</param>
		public static void Multiply(in Matrix l, float r, out Matrix o)
		{
			o = default;
			fixed (float* lp = l._m, op = o._m)
			{
				Vector128<float>* lvec = (Vector128<float>*)lp, ovec = (Vector128<float>*)op;
				Vector128<float> fact = Vector128.Create(r);
				ovec[0] = Sse.Multiply(lvec[0], fact);
				ovec[1] = Sse.Multiply(lvec[1], fact);
				ovec[2] = Sse.Multiply(lvec[2], fact);
				ovec[3] = Sse.Multiply(lvec[3], fact);
			}
		}

		/// <summary>
		/// Multiplies two matrices using standard linear algebra matrix multiplication rules.
		/// </summary>
		/// <param name="l">The first matrix.</param>
		/// <param name="r">The second matrix.</param>
		/// <returns>The multiplied matrix.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix Multiply(in Matrix l, in Matrix r)
		{
			Matrix.Multiply(l, r, out var o);
			return o;
		}

		/// <summary>
		/// Multiplies two matrices using standard linear algebra matrix multiplication rules.
		/// </summary>
		/// <param name="l">The first matrix.</param>
		/// <param name="r">The second matrix.</param>
		/// <param name="o">The multiplied matrix.</param>
		public static void Multiply(in Matrix l, in Matrix r, out Matrix o)
		{
			o = default;
			fixed (float* lp = l._m, rp = r._m, op = o._m)
			{
				Vector128<float>* rvec = (Vector128<float>*)rp,
								  ovec = (Vector128<float>*)op;
				_lincomb(lp + 0, rvec, ovec + 0);
				_lincomb(lp + 4, rvec, ovec + 1);
				_lincomb(lp + 8, rvec, ovec + 2);
				_lincomb(lp + 12, rvec, ovec + 3);
			}

			static void _lincomb(float* lp, Vector128<float>* rp, Vector128<float>* op)
			{
				var res = Sse.Multiply(Vector128.Create(lp[0]), rp[0]);
				res = Sse.Add(res, Sse.Multiply(Vector128.Create(lp[1]), rp[1]));
				res = Sse.Add(res, Sse.Multiply(Vector128.Create(lp[2]), rp[2]));
				*op = Sse.Add(res, Sse.Multiply(Vector128.Create(lp[3]), rp[3]));
			}
		}

		/// <summary>
		/// Performs an element-wise division of a matrix by a scalar.
		/// </summary>
		/// <param name="l">The matrix.</param>
		/// <param name="r">The scalar.</param>
		/// <returns>The element-wise division.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix Divide(in Matrix l, float r)
		{
			Matrix.Divide(l, r, out var o);
			return o;
		}

		/// <summary>
		/// Performs an element-wise division of a matrix by a scalar.
		/// </summary>
		/// <param name="l">The matrix.</param>
		/// <param name="r">The scalar.</param>
		/// <param name="o">The element-wise division.</param>
		public static void Divide(in Matrix l, float r, out Matrix o)
		{
			o = default;
			fixed (float* lp = l._m, op = o._m)
			{
				Vector128<float>* lvec = (Vector128<float>*)lp, ovec = (Vector128<float>*)op;
				Vector128<float> fact = Vector128.Create(1f / r);
				ovec[0] = Sse.Multiply(lvec[0], fact);
				ovec[1] = Sse.Multiply(lvec[1], fact);
				ovec[2] = Sse.Multiply(lvec[2], fact);
				ovec[3] = Sse.Multiply(lvec[3], fact);
			}
		}

		/// <summary>
		/// Checks if the matrices are approximately equal to each other.
		/// </summary>
		/// <param name="l">First matrix.</param>
		/// <param name="r">Second matrix.</param>
		/// <param name="eps">The maximum difference for any element to be considered nearly equal.</param>
		public static bool NearlyEqual(in Matrix l, in Matrix r, float eps = MathHelper.MAX_REL_EPS_F) =>
			MathHelper.NearlyEqual(l.M00, r.M00, eps) && MathHelper.NearlyEqual(l.M01, r.M01, eps) && MathHelper.NearlyEqual(l.M02, r.M02, eps) && MathHelper.NearlyEqual(l.M03, r.M03, eps) &&
			MathHelper.NearlyEqual(l.M10, r.M10, eps) && MathHelper.NearlyEqual(l.M11, r.M11, eps) && MathHelper.NearlyEqual(l.M12, r.M12, eps) && MathHelper.NearlyEqual(l.M13, r.M13, eps) &&
			MathHelper.NearlyEqual(l.M20, r.M20, eps) && MathHelper.NearlyEqual(l.M21, r.M21, eps) && MathHelper.NearlyEqual(l.M22, r.M22, eps) && MathHelper.NearlyEqual(l.M23, r.M23, eps) &&
			MathHelper.NearlyEqual(l.M30, r.M30, eps) && MathHelper.NearlyEqual(l.M31, r.M31, eps) && MathHelper.NearlyEqual(l.M32, r.M32, eps) && MathHelper.NearlyEqual(l.M33, r.M33, eps);

		/// <summary>
		/// Checks if the passed matrix is within a certain limit of the <see cref="Identity"/> matrix.
		/// </summary>
		/// <param name="m">The matrix to check.</param>
		/// <param name="eps">The maximum different for any element to be considered nearly equal.</param>
		public static bool NearlyIdentity(in Matrix m, float eps = MathHelper.MAX_REL_EPS_F) =>
			MathHelper.NearlyEqual(m.M00, 1, eps) && MathHelper.NearlyEqual(m.M01, 0, eps) && MathHelper.NearlyEqual(m.M02, 0, eps) && MathHelper.NearlyEqual(m.M03, 0, eps) &&
			MathHelper.NearlyEqual(m.M10, 0, eps) && MathHelper.NearlyEqual(m.M11, 1, eps) && MathHelper.NearlyEqual(m.M12, 0, eps) && MathHelper.NearlyEqual(m.M13, 0, eps) &&
			MathHelper.NearlyEqual(m.M20, 0, eps) && MathHelper.NearlyEqual(m.M21, 0, eps) && MathHelper.NearlyEqual(m.M22, 1, eps) && MathHelper.NearlyEqual(m.M23, 0, eps) &&
			MathHelper.NearlyEqual(m.M30, 0, eps) && MathHelper.NearlyEqual(m.M31, 0, eps) && MathHelper.NearlyEqual(m.M32, 0, eps) && MathHelper.NearlyEqual(m.M33, 1, eps);
		#endregion // Basic Math Operations

		#region Matrix Operations
		/// <summary>
		/// Transposes the matrix.
		/// </summary>
		/// <param name="m">The input matrix.</param>
		/// <returns>The transposed matrix.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix Transpose(in Matrix m)
		{
			Matrix.Transpose(m, out var o);
			return o;
		}

		/// <summary>
		/// Transposes the matrix.
		/// </summary>
		/// <param name="m">The input matrix.</param>
		/// <param name="o">The transposed matrix.</param>
		public static void Transpose(in Matrix m, out Matrix o)
		{
			o = default;
			fixed (float* mp = m._m, op = o._m)
			{
				Vector128<float>* mvec = (Vector128<float>*)mp, ovec = (Vector128<float>*)op;
				var t1 = Sse.UnpackLow(mvec[0], mvec[1]);
				var t2 = Sse.UnpackLow(mvec[2], mvec[3]);
				var t3 = Sse.UnpackHigh(mvec[0], mvec[1]);
				var t4 = Sse.UnpackHigh(mvec[2], mvec[3]);
				ovec[0] = Sse.MoveLowToHigh(t1, t2);
				ovec[1] = Sse.MoveHighToLow(t2, t1);
				ovec[2] = Sse.MoveLowToHigh(t3, t4);
				ovec[3] = Sse.MoveHighToLow(t4, t3);
			}
		}

		/// <summary>
		/// Calculates the trace of the matrix.
		/// </summary>
		/// <param name="m">The matrix to get the trace of.</param>
		/// <returns>The trace of the matrix.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Trace(in Matrix m) => m.M00 + m.M11 + m.M22 + m.M33;

		/// <summary>
		/// Calculates the trace of the matrix.
		/// </summary>
		/// <param name="m">The matrix to get the trace of.</param>
		/// <param name="o">The trace of the matrix.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Trace(in Matrix m, out float o) => o = m.M00 + m.M11 + m.M22 + m.M33;

		/// <summary>
		/// Calculates the determinant of the matrix.
		/// </summary>
		/// <param name="m">The matrix to get the determinant for.</param>
		/// <returns>The matrix determinant.</returns>
		public static float Determinant(in Matrix m) =>
			(m.M03 * m.M12 * m.M21 * m.M30) - (m.M02 * m.M13 * m.M21 * m.M30) -
			(m.M03 * m.M11 * m.M22 * m.M30) + (m.M01 * m.M13 * m.M22 * m.M30) +
			(m.M02 * m.M11 * m.M23 * m.M30) - (m.M01 * m.M12 * m.M23 * m.M30) -
			(m.M03 * m.M12 * m.M20 * m.M31) + (m.M02 * m.M13 * m.M20 * m.M31) +
			(m.M03 * m.M10 * m.M22 * m.M31) - (m.M00 * m.M13 * m.M22 * m.M31) -
			(m.M02 * m.M10 * m.M23 * m.M31) + (m.M00 * m.M12 * m.M23 * m.M31) +
			(m.M03 * m.M11 * m.M20 * m.M32) - (m.M01 * m.M13 * m.M20 * m.M32) -
			(m.M03 * m.M10 * m.M21 * m.M32) + (m.M00 * m.M13 * m.M21 * m.M32) +
			(m.M01 * m.M10 * m.M23 * m.M32) - (m.M00 * m.M11 * m.M23 * m.M32) -
			(m.M02 * m.M11 * m.M20 * m.M33) + (m.M01 * m.M12 * m.M20 * m.M33) +
			(m.M02 * m.M10 * m.M21 * m.M33) - (m.M00 * m.M12 * m.M21 * m.M33) -
			(m.M01 * m.M10 * m.M22 * m.M33) + (m.M00 * m.M11 * m.M22 * m.M33) ;

		/// <summary>
		/// Calculates the determinant of the matrix.
		/// </summary>
		/// <param name="m">The matrix to get the determinant for.</param>
		/// <param name="o">The matrix determinant.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Determinant(in Matrix m, out float o) => o = Matrix.Determinant(m);

		/// <summary>
		/// Calculates the inverse of the matrix. Gives <see cref="Matrix.Zero"/> if the matrix cannot be inverted.
		/// </summary>
		/// <param name="m">The matrix to invert.</param>
		/// <returns>The inverted matrix.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix Invert(in Matrix m)
		{
			Matrix.Invert(m, out var o);
			return o;
		}

		/// <summary>
		/// Calculates the inverse of the matrix. Gives <see cref="Matrix.Zero"/> if the matrix cannot be inverted.
		/// </summary>
		/// <param name="m">The matrix to invert.</param>
		/// <param name="o">The inverted matrix.</param>
		public static void Invert(in Matrix m, out Matrix o)
		{
			// Solution based on Laplace Expansion, found at https://stackoverflow.com/a/9614511
			double s0 = (double)m.M00 * m.M11 - (double)m.M10 * m.M01;
			double s1 = (double)m.M00 * m.M12 - (double)m.M10 * m.M02;
			double s2 = (double)m.M00 * m.M13 - (double)m.M10 * m.M03;
			double s3 = (double)m.M01 * m.M12 - (double)m.M11 * m.M02;
			double s4 = (double)m.M01 * m.M13 - (double)m.M11 * m.M03;
			double s5 = (double)m.M02 * m.M13 - (double)m.M12 * m.M03;

			double c5 = (double)m.M22 * m.M33 - (double)m.M32 * m.M23;
			double c4 = (double)m.M21 * m.M33 - (double)m.M31 * m.M23;
			double c3 = (double)m.M21 * m.M32 - (double)m.M31 * m.M22;
			double c2 = (double)m.M20 * m.M33 - (double)m.M30 * m.M23;
			double c1 = (double)m.M20 * m.M32 - (double)m.M30 * m.M22;
			double c0 = (double)m.M20 * m.M31 - (double)m.M30 * m.M21;

			double det = (s0 * c5 - s1 * c4 + s2 * c3 + s3 * c2 - s4 * c1 + s5 * c0);
			if (MathHelper.NearlyEqual(det, 0, 1e-10))
			{
				o = Matrix.Zero;
				return;
			}
			double invdet = 1.0 / det;

			o.M00 = (float)((m.M11 * c5 - m.M12 * c4 + m.M13 * c3) * invdet);
			o.M01 = (float)((-m.M01 * c5 + m.M02 * c4 - m.M03 * c3) * invdet);
			o.M02 = (float)((m.M31 * s5 - m.M32 * s4 + m.M33 * s3) * invdet);
			o.M03 = (float)((-m.M21 * s5 + m.M22 * s4 - m.M23 * s3) * invdet);

			o.M10 = (float)((-m.M10 * c5 + m.M12 * c2 - m.M13 * c1) * invdet);
			o.M11 = (float)((m.M00 * c5 - m.M02 * c2 + m.M03 * c1) * invdet);
			o.M12 = (float)((-m.M30 * s5 + m.M32 * s2 - m.M33 * s1) * invdet);
			o.M13 = (float)((m.M20 * s5 - m.M22 * s2 + m.M23 * s1) * invdet);

			o.M20 = (float)((m.M10 * c4 - m.M11 * c2 + m.M13 * c0) * invdet);
			o.M21 = (float)((-m.M00 * c4 + m.M01 * c2 - m.M03 * c0) * invdet);
			o.M22 = (float)((m.M30 * s4 - m.M31 * s2 + m.M33 * s0) * invdet);
			o.M23 = (float)((-m.M20 * s4 + m.M21 * s2 - m.M23 * s0) * invdet);

			o.M30 = (float)((-m.M10 * c3 + m.M11 * c1 - m.M12 * c0) * invdet);
			o.M31 = (float)((m.M00 * c3 - m.M01 * c1 + m.M02 * c0) * invdet);
			o.M32 = (float)((-m.M30 * s3 + m.M31 * s1 - m.M32 * s0) * invdet);
			o.M33 = (float)((m.M20 * s3 - m.M21 * s1 + m.M22 * s0) * invdet);
		}
		#endregion // Matrix Operations

		#region Vector Operations
		/// <summary>
		/// Transforms the vector using standard matrix/vector multiplication.
		/// </summary>
		/// <param name="m">The multiplicitive matrix.</param>
		/// <param name="v">The pre-transform vector.</param>
		/// <returns>The transformed vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 Transform(in Matrix m, in Vec3 v)
		{
			Matrix.Transform(m, v, out var o);
			return o;
		}

		/// <summary>
		/// Transforms the vector using standard matrix/vector multiplication.
		/// </summary>
		/// <param name="m">The multiplicitive matrix.</param>
		/// <param name="v">The pre-transform vector.</param>
		/// <param name="o">The transformed vector.</param>
		public static void Transform(in Matrix m, in Vec3 v, out Vec3 o)
		{
			o.X = (m.M00 * v.X) + (m.M10 * v.Y) + (m.M20 * v.Z) + m.M30;
			o.Y = (m.M01 * v.X) + (m.M11 * v.Y) + (m.M21 * v.Z) + m.M31;
			o.Z = (m.M02 * v.X) + (m.M12 * v.Y) + (m.M22 * v.Z) + m.M32;
		}

		/// <summary>
		/// Transforms the vector using standard matrix/vector multiplication.
		/// </summary>
		/// <param name="m">The multiplicitive matrix.</param>
		/// <param name="v">The pre-transform vector.</param>
		/// <returns>The transformed vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 Transform(in Matrix m, in Vec4 v)
		{
			Matrix.Transform(m, v, out var o);
			return o;
		}

		/// <summary>
		/// Transforms the vector using standard matrix/vector multiplication.
		/// </summary>
		/// <param name="m">The multiplicitive matrix.</param>
		/// <param name="v">The pre-transform vector.</param>
		/// <param name="o">The transformed vector.</param>
		public static void Transform(in Matrix m, in Vec4 v, out Vec4 o)
		{
			o.X = (m.M00 * v.X) + (m.M10 * v.Y) + (m.M20 * v.Z) + (m.M30 * v.W);
			o.Y = (m.M01 * v.X) + (m.M11 * v.Y) + (m.M21 * v.Z) + (m.M31 * v.W);
			o.Z = (m.M02 * v.X) + (m.M12 * v.Y) + (m.M22 * v.Z) + (m.M32 * v.W);
			o.W = (m.M03 * v.X) + (m.M13 * v.Y) + (m.M23 * v.Z) + (m.M33 * v.W);
		}
		#endregion // Vector Operations

		#region World Matrices
		#region Rotation
		/// <summary>
		/// Creates a matrix describing a rotation around the x-axis.
		/// </summary>
		/// <param name="angle">The angle of rotation, in radians.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix CreateRotationX(float angle)
		{
			Matrix.CreateRotationX(angle, out Matrix o);
			return o;
		}

		/// <summary>
		/// Creates a matrix describing a rotation around the x-axis.
		/// </summary>
		/// <param name="angle">The angle of rotation, in radians.</param>
		/// <param name="o">The rotation matrix.</param>
		public static void CreateRotationX(float angle, out Matrix o)
		{
			float c = MathF.Cos(angle);
			float s = MathF.Sin(angle);

			o.M00 = 1; o.M01 =  0; o.M02 = 0; o.M03 = 0;
			o.M10 = 0; o.M11 =  c; o.M12 = s; o.M13 = 0;
			o.M20 = 0; o.M21 = -s; o.M22 = c; o.M23 = 0;
			o.M30 = 0; o.M31 =  0; o.M32 = 0; o.M33 = 1;
		}

		/// <summary>
		/// Creates a matrix describing a rotation around the y-axis.
		/// </summary>
		/// <param name="angle">The angle of rotation, in radians.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix CreateRotationY(float angle)
		{
			CreateRotationY(angle, out Matrix o);
			return o;
		}

		/// <summary>
		/// Creates a matrix describing a rotation around the y-axis.
		/// </summary>
		/// <param name="angle">The angle of rotation, in radians.</param>
		/// <param name="o">The rotation matrix.</param>
		public static void CreateRotationY(float angle, out Matrix o)
		{
			float c = MathF.Cos(angle);
			float s = MathF.Sin(angle);

			o.M00 = c; o.M01 = 0; o.M02 = -s; o.M03 = 0;
			o.M10 = 0; o.M11 = 1; o.M12 =  0; o.M13 = 0;
			o.M20 = s; o.M21 = 0; o.M22 =  c; o.M23 = 0;
			o.M30 = 0; o.M31 = 0; o.M32 =  0; o.M33 = 1;
		}

		/// <summary>
		/// Creates a matrix describing a rotation around the z-axis.
		/// </summary>
		/// <param name="angle">The angle of rotation, in radians.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix CreateRotationZ(float angle)
		{
			CreateRotationZ(angle, out Matrix o);
			return o;
		}

		/// <summary>
		/// Creates a matrix describing a rotation around the z-axis.
		/// </summary>
		/// <param name="angle">The angle of rotation, in radians.</param>
		/// <param name="o">The rotation matrix.</param>
		public static void CreateRotationZ(float angle, out Matrix o)
		{
			float c = MathF.Cos(angle);
			float s = MathF.Sin(angle);

			o.M00 =  c; o.M01 = s; o.M02 = 0; o.M03 = 0;
			o.M10 = -s; o.M11 = c; o.M12 = 0; o.M13 = 0;
			o.M20 =  0; o.M21 = 0; o.M22 = 1; o.M23 = 0;
			o.M30 =  0; o.M31 = 0; o.M32 = 0; o.M33 = 1;
		}

		/// <summary>
		/// Creates a matrix describing a rotation moment around a specified axis.
		/// </summary>
		/// <param name="axis">The axis about which to rotate - must be normalized.</param>
		/// <param name="angle">The rotation angle in radians.</param>
		/// <returns>The rotation matrix.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix CreateAxisRotation(in Vec3 axis, float angle)
		{
			CreateAxisRotation(axis, angle, out var o);
			return o;
		}

		/// <summary>
		/// Creates a matrix describing a rotation moment around a specified axis.
		/// </summary>
		/// <param name="axis">The axis about which to rotate - must be normalized.</param>
		/// <param name="angle">The rotation angle in radians.</param>
		/// <param name="o">The rotation matrix.</param>
		public static void CreateAxisRotation(in Vec3 axis, float angle, out Matrix o)
		{
			float c = MathF.Cos(angle);
			float s = MathF.Sin(angle);
			float xx = axis.X * axis.X,
				  yy = axis.Y * axis.Y,
				  zz = axis.Z * axis.Z,
				  xy = axis.X * axis.Y,
				  xz = axis.X * axis.Z,
				  yz = axis.Y * axis.Z;

			o.M00 =           xx + (c * (1f - xx)); o.M01 = (xy - (c * xy)) + (s * axis.Z); o.M02 = (xz - (c * xz)) - (s * axis.Y); o.M03 = 0;
			o.M10 = (xy - (c * xy)) - (s * axis.Z); o.M11 =           yy + (c * (1f - yy)); o.M12 = (yz - (c * yz)) + (s * axis.X); o.M13 = 0;
			o.M20 = (xz - (c * xz)) + (s * axis.Y); o.M21 = (yz - (c * yz)) - (s * axis.X); o.M22 =           zz + (c * (1f - zz)); o.M23 = 0;
			o.M30 =                              0; o.M31 =                              0; o.M32 =                              0; o.M33 = 1;
		}

		// TODO: Quaternion, YawPitchRoll
		#endregion // Rotation

		#region Scale
		/// <summary>
		/// Creates a new scaling matrix with all three dimensions scaled identically.
		/// </summary>
		/// <param name="s">The scale factor.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix CreateScale(float s)
		{
			CreateScale(s, s, s, out Matrix o);
			return o;
		}

		/// <summary>
		/// Creates a new scaling matrix with all three dimensions scaled identically.
		/// </summary>
		/// <param name="s">The scale factor.</param>
		/// <param name="o">The scaling matrix.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void CreateScale(float s, out Matrix o)
		{
			CreateScale(s, s, s, out o);
		}

		/// <summary>
		/// Creates a new scaling matrix with different scaling factors for each axis.
		/// </summary>
		/// <param name="sx">The x-axis scaling factor.</param>
		/// <param name="sy">The y-axis scaling factor.</param>
		/// <param name="sz">The z-axis scaling factor.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix CreateScale(float sx, float sy, float sz)
		{
			CreateScale(sx, sy, sz, out Matrix o);
			return o;
		}

		/// <summary>
		/// Creates a new scaling matrix with different scaling factors for each axis.
		/// </summary>
		/// <param name="sx">The x-axis scaling factor.</param>
		/// <param name="sy">The y-axis scaling factor.</param>
		/// <param name="sz">The z-axis scaling factor.</param>
		/// <param name="o">The scaling matrix.</param>
		public static void CreateScale(float sx, float sy, float sz, out Matrix o)
		{
			o.M00 = sx; o.M01 =  0; o.M02 =  0; o.M03 = 0;
			o.M10 =  0; o.M11 = sy; o.M12 =  0; o.M13 = 0;
			o.M20 =  0; o.M21 =  0; o.M22 = sz; o.M23 = 0;
			o.M30 =  0; o.M31 =  0; o.M32 =  0; o.M33 = 1;
		}

		/// <summary>
		/// Creates a new scaling matrix with the axis scaling factors given by a vector.
		/// </summary>
		/// <param name="s">The scaling factors.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix CreateScale(in Vec3 s)
		{
			CreateScale(s.X, s.Y, s.Z, out Matrix o);
			return o;
		}

		/// <summary>
		/// Creates a new scaling matrix with the axis scaling factors given by a vector.
		/// </summary>
		/// <param name="s">The scaling factors.</param>
		/// <param name="o">The scaling matrix.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void CreateScale(in Vec3 s, out Matrix o)
		{
			CreateScale(s.X, s.Y, s.Z, out o);
		}
		#endregion // Scale

		#region Translation
		/// <summary>
		/// Creates a new matrix describing a translation to the given coordinates.
		/// </summary>
		/// <param name="x">The x-coordinate.</param>
		/// <param name="y">The y-coordinate.</param>
		/// <param name="z">The z-coordinate.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix CreateTranslation(float x, float y, float z)
		{
			CreateTranslation(x, y, z, out Matrix o);
			return o;
		}

		/// <summary>
		/// Creates a new matrix describing a translation to the given coordinates.
		/// </summary>
		/// <param name="x">The x-coordinate.</param>
		/// <param name="y">The y-coordinate.</param>
		/// <param name="z">The z-coordinate.</param>
		/// <param name="o">The translation matrix.</param>
		public static void CreateTranslation(float x, float y, float z, out Matrix o)
		{
			o.M00 = 1; o.M01 = 0; o.M02 = 0; o.M03 = 0;
			o.M10 = 0; o.M11 = 1; o.M12 = 0; o.M13 = 0;
			o.M20 = 0; o.M21 = 0; o.M22 = 1; o.M23 = 0;
			o.M30 = x; o.M31 = y; o.M32 = z; o.M33 = 1;
		}

		/// <summary>
		/// Creates a new matrix describing a translation to the given coordinates.
		/// </summary>
		/// <param name="pos">The translation coordinates.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix CreateTranslation(in Vec3 pos)
		{
			CreateTranslation(pos.X, pos.Y, pos.Z, out Matrix o);
			return o;
		}

		/// <summary>
		/// Creates a new matrix describing a translation to the given coordinates.
		/// </summary>
		/// <param name="pos">The translation coordinates.</param>
		/// <param name="o">The translation matrix.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void CreateTranslation(in Vec3 pos, out Matrix o)
		{
			CreateTranslation(pos.X, pos.Y, pos.Z, out o);
		}
		#endregion // Translation

		#region Other
		/// <summary>
		/// Creates a matrix describing the translation and rotations given by the vectors.
		/// </summary>
		/// <param name="pos">The world position of the matrix.</param>
		/// <param name="forward">The forward vector of the matrix - must be normalized.</param>
		/// <param name="up">The upward vector of the matrix - must be normalized.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix CreateWorld(in Vec3 pos, in Vec3 forward, in Vec3 up)
		{
			CreateWorld(pos, forward, up, out Matrix o);
			return o;
		}

		/// <summary>
		/// Creates a matrix describing the translation and rotations given by the vectors.
		/// </summary>
		/// <param name="pos">The world position of the matrix.</param>
		/// <param name="forward">The forward vector of the matrix - must be normalized.</param>
		/// <param name="up">The upward vector of the matrix - must be normalized.</param>
		/// <param name="o">The world matrix (combined translation and rotation matrices).</param>
		public static void CreateWorld(in Vec3 pos, in Vec3 forward, in Vec3 up, out Matrix o)
		{
			Vec3.Cross(forward, up, out var right);
			Vec3.Cross(right, forward, out var trueup);

			o.M00 =   right.X; o.M01 =   right.Y; o.M02 =   right.Z; o.M03 = 0;
			o.M10 =  trueup.X; o.M11 =  trueup.Y; o.M12 =  trueup.Z; o.M13 = 0;
			o.M20 = forward.X; o.M21 = forward.Y; o.M22 = forward.Z; o.M23 = 0;
			o.M30 =     pos.X; o.M31 =     pos.Y; o.M32 =     pos.Z; o.M33 = 1;
		}

		/// <summary>
		/// Creates a world matrix for an object to sperically billboard to point towards the camera.
		/// </summary>
		/// <param name="objPos">The position of the object.</param>
		/// <param name="camPos">The position of the camera.</param>
		/// <param name="camUp">The up vector of the camera - must be normalized.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix CreateBillboard(in Vec3 objPos, in Vec3 camPos, in Vec3 camUp)
		{
			CreateBillboard(objPos, camPos, camUp, out Matrix o);
			return o;
		}

		/// <summary>
		/// Creates a world matrix for an object to sperically billboard to point towards the camera.
		/// </summary>
		/// <param name="objPos">The position of the object.</param>
		/// <param name="camPos">The position of the camera.</param>
		/// <param name="camUp">The up vector of the camera - must be normalized.</param>
		/// <param name="o">The output matrix.</param>
		public static void CreateBillboard(in Vec3 objPos, in Vec3 camPos, in Vec3 camUp, out Matrix o)
		{
			Vec3 dir = (objPos - camPos).Normalized;
			Vec3.Cross(camUp, dir, out Vec3 right);
			Vec3.Cross(dir, right, out Vec3 trueup);

			o.M00 =  right.X; o.M01 =  right.Y; o.M02 =  right.Z; o.M03 = 0;
			o.M10 = trueup.X; o.M11 = trueup.Y; o.M12 = trueup.Z; o.M13 = 0;
			o.M20 =    dir.X; o.M21 =    dir.Y; o.M22 =    dir.Z; o.M23 = 0;
			o.M30 = objPos.X; o.M31 = objPos.Y; o.M32 = objPos.Z; o.M33 = 1;
		}

		// TODO: Shadow, Reflection
		#endregion // Other
		#endregion // World Matrices

		#region Camera Matrices
		/// <summary>
		/// Creates a view matrix that represents a camera in a certain location, looking at a target position.
		/// </summary>
		/// <param name="pos">The camera position.</param>
		/// <param name="targ">The camera target.</param>
		/// <param name="up">The "up" direction for the camera, used to control the roll.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix CreateLookAt(in Vec3 pos, in Vec3 targ, in Vec3 up)
		{
			CreateLookAt(pos, targ, up, out Matrix o);
			return o;
		}

		/// <summary>
		/// Creates a view matrix that represents a camera in a certain location, looking at a target position.
		/// </summary>
		/// <param name="pos">The camera position.</param>
		/// <param name="targ">The camera target.</param>
		/// <param name="up">The "up" direction for the camera, used to control the roll.</param>
		/// <param name="o">The output matrix.</param>
		public static void CreateLookAt(in Vec3 pos, in Vec3 targ, in Vec3 up, out Matrix o)
		{
			Vec3 forward = (pos - targ).Normalized;
			Vec3.Cross(forward, up, out Vec3 right);
			Vec3.Cross(forward, right, out Vec3 trueup);

			float d1 = -Vec3.Dot(right, pos);
			float d2 = -Vec3.Dot(trueup, pos);
			float d3 = -Vec3.Dot(forward, pos);

			o.M00 = right.X; o.M01 = trueup.X; o.M02 = forward.X; o.M03 = 0;
			o.M10 = right.Y; o.M11 = trueup.Y; o.M12 = forward.Y; o.M13 = 0;
			o.M20 = right.Z; o.M21 = trueup.Z; o.M22 = forward.Z; o.M23 = 0;
			o.M30 =      d1; o.M31 =       d2; o.M32 =        d3; o.M33 = 1;
		}

		/// <summary>
		/// Creates a matrix that represents a perspective projection (more distant objects are smaller).
		/// </summary>
		/// <param name="fov">The field of view of the projection.</param>
		/// <param name="aspect">The aspect ratio of the projections.</param>
		/// <param name="near">The distance to the near clipping plane.</param>
		/// <param name="far">The distance to the far clipping plane.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix CreatePerspective(float fov, float aspect, float near, float far)
		{
			CreatePerspective(fov, aspect, near, far, out Matrix o);
			return o;
		}

		/// <summary>
		/// Creates a matrix that represents a perspective projection (more distant objects are smaller).
		/// </summary>
		/// <param name="fov">The field of view of the projection.</param>
		/// <param name="aspect">The aspect ratio of the projections.</param>
		/// <param name="near">The distance to the near clipping plane.</param>
		/// <param name="far">The distance to the far clipping plane.</param>
		/// <param name="o">The output matrix.</param>
		public static void CreatePerspective(float fov, float aspect, float near, float far, out Matrix o)
		{
			float f = 1f / MathF.Atan(fov / 2);

			o.M00 = f / aspect; o.M01 =  0; o.M02 =                           0; o.M03 =  0;
			o.M10 =          0; o.M11 = -f; o.M12 =                           0; o.M13 =  0;
			o.M20 =          0; o.M21 =  0; o.M22 =          far / (near - far); o.M23 = -1;
			o.M30 =          0; o.M31 =  0; o.M32 = (near * far) / (near - far); o.M33 =  0;
		}

		/// <summary>
		/// Creates a matrix that represents an orthographic projection (all distances are the same size).
		/// </summary>
		/// <param name="width">The width of the projection.</param>
		/// <param name="height">The height of the projection.</param>
		/// <param name="near">The distance to the near clipping plane.</param>
		/// <param name="far">The distance to the far clipping plane.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix CreateOrthographic(float width, float height, float near, float far)
		{
			CreateOrthographic(width, height, near, far, out Matrix o);
			return o;
		}

		/// <summary>
		/// Creates a matrix that represents an orthographic projection (all distances are the same size).
		/// </summary>
		/// <param name="width">The width of the projection.</param>
		/// <param name="height">The height of the projection.</param>
		/// <param name="near">The distance to the near clipping plane.</param>
		/// <param name="far">The distance to the far clipping plane.</param>
		/// <param name="o">The output matrix.</param>
		public static void CreateOrthographic(float width, float height, float near, float far, out Matrix o)
		{
			float depth = near - far;

			o.M00 = 2 / width; o.M01 =          0; o.M02 =            0; o.M03 = 0;
			o.M10 =         0; o.M11 = 2 / height; o.M12 =            0; o.M13 = 0;
			o.M20 =         0; o.M21 =          0; o.M22 =    1 / depth; o.M23 = 0;
			o.M30 =        -1; o.M31 =         -1; o.M32 = near / depth; o.M33 = 1;
		}

		/// <summary>
		/// Creates a matrix that represents an orthographic projection (all distances are the same size). This
		/// function allows off-center projections to be made.
		/// </summary>
		/// <param name="left">The coordinate of the left view plane.</param>
		/// <param name="right">The coordinate of the right view plane.</param>
		/// <param name="bottom">The coordinate of the bottom view plane.</param>
		/// <param name="top">The coordinate of the top view plane.</param>
		/// <param name="near">The distance to the near clipping plane.</param>
		/// <param name="far">The distance to the far clipping plane.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix CreateOrthographicOffCenter(float left, float right, float bottom, float top, float near, float far)
		{
			CreateOrthographicOffCenter(left, right, bottom, top, near, far, out Matrix o);
			return o;
		}

		/// <summary>
		/// Creates a matrix that represents an orthographic projection (all distances are the same size). This
		/// function allows off-center projections to be made.
		/// </summary>
		/// <param name="left">The coordinate of the left view plane.</param>
		/// <param name="right">The coordinate of the right view plane.</param>
		/// <param name="bottom">The coordinate of the bottom view plane.</param>
		/// <param name="top">The coordinate of the top view plane.</param>
		/// <param name="near">The distance to the near clipping plane.</param>
		/// <param name="far">The distance to the far clipping plane.</param>
		/// <param name="o">The output matrix.</param>
		public static void CreateOrthographicOffCenter(float left, float right, float bottom, float top, float near, float far, out Matrix o)
		{
			float width = right - left, height = bottom - top, depth = near - far;

			o.M00 =               2 / width; o.M01 =                        0; o.M02 =            0; o.M03 = 0;
			o.M10 =                       0; o.M11 =               2 / height; o.M12 =            0; o.M13 = 0;
			o.M20 =                       0; o.M21 =                        0; o.M22 =    1 / depth; o.M23 = 0;
			o.M30 = -(right + left) / width; o.M31 = -(bottom + top) / height; o.M32 = near / depth; o.M33 = 1;
		}
		#endregion // Camera Matrices

		#region Operators
		public static bool operator == (in Matrix l, in Matrix r)
		{
			return (l.M00 == r.M00) && (l.M01 == r.M01) && (l.M02 == r.M02) && (l.M03 == r.M03) &&
				   (l.M10 == r.M10) && (l.M11 == r.M11) && (l.M12 == r.M12) && (l.M13 == r.M13) &&
				   (l.M20 == r.M20) && (l.M21 == r.M21) && (l.M22 == r.M22) && (l.M23 == r.M23) &&
				   (l.M30 == r.M30) && (l.M31 == r.M31) && (l.M32 == r.M32) && (l.M33 == r.M33);
		}

		public static bool operator != (in Matrix l, in Matrix r)
		{
			return (l.M00 != r.M00) || (l.M01 != r.M01) || (l.M02 != r.M02) || (l.M03 != r.M03) ||
				   (l.M10 != r.M10) || (l.M11 != r.M11) || (l.M12 != r.M12) || (l.M13 != r.M13) ||
				   (l.M20 != r.M20) || (l.M21 != r.M21) || (l.M22 != r.M22) || (l.M23 != r.M23) ||
				   (l.M30 != r.M30) || (l.M31 != r.M31) || (l.M32 != r.M32) || (l.M33 != r.M33);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix operator + (in Matrix l, in Matrix r)
		{
			Matrix.Add(l, r, out var o);
			return o;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix operator - (in Matrix l, in Matrix r)
		{
			Matrix.Subtract(l, r, out var o);
			return o;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix operator - (in Matrix m)
		{
			Matrix.Negate(m, out var o);
			return o;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix operator * (in Matrix l, float r)
		{
			Matrix.Multiply(l, r, out var o);
			return o;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix operator * (float l, in Matrix r)
		{
			Matrix.Multiply(r, l, out var o);
			return o;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix operator * (in Matrix l, in Matrix r)
		{
			Matrix.Multiply(l, r, out var o);
			return o;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Matrix operator / (in Matrix l, float r)
		{
			Matrix.Divide(l, r, out var o);
			return o;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 operator * (in Matrix l, in Vec3 r)
		{
			Matrix.Transform(l, r, out var o);
			return o;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 operator * (in Matrix l, in Vec4 r)
		{
			Matrix.Transform(l, r, out var o);
			return o;
		}
		#endregion // Operators
	}
}
