using System;
using System.Runtime.InteropServices;

namespace Spectrum
{
	/// <summary>
	/// A 4x4 matrix of numbers, stored in column-major order and accessed using row-major notation.
	/// </summary>
	/// <remarks>
	/// The row-major notation creates a matrix like:
	/// <code>
	///		M00 M01 M02 M03
	///		M10 M11 M12 M13
	///		M20 M21 M22 M23
	///		M30 M31 M32 M33
	/// </code>
	/// </remarks>
	[StructLayout(LayoutKind.Explicit, Size=(16*sizeof(float)))]
	public unsafe struct Matrix : IEquatable<Matrix>
	{
		/// <summary>
		/// The identity matrix.
		/// </summary>
		public static readonly Matrix Identity = new Matrix(
			1, 0, 0, 0,
			0, 1, 0, 0,
			0, 0, 1, 0,
			0, 0, 0, 1
		);
		/// <summary>
		/// A matrix with all zero entries.
		/// </summary>
		public static readonly Matrix Zero = new Matrix(
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
		);

		#region Fields
		/// <summary>
		/// Value at row 0, column 0.
		/// </summary>
		[FieldOffset(0)]
		public float M00;
		/// <summary>
		/// Value at row 1, column 0.
		/// </summary>
		[FieldOffset(1 * sizeof(float))]
		public float M10;
		/// <summary>
		/// Value at row 2, column 0.
		/// </summary>
		[FieldOffset(2 * sizeof(float))]
		public float M20;
		/// <summary>
		/// Value at row 3, column 0.
		/// </summary>
		[FieldOffset(3 * sizeof(float))]
		public float M30;
		/// <summary>
		/// Value at row 0, column 1.
		/// </summary>
		[FieldOffset(4 * sizeof(float))]
		public float M01;
		/// <summary>
		/// Value at row 1, column 1.
		/// </summary>
		[FieldOffset(5 * sizeof(float))]
		public float M11;
		/// <summary>
		/// Value at row 2, column 1.
		/// </summary>
		[FieldOffset(6 * sizeof(float))]
		public float M21;
		/// <summary>
		/// Value at row 3, column 1.
		/// </summary>
		[FieldOffset(7 * sizeof(float))]
		public float M31;
		/// <summary>
		/// Value at row 0, column 2.
		/// </summary>
		[FieldOffset(8 * sizeof(float))]
		public float M02;
		/// <summary>
		/// Value at row 1, column 2.
		/// </summary>
		[FieldOffset(9 * sizeof(float))]
		public float M12;
		/// <summary>
		/// Value at row 2, column 2.
		/// </summary>
		[FieldOffset(10 * sizeof(float))]
		public float M22;
		/// <summary>
		/// Value at row 3, column 2.
		/// </summary>
		[FieldOffset(11 * sizeof(float))]
		public float M32;
		/// <summary>
		/// Value at row 0, column 3.
		/// </summary>
		[FieldOffset(12 * sizeof(float))]
		public float M03;
		/// <summary>
		/// Value at row 1, column 3.
		/// </summary>
		[FieldOffset(13 * sizeof(float))]
		public float M13;
		/// <summary>
		/// Value at row 2, column 3.
		/// </summary>
		[FieldOffset(14 * sizeof(float))]
		public float M23;
		/// <summary>
		/// Value at row 3, column 3.
		/// </summary>
		[FieldOffset(15 * sizeof(float))]
		public float M33;

		// Fixed size buffer for fast access to the values
		[FieldOffset(0)]
		private fixed float _values[16];
		#endregion // Fields

		#region Indexers
		/// <summary>
		/// Gets/Sets a single member of the matrix in order of packing.
		/// </summary>
		/// <param name="i">The index of the member.</param>
		public float this[int i]
		{
			get
			{
				if (i < 0 || i > 15)
					throw new IndexOutOfRangeException($"Matrix indices must be in the range [0,15] ({i})");
				return _values[i];
			}
			set
			{
				if (i < 0 || i > 15)
					throw new IndexOutOfRangeException($"Matrix indices must be in the range [0,15] ({i})");
				_values[i] = value;
			}
		}

		/// <summary>
		/// Gets/Sets a single member of the matrix by its row/column position.
		/// </summary>
		/// <param name="r">The row of the member.</param>
		/// <param name="c">The column of the member.</param>
		public float this[int r, int c]
		{
			get
			{
				if (r < 0 || r > 3 || c < 0 || c > 3)
					throw new IndexOutOfRangeException($"Matrix indices must be in the range [0,3] ({r},{c})");
				int i = (c * 4) + r;
				return _values[i];
			}
			set
			{
				if (r < 0 || r > 3 || c < 0 || c > 3)
					throw new IndexOutOfRangeException($"Matrix indices must be in the range [0,3] ({r},{c})");
				int i = (c * 4) + r;
				_values[i] = value;
			}
		}
		#endregion // Indexers

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

		#region Overrides
		public bool Equals(Matrix m)
		{
			return (M00 == m.M00) && (M01 == m.M01) && (M02 == m.M02) && (M03 == m.M03) &&
				   (M10 == m.M10) && (M11 == m.M11) && (M12 == m.M12) && (M13 == m.M13) &&
				   (M20 == m.M20) && (M21 == m.M21) && (M22 == m.M22) && (M23 == m.M23) &&
				   (M30 == m.M30) && (M31 == m.M31) && (M32 == m.M32) && (M33 == m.M33);
		}

		public override bool Equals(object obj)
		{
			return (obj as Matrix?)?.Equals(this) ?? false;
		}

		// Not the best hash, we will look at this again later (but honestly you really shouldn't be using Matrix
		//   instances as keys... like *really really* don't do this).
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + M00.GetHashCode();
				hash = (hash * 23) + M11.GetHashCode();
				hash = (hash * 23) + M22.GetHashCode();
				hash = (hash * 23) + M33.GetHashCode();
				return hash;
			}
		}

		public override string ToString()
		{
			return $"{{{{{M00}, {M01}, {M02}, {M03}}}" +
					 $"{{{M10}, {M11}, {M12}, {M13}}}" +
					 $"{{{M20}, {M21}, {M22}, {M23}}}" +
					 $"{{{M30}, {M31}, {M32}, {M33}}}}}";
		}
		#endregion // Overrides

		#region Matrix Operations
		/// <summary>
		/// Calculates the transpose of the matrix.
		/// </summary>
		/// <param name="m">The matrix to transpose.</param>
		public static Matrix Transpose(in Matrix m)
		{
			Transpose(m, out Matrix o);
			return o;
		}

		/// <summary>
		/// Calculates the transpose of the matrix.
		/// </summary>
		/// <param name="m">The matrix to transpose.</param>
		/// <param name="o">The transposed matrix.</param>
		public static void Transpose(in Matrix m, out Matrix o)
		{
			o.M00 = m.M00; o.M01 = m.M10; o.M02 = m.M20; o.M03 = m.M30;
			o.M10 = m.M01; o.M11 = m.M11; o.M12 = m.M21; o.M13 = m.M31;
			o.M20 = m.M02; o.M21 = m.M12; o.M22 = m.M22; o.M23 = m.M32;
			o.M30 = m.M03; o.M31 = m.M13; o.M32 = m.M23; o.M33 = m.M33;
		}

		/// <summary>
		/// Calculates the inverse of the matrix.
		/// </summary>
		/// <param name="m">The matrix to invert, or <see cref="Matrix.Zero"/> if it cannot be inverted.</param>
		public static Matrix Invert(in Matrix m)
		{
			Invert(m, out Matrix o);
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
			if (MathUtils.NearlyEqual(det, 0, 1e-8))
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

		/// <summary>
		/// Calculates the determinant of the matrix.
		/// </summary>
		/// <param name="m">The matrix to find the determinant of.</param>
		public static float Determinant(in Matrix m)
		{
			// Hardcoded 4x4 determinant, found at https://stackoverflow.com/a/2937973
			return (m.M03 * m.M12 * m.M21 * m.M30) - (m.M02 * m.M13 * m.M21 * m.M30) -
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
				   (m.M01 * m.M10 * m.M22 * m.M33) + (m.M00 * m.M11 * m.M22 * m.M33);
		}

		/// <summary>
		/// Calculates the trace of the matrix (sum of diagonals).
		/// </summary>
		/// <param name="m">The matrix to find the trace of.</param>
		public static float Trace(in Matrix m) => m.M00 + m.M11 + m.M22 + m.M33;

		/// <summary>
		/// Adds two matricies together element-wise.
		/// </summary>
		/// <param name="l">The first matrix.</param>
		/// <param name="r">The second matrix.</param>
		public static Matrix Add(in Matrix l, in Matrix r)
		{
			Add(l, r, out Matrix o);
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
			o.M00 = l.M00 + r.M00; o.M01 = l.M01 + r.M01; o.M02 = l.M02 + r.M02; o.M03 = l.M03 + r.M03;
			o.M10 = l.M10 + r.M10; o.M11 = l.M11 + r.M11; o.M12 = l.M12 + r.M12; o.M13 = l.M13 + r.M13;
			o.M20 = l.M20 + r.M20; o.M21 = l.M21 + r.M21; o.M22 = l.M22 + r.M22; o.M23 = l.M23 + r.M23;
			o.M30 = l.M30 + r.M30; o.M31 = l.M31 + r.M31; o.M32 = l.M32 + r.M32; o.M33 = l.M33 + r.M33;
		}

		/// <summary>
		/// Subtracts one matrix from the other element-wise.
		/// </summary>
		/// <param name="l">The first matrix.</param>
		/// <param name="r">The second matrix.</param>
		public static Matrix Subtract(in Matrix l, in Matrix r)
		{
			Subtract(l, r, out Matrix o);
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
			o.M00 = l.M00 - r.M00; o.M01 = l.M01 - r.M01; o.M02 = l.M02 - r.M02; o.M03 = l.M03 - r.M03;
			o.M10 = l.M10 - r.M10; o.M11 = l.M11 - r.M11; o.M12 = l.M12 - r.M12; o.M13 = l.M13 - r.M13;
			o.M20 = l.M20 - r.M20; o.M21 = l.M21 - r.M21; o.M22 = l.M22 - r.M22; o.M23 = l.M23 - r.M23;
			o.M30 = l.M30 - r.M30; o.M31 = l.M31 - r.M31; o.M32 = l.M32 - r.M32; o.M33 = l.M33 - r.M33;
		}

		/// <summary>
		/// Performs the element-wise negation of the matrix.
		/// </summary>
		/// <param name="m">The matrix to negate.</param>
		public static Matrix Negate(in Matrix m)
		{
			Negate(m, out Matrix o);
			return o;
		}

		/// <summary>
		/// Performs the element-wise negation of the matrix.
		/// </summary>
		/// <param name="m">The matrix to negate.</param>
		/// <param name="o">The element-wise negated matrix.</param>
		public static void Negate(in Matrix m, out Matrix o)
		{
			o.M00 = -m.M00; o.M01 = -m.M01; o.M02 = -m.M02; o.M03 = -m.M03;
			o.M10 = -m.M10; o.M11 = -m.M11; o.M12 = -m.M12; o.M13 = -m.M13;
			o.M20 = -m.M20; o.M21 = -m.M21; o.M22 = -m.M22; o.M23 = -m.M23;
			o.M30 = -m.M30; o.M31 = -m.M31; o.M32 = -m.M32; o.M33 = -m.M33;
		}

		/// <summary>
		/// Performs an element-wise multiplication of a matrix by a scalar.
		/// </summary>
		/// <param name="l">The matrix.</param>
		/// <param name="r">The scalar.</param>
		public static Matrix Multiply(in Matrix l, float r)
		{
			Multiply(l, r, out Matrix o);
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
			o.M00 = l.M00 * r; o.M01 = l.M01 * r; o.M02 = l.M02 * r; o.M03 = l.M03 * r;
			o.M10 = l.M10 * r; o.M11 = l.M11 * r; o.M12 = l.M12 * r; o.M13 = l.M13 * r;
			o.M20 = l.M20 * r; o.M21 = l.M21 * r; o.M22 = l.M22 * r; o.M23 = l.M23 * r;
			o.M30 = l.M30 * r; o.M31 = l.M31 * r; o.M32 = l.M32 * r; o.M33 = l.M33 * r;
		}

		/// <summary>
		/// Multiplies two matrices using standard matrix multiplication rules.
		/// </summary>
		/// <param name="l">The first matrix.</param>
		/// <param name="r">The second matrix.</param>
		public static Matrix Multiply(in Matrix l, in Matrix r)
		{
			Multiply(l, r, out Matrix o);
			return o;
		}

		/// <summary>
		/// Multiplies two matrices using standard matrix multiplication rules.
		/// </summary>
		/// <param name="l">The first matrix.</param>
		/// <param name="r">The second matrix.</param>
		/// <param name="o">The multiplied matrices.</param>
		public static void Multiply(in Matrix l, in Matrix r, out Matrix o)
		{
			o.M00 = (l.M00 * r.M00) + (l.M01 * r.M10) + (l.M02 * r.M20) + (l.M03 * r.M30);
			o.M01 = (l.M00 * r.M01) + (l.M01 * r.M11) + (l.M02 * r.M21) + (l.M03 * r.M31);
			o.M02 = (l.M00 * r.M02) + (l.M01 * r.M12) + (l.M02 * r.M22) + (l.M03 * r.M32);
			o.M03 = (l.M00 * r.M03) + (l.M01 * r.M13) + (l.M02 * r.M23) + (l.M03 * r.M33);

			o.M10 = (l.M10 * r.M00) + (l.M11 * r.M10) + (l.M12 * r.M20) + (l.M13 * r.M30);
			o.M11 = (l.M10 * r.M01) + (l.M11 * r.M11) + (l.M12 * r.M21) + (l.M13 * r.M31);
			o.M12 = (l.M10 * r.M02) + (l.M11 * r.M12) + (l.M12 * r.M22) + (l.M13 * r.M32);
			o.M13 = (l.M10 * r.M03) + (l.M11 * r.M13) + (l.M12 * r.M23) + (l.M13 * r.M33);

			o.M20 = (l.M20 * r.M00) + (l.M21 * r.M10) + (l.M22 * r.M20) + (l.M23 * r.M30);
			o.M21 = (l.M20 * r.M01) + (l.M21 * r.M11) + (l.M22 * r.M21) + (l.M23 * r.M31);
			o.M22 = (l.M20 * r.M02) + (l.M21 * r.M12) + (l.M22 * r.M22) + (l.M23 * r.M32);
			o.M23 = (l.M20 * r.M03) + (l.M21 * r.M13) + (l.M22 * r.M23) + (l.M23 * r.M33);

			o.M30 = (l.M30 * r.M00) + (l.M31 * r.M10) + (l.M32 * r.M20) + (l.M33 * r.M30);
			o.M31 = (l.M30 * r.M01) + (l.M31 * r.M11) + (l.M32 * r.M21) + (l.M33 * r.M31);
			o.M32 = (l.M30 * r.M02) + (l.M31 * r.M12) + (l.M32 * r.M22) + (l.M33 * r.M32);
			o.M33 = (l.M30 * r.M03) + (l.M31 * r.M13) + (l.M32 * r.M23) + (l.M33 * r.M33);
		}

		/// <summary>
		/// Transforms a vector by multiplying it by a matrix.
		/// </summary>
		/// <param name="l">The matrix.</param>
		/// <param name="r">The vector to transform.</param>
		public static Vec3 Transform(in Matrix l, in Vec3 r)
		{
			Transform(l, r, out Vec3 o);
			return o;
		}

		/// <summary>
		/// Transforms a vector by multiplying it by a matrix.
		/// </summary>
		/// <param name="l">The matrix.</param>
		/// <param name="r">The vector to transform.</param>
		/// <param name="o">The transformed vector.</param>
		public static void Transform(in Matrix l, in Vec3 r, out Vec3 o)
		{
			o.X = (l.M00 * r.X) + (l.M01 * r.Y) + (l.M02 * r.Z) + l.M03;
			o.Y = (l.M10 * r.X) + (l.M11 * r.Y) + (l.M12 * r.Z) + l.M13;
			o.Z = (l.M20 * r.X) + (l.M21 * r.Y) + (l.M22 * r.Z) + l.M23;
		}

		/// <summary>
		/// Transforms a vector by multiplying it by a matrix.
		/// </summary>
		/// <param name="l">The matrix.</param>
		/// <param name="r">The vector to transform.</param>
		public static Vec4 Transform(in Matrix l, in Vec4 r)
		{
			Transform(l, r, out Vec4 o);
			return o;
		}

		/// <summary>
		/// Transforms a vector by multiplying it by a matrix.
		/// </summary>
		/// <param name="l">The matrix.</param>
		/// <param name="r">The vector to transform.</param>
		/// <param name="o">The transformed vector.</param>
		public static void Transform(in Matrix l, in Vec4 r, out Vec4 o)
		{
			o.X = (l.M00 * r.X) + (l.M01 * r.Y) + (l.M02 * r.Z) + (l.M03 * r.W);
			o.Y = (l.M10 * r.X) + (l.M11 * r.Y) + (l.M12 * r.Z) + (l.M13 * r.W);
			o.Z = (l.M20 * r.X) + (l.M21 * r.Y) + (l.M22 * r.Z) + (l.M23 * r.W);
			o.W = (l.M30 * r.X) + (l.M31 * r.Y) + (l.M32 * r.Z) + (l.M33 * r.W);
		}

		/// <summary>
		/// Checks if the matrices are approximately equal to each other.
		/// </summary>
		/// <param name="l">First matrix.</param>
		/// <param name="r">Second matrix.</param>
		/// <param name="eps">The maximum difference for any element to be considered nearly equal.</param>
		public static bool NearlyEqual(in Matrix l, in Matrix r, float eps = 1e-5f)
		{
			return MathUtils.NearlyEqual(l.M00, r.M00, eps) && MathUtils.NearlyEqual(l.M01, r.M01, eps) && MathUtils.NearlyEqual(l.M02, r.M02, eps) && MathUtils.NearlyEqual(l.M03, r.M03, eps) &&
				   MathUtils.NearlyEqual(l.M10, r.M10, eps) && MathUtils.NearlyEqual(l.M11, r.M11, eps) && MathUtils.NearlyEqual(l.M12, r.M12, eps) && MathUtils.NearlyEqual(l.M13, r.M13, eps) &&
				   MathUtils.NearlyEqual(l.M20, r.M20, eps) && MathUtils.NearlyEqual(l.M21, r.M21, eps) && MathUtils.NearlyEqual(l.M22, r.M22, eps) && MathUtils.NearlyEqual(l.M23, r.M23, eps) &&
				   MathUtils.NearlyEqual(l.M30, r.M30, eps) && MathUtils.NearlyEqual(l.M31, r.M31, eps) && MathUtils.NearlyEqual(l.M32, r.M32, eps) && MathUtils.NearlyEqual(l.M33, r.M33, eps);
		}

		/// <summary>
		/// Checks if the passed matrix is within a certain limit of the <see cref="Identity"/> matrix.
		/// </summary>
		/// <param name="m">The matrix to check.</param>
		/// <param name="eps">The maximum different for any element to be considered nearly equal.</param>
		public static bool NearlyIdentity(in Matrix m, float eps = 1e-5f)
		{
			return MathUtils.NearlyEqual(m.M00, 1, eps) && MathUtils.NearlyEqual(m.M01, 0, eps) && MathUtils.NearlyEqual(m.M02, 0, eps) && MathUtils.NearlyEqual(m.M03, 0, eps) &&
				   MathUtils.NearlyEqual(m.M10, 0, eps) && MathUtils.NearlyEqual(m.M11, 1, eps) && MathUtils.NearlyEqual(m.M12, 0, eps) && MathUtils.NearlyEqual(m.M13, 0, eps) &&
				   MathUtils.NearlyEqual(m.M20, 0, eps) && MathUtils.NearlyEqual(m.M21, 0, eps) && MathUtils.NearlyEqual(m.M22, 1, eps) && MathUtils.NearlyEqual(m.M23, 0, eps) &&
				   MathUtils.NearlyEqual(m.M30, 0, eps) && MathUtils.NearlyEqual(m.M31, 0, eps) && MathUtils.NearlyEqual(m.M32, 0, eps) && MathUtils.NearlyEqual(m.M33, 1, eps);
		}
		#endregion // Matrix Operations

		#region World Matrices
		#region Rotation
		/// <summary>
		/// Creates a matrix describing a rotation around the x-axis.
		/// </summary>
		/// <param name="angle">The angle of rotation, in radians.</param>
		public static Matrix CreateRotationX(float angle)
		{
			CreateRotationX(angle, out Matrix o);
			return o;
		}

		/// <summary>
		/// Creates a matrix describing a rotation around the x-axis.
		/// </summary>
		/// <param name="angle">The angle of rotation, in radians.</param>
		/// <param name="o">The rotation matrix.</param>
		public static void CreateRotationX(float angle, out Matrix o)
		{
			float c = (float)Math.Cos(angle);
			float s = (float)Math.Sin(angle);

			o.M00 = 1; o.M01 = 0; o.M02 =  0; o.M03 = 0;
			o.M10 = 0; o.M11 = c; o.M12 = -s; o.M13 = 0;
			o.M20 = 0; o.M21 = s; o.M22 =  c; o.M23 = 0;
			o.M30 = 0; o.M31 = 0; o.M32 =  0; o.M33 = 1;
		}

		/// <summary>
		/// Creates a matrix describing a rotation around the y-axis.
		/// </summary>
		/// <param name="angle">The angle of rotation, in radians.</param>
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
			float c = (float)Math.Cos(angle);
			float s = (float)Math.Sin(angle);

			o.M00 =  c; o.M01 = 0; o.M02 = s; o.M03 = 0;
			o.M10 =  0; o.M11 = 1; o.M12 = 0; o.M13 = 0;
			o.M20 = -s; o.M21 = 0; o.M22 = c; o.M23 = 0;
			o.M30 =  0; o.M31 = 0; o.M32 = 0; o.M33 = 1;
		}

		/// <summary>
		/// Creates a matrix describing a rotation around the z-axis.
		/// </summary>
		/// <param name="angle">The angle of rotation, in radians.</param>
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
			float c = (float)Math.Cos(angle);
			float s = (float)Math.Sin(angle);

			o.M00 = c; o.M01 = -s; o.M02 = 0; o.M03 = 0;
			o.M10 = s; o.M11 =  c; o.M12 = 0; o.M13 = 0;
			o.M20 = 0; o.M21 =  0; o.M22 = 1; o.M23 = 0;
			o.M30 = 0; o.M31 =  0; o.M32 = 0; o.M33 = 1;
		}



		#endregion // Rotation

		#region Scaling
		/// <summary>
		/// Creates a new scaling matrix with all three dimensions scaled identically.
		/// </summary>
		/// <param name="s">The scale factor.</param>
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
		public static void CreateScale(in Vec3 s, out Matrix o)
		{
			CreateScale(s.X, s.Y, s.Z, out o);
		}
		#endregion // Scaling

		#region Translation
		/// <summary>
		/// Creates a new matrix describing a translation to the given coordinates.
		/// </summary>
		/// <param name="x">The x-coordinate.</param>
		/// <param name="y">The y-coordinate.</param>
		/// <param name="z">The z-coordinate.</param>
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
			o.M00 = 1; o.M01 = 0; o.M02 = 0; o.M03 = x;
			o.M10 = 0; o.M11 = 1; o.M12 = 0; o.M13 = y;
			o.M20 = 0; o.M21 = 0; o.M22 = 1; o.M23 = z;
			o.M30 = 0; o.M31 = 0; o.M32 = 0; o.M33 = 1;
		}

		/// <summary>
		/// Creates a new matrix describing a translation to the given coordinates.
		/// </summary>
		/// <param name="pos">The translation coordinates.</param>
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
		public static void CreateTranslation(in Vec3 pos, out Matrix o)
		{
			CreateTranslation(pos.X, pos.Y, pos.Z, out o);
		}
		#endregion // Translation

		/// <summary>
		/// Creates a matrix describing the translation and rotations given by the vectors.
		/// </summary>
		/// <param name="pos">The world position of the matrix.</param>
		/// <param name="forward">The forward vector of the matrix.</param>
		/// <param name="up">The upward vector of the matrix.</param>
		public static Matrix CreateWorld(in Vec3 pos, in Vec3 forward, in Vec3 up)
		{
			CreateWorld(pos, forward, up, out Matrix o);
			return o;
		}

		/// <summary>
		/// Creates a matrix describing the translation and rotations given by the vectors.
		/// </summary>
		/// <param name="pos">The world position of the matrix.</param>
		/// <param name="forward">The forward vector of the matrix.</param>
		/// <param name="up">The upward vector of the matrix.</param>
		/// <param name="o">The world matrix (combined translation and rotation matrices).</param>
		public static void CreateWorld(in Vec3 pos, in Vec3 forward, in Vec3 up, out Matrix o)
		{
			Vec3.Normalized(forward, out Vec3 truef);
			Vec3.Cross(forward, up, out Vec3 right);
			Vec3.Normalized(right, out right);
			Vec3.Cross(right, forward, out Vec3 trueup);
			Vec3.Normalized(trueup, out trueup);

			o.M00 =  right.X; o.M01 =  right.Y; o.M02 =  right.Z; o.M03 = pos.X;
			o.M10 = trueup.X; o.M11 = trueup.Y; o.M12 = trueup.Z; o.M13 = pos.Y;
			o.M20 =  truef.X; o.M21 =  truef.Y; o.M22 =  truef.Z; o.M23 = pos.Z;
			o.M30 =        0; o.M31 =        0; o.M32 =        0; o.M33 =     1;
		}
		#endregion // World Matrices

		#region Camera Matrices
		/// <summary>
		/// Creates a view matrix that represents a camera in a certain location, looking at a target position.
		/// </summary>
		/// <param name="pos">The camera position.</param>
		/// <param name="targ">The camera target.</param>
		/// <param name="up">The "up" direction for the camera, used to control the roll.</param>
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
			Vec3.Normalized((pos - targ), out Vec3 forward);
			Vec3.Cross(forward, up, out Vec3 right);
			Vec3.Cross(forward, right, out Vec3 trueup);

			o.M00 =   right.X; o.M01 =   right.Y; o.M02 =   right.Z; o.M03 =   -Vec3.Dot(right, pos);
			o.M10 =  trueup.X; o.M11 =  trueup.Y; o.M12 =  trueup.Z; o.M13 =  -Vec3.Dot(trueup, pos);
			o.M20 = forward.X; o.M21 = forward.Y; o.M22 = forward.Z; o.M23 = -Vec3.Dot(forward, pos);
			o.M30 =         0; o.M31 =         0; o.M32 =         0; o.M33 =                       1;
		}

		/// <summary>
		/// Creates a matrix that represents a perspective projection (more distant objects are smaller).
		/// </summary>
		/// <param name="fov">The field of view of the projection.</param>
		/// <param name="aspect">The aspect ratio of the projections.</param>
		/// <param name="near">The distance to the near clipping plane.</param>
		/// <param name="far">The distance to the far clipping plane.</param>
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
			float f = 1f / Mathf.Atan(fov / 2);

			o.M00 = f / aspect; o.M01 =  0; o.M02 =                  0; o.M03 =                           0;
			o.M10 =          0; o.M11 = -f; o.M12 =                  0; o.M13 =                           0;
			o.M20 =          0; o.M21 =  0; o.M22 = far / (near - far); o.M23 = (near * far) / (near - far);
			o.M30 =          0; o.M31 =  0; o.M32 =                 -1; o.M33 =                           0;
		}

		/// <summary>
		/// Creates a matrix that represents an orthographic projection (all distances are the same size).
		/// </summary>
		/// <param name="width">The width of the projection.</param>
		/// <param name="height">The height of the projection.</param>
		/// <param name="near">The distance to the near clipping plane.</param>
		/// <param name="far">The distance to the far clipping plane.</param>
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

			o.M00 = 2 / width; o.M01 =          0; o.M02 =         0; o.M03 =           -1;
			o.M10 =         0; o.M11 = 2 / height; o.M12 =         0; o.M13 =           -1;
			o.M20 =         0; o.M21 =          0; o.M22 = 1 / depth; o.M23 = near / depth;
			o.M30 =         0; o.M31 =          0; o.M32 =         0; o.M33 =            1;
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

			o.M00 = 2 / width; o.M01 =          0; o.M02 =         0; o.M03 =  -(right + left) / width;
			o.M10 =         0; o.M11 = 2 / height; o.M12 =         0; o.M13 = -(bottom + top) / height;
			o.M20 =         0; o.M21 =          0; o.M22 = 1 / depth; o.M23 =             near / depth;
			o.M30 =         0; o.M31 =          0; o.M32 =         0; o.M33 =                        1;
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

		public static Matrix operator + (in Matrix l, in Matrix r)
		{
			Add(l, r, out Matrix o);
			return o;
		}

		public static Matrix operator - (in Matrix l, in Matrix r)
		{
			Subtract(l, r, out Matrix o);
			return o;
		}

		public static Matrix operator - (in Matrix m)
		{
			Negate(m, out Matrix o);
			return o;
		}

		public static Matrix operator * (in Matrix l, float r)
		{
			Multiply(l, r, out Matrix o);
			return o;
		}

		public static Matrix operator * (float l, in Matrix r)
		{
			Multiply(r, l, out Matrix o);
			return o;
		}

		public static Matrix operator * (in Matrix l, in Matrix r)
		{
			Multiply(l, r, out Matrix o);
			return o;
		}

		public static Vec3 operator * (in Matrix l, in Vec3 r)
		{
			Transform(l, r, out Vec3 o);
			return o;
		}

		public static Vec4 operator * (in Matrix l, in Vec4 r)
		{
			Transform(l, r, out Vec4 o);
			return o;
		}
		#endregion // Operators
	}
}
