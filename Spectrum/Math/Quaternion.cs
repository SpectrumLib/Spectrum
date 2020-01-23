/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Spectrum
{
	/// <summary>
	/// Efficient mathematical object for representing rotations in 3D space.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size=4*sizeof(float))]
	public struct Quaternion : IEquatable<Quaternion>
	{
		#region Constant Quaternions
		/// <summary>
		/// The identity quaternion representing no rotations.
		/// </summary>
		public static readonly Quaternion Identity = new Quaternion(0, 0, 0, 1);
		#endregion // Constant Quaternions

		#region Fields
		/// <summary>
		/// The x-component of the rotation.
		/// </summary>
		[FieldOffset(0)]
		public float X;
		/// <summary>
		/// The y-component of the rotation.
		/// </summary>
		[FieldOffset(sizeof(float))]
		public float Y;
		/// <summary>
		/// The z-component of the rotation.
		/// </summary>
		[FieldOffset(2*sizeof(float))]
		public float Z;
		/// <summary>
		/// The magnitude of the rotation.
		/// </summary>
		[FieldOffset(3*sizeof(float))]
		public float W;

		/// <summary>
		/// Gets the magnitude of the quaternion components.
		/// </summary>
		public readonly float Length => MathF.Sqrt(X * X + Y * Y + Z * Z + W * W);
		/// <summary>
		/// Gets the squared magnitude of the quaternion components.
		/// </summary>
		public readonly float LengthSquared => X * X + Y * Y + Z * Z + W * W;
		/// <summary>
		/// Gets the normalized version of this quaternion.
		/// </summary>
		public readonly Quaternion Normalized => this * (1f / Length);
		#endregion // Fields

		#region Ctor
		/// <summary>
		/// Creates a quaternion from the component values and rotation magnitude.
		/// </summary>
		/// <param name="x">The x-component.</param>
		/// <param name="y">The y-component.</param>
		/// <param name="z">The z-component.</param>
		/// <param name="w">The rotation component.</param>
		public Quaternion(float x, float y, float z, float w)
		{
			X = x;
			Y = y;
			Z = z;
			W = w;
		}

		/// <summary>
		/// Creates a quaterion from a vector of component values and a rotation magnitude.
		/// </summary>
		/// <param name="value">The axis components.</param>
		/// <param name="w">The rotation component.</param>
		public Quaternion(in Vec3 value, float w)
		{
			X = value.X;
			Y = value.Y;
			Z = value.Z;
			W = w;
		}

		/// <summary>
		/// Creates a quaternion from 4-vector values.
		/// </summary>
		/// <param name="value">The axis and rotation components.</param>
		public Quaternion(in Vec4 value)
		{
			X = value.X;
			Y = value.Y;
			Z = value.Z;
			W = value.W;
		}
		#endregion // Ctor

		#region Overrides
		readonly bool IEquatable<Quaternion>.Equals(Quaternion q) => q == this;

		public readonly override bool Equals(object obj) => (obj is Quaternion) && ((Quaternion)obj == this);

		public readonly override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + X.GetHashCode();
				hash = (hash * 23) + Y.GetHashCode();
				hash = (hash * 23) + Z.GetHashCode();
				hash = (hash * 23) + W.GetHashCode();
				return hash;
			}
		}

		public readonly override string ToString() => $"{{{X} {Y} {Z} {W}}}";
		#endregion // Overrides

		#region Basic Math Operations
		/// <summary>
		/// Adds the quaternions component-wise.
		/// </summary>
		/// <param name="l">The first quaternion to add.</param>
		/// <param name="r">The second quaternion to add.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Quaternion Add(in Quaternion l, in Quaternion r) => new Quaternion(l.X + r.X, l.Y + r.Y, l.Z + r.Z, l.W + r.W);

		/// <summary>
		/// Adds the quaternions component-wise.
		/// </summary>
		/// <param name="l">The first quaternion to add.</param>
		/// <param name="r">The second quaternion to add.</param>
		/// <param name="o">The output quaternion.</param>
		public static void Add(in Quaternion l, in Quaternion r, out Quaternion o)
		{
			o.X = l.X + r.X;
			o.Y = l.Y + r.Y;
			o.Z = l.Z + r.Z;
			o.W = l.W + r.W;
		}

		/// <summary>
		/// Subtracts the quaternions component-wise.
		/// </summary>
		/// <param name="l">The first quaternion to subtract.</param>
		/// <param name="r">The second quaternion to subtract.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Quaternion Subtract(in Quaternion l, in Quaternion r) => new Quaternion(l.X - r.X, l.Y - r.Y, l.Z - r.Z, l.W - r.W);

		/// <summary>
		/// Subtracts the quaternions component-wise.
		/// </summary>
		/// <param name="l">The first quaternion to subtract.</param>
		/// <param name="r">The second quaternion to subtract.</param>
		/// <param name="o">The output quaternion.</param>
		public static void Subtract(in Quaternion l, in Quaternion r, out Quaternion o)
		{
			o.X = l.X - r.X;
			o.Y = l.Y - r.Y;
			o.Z = l.Z - r.Z;
			o.W = l.W - r.W;
		}

		/// <summary>
		/// Multiplies two quaternion values using standard quaternion multiplication. Equivalent to rotating first by
		/// <paramref name="l"/>, and then by <paramref name="r"/>.
		/// </summary>
		/// <param name="l">The first quaternion.</param>
		/// <param name="r">The second quaternion.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Quaternion Multiply(in Quaternion l, in Quaternion r)
		{
			Multiply(l, r, out Quaternion o);
			return o;
		}

		/// <summary>
		/// Multiplies two quaternion values using standard quaternion multiplication. Equivalent to rotating first by
		/// <paramref name="l"/>, and then by <paramref name="r"/>.
		/// </summary>
		/// <param name="l">The first quaternion.</param>
		/// <param name="r">The second quaternion.</param>
		/// <param name="o">The output quaternion.</param>
		public static void Multiply(in Quaternion l, in Quaternion r, out Quaternion o)
		{
			float n1 = (l.Y * r.Z) - (l.Z * r.Y);
			float n2 = (l.Z * r.X) - (l.X * r.Z);
			float n3 = (l.X * r.Y) - (l.Y * r.X);
			float n4 = (l.X * r.X) + (l.Y * r.Y) + (l.Z * r.Z);
			o.X = (l.X * r.W) + (l.W * r.X) + n1;
			o.Y = (l.Y * r.W) + (l.W * r.Y) + n2;
			o.Z = (l.Z * r.W) + (l.W * r.Z) + n3;
			o.W = (l.W * r.W) - n4;
		}

		/// <summary>
		/// Performs scalar multiplication of the quaternion.
		/// </summary>
		/// <param name="l">The quaternion to multiply.</param>
		/// <param name="r">The scalar to multiply.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Quaternion Multiply(in Quaternion l, float r) => new Quaternion(l.X * r, l.Y * r, l.Z * r, l.W * r);

		/// <summary>
		/// Performs scalar multiplication of the quaternion.
		/// </summary>
		/// <param name="l">The quaternion to multiply.</param>
		/// <param name="r">The scalar to multiply.</param>
		/// <param name="o">The output quaternion.</param>
		public static void Multiply(in Quaternion l, float r, out Quaternion o)
		{
			o.X = l.X * r;
			o.Y = l.Y * r;
			o.Z = l.Z * r;
			o.W = l.W * r;
		}

		/// <summary>
		/// Divides two quaternions using standard quaternion division.
		/// </summary>
		/// <param name="l">The numerator quaternion.</param>
		/// <param name="r">The denominator quaternion.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Quaternion Divide(in Quaternion l, in Quaternion r)
		{
			Divide(l, r, out Quaternion o);
			return o;
		}

		/// <summary>
		/// Divides two quaternions using standard quaternion division.
		/// </summary>
		/// <param name="l">The numerator quaternion.</param>
		/// <param name="r">The denominator quaternion.</param>
		/// <param name="o">The output quaternion.</param>
		public static void Divide(in Quaternion l, in Quaternion r, out Quaternion o)
		{
			float a = 1 / ((r.X * r.X) + (r.Y * r.Y) + (r.Z * r.Z) + (r.W * r.W));
			float nw = (r.W * l.W) + (r.X * l.X) + (r.Y * l.Y) + (r.Z * l.Z);
			float nx = (r.W * l.X) - (r.X * l.W) - (r.Y * l.Z) + (r.Z * l.Y);
			float ny = (r.W * l.Y) + (r.X * l.Z) - (r.Y * l.W) - (r.Z * l.X);
			float nz = (r.W * l.Z) - (r.X * l.Y) + (r.Y * l.X) - (r.Z * l.W);
			o.X = nx * a; o.Y = ny * a; o.Z = nz * a; o.W = nw * a;
		}

		/// <summary>
		/// Performs scalar division of the quaternion.
		/// </summary>
		/// <param name="l">The quaternion numerator.</param>
		/// <param name="r">The scalar denominator.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Quaternion Divide(in Quaternion l, float r) => new Quaternion(l.X / r, l.Y / r, l.Z / r, l.W / r);

		/// <summary>
		/// Performs scalar division of the quaternion.
		/// </summary>
		/// <param name="l">The quaternion numerator.</param>
		/// <param name="r">The scalar denominator.</param>
		/// <param name="o">The output quaternion.</param>
		public static void Divide(in Quaternion l, float r, out Quaternion o)
		{
			o.X = l.X / r;
			o.Y = l.Y / r;
			o.Z = l.Z / r;
			o.W = l.W / r;
		}

		/// <summary>
		/// Negates the quaternion components.
		/// </summary>
		/// <param name="q">The quaternion to negate.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Quaternion Negate(in Quaternion q) => new Quaternion(-q.X, -q.Y, -q.Z, -q.W);

		/// <summary>
		/// Negates the quaternion components.
		/// </summary>
		/// <param name="q">The quaternion to negate.</param>
		/// <param name="o">The output quaternion.</param>
		public static void Negate(in Quaternion q, out Quaternion o)
		{
			o.X = -q.X;
			o.Y = -q.Y;
			o.Z = -q.Z;
			o.W = -q.W;
		}
		#endregion // Basic Math Operations

		#region Quaternion Operations
		/// <summary>
		/// Calculates the conjugate to the passed quaternion.
		/// </summary>
		/// <param name="q">The quaternion to conjugate.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Quaternion Conjugate(in Quaternion q) => new Quaternion(-q.X, -q.Y, -q.Z, q.W);

		/// <summary>
		/// Calculates the conjugate to the passed quaternion.
		/// </summary>
		/// <param name="q">The quaternion to conjugate.</param>
		/// <param name="o">The output quaternion.</param>
		public static void Conjugate(in Quaternion q, out Quaternion o)
		{
			o.X = -q.X;
			o.Y = -q.Y;
			o.Z = -q.Z;
			o.W = q.W;
		}

		/// <summary>
		/// Calculates the dot product of the two quaternions.
		/// </summary>
		/// <param name="l">The first quaternion.</param>
		/// <param name="r">The second quaternion.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Dot(in Quaternion l, in Quaternion r) => (l.X * r.X) + (l.Y * r.Y) + (l.Z * r.Z) + (l.W * r.W);

		/// <summary>
		/// Caluclates the inverse quaterion, which represents the oppposite rotation.
		/// </summary>
		/// <param name="q">The quaternion to invert.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Quaternion Invert(in Quaternion q)
		{
			Invert(q, out var o);
			return o;
		}

		/// <summary>
		/// Caluclates the inverse quaterion, which represents the oppposite rotation.
		/// </summary>
		/// <param name="q">The quaternion to invert.</param>
		/// <param name="o">The output quaternion.</param>
		public static void Invert(in Quaternion q, out Quaternion o)
		{
			float a = 1 / ((q.X * q.X) + (q.Y * q.Y) + (q.Z * q.Z) + (q.W * q.W));
			o.X = -q.X * a;
			o.Y = -q.Y * a;
			o.Z = -q.Z * a;
			o.W = q.W * a;
		}

		/// <summary>
		/// Normalizes the quaternion so the component magnitudes are one.
		/// </summary>
		/// <param name="q">The quaternion to normalize.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Quaternion Normalize(in Quaternion q) => q.Normalized;

		/// <summary>
		/// Normalizes the quaternion so the component magnitudes are one.
		/// </summary>
		/// <param name="q">The quaternion to normalize.</param>
		/// <param name="o">The output quaternion.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Normalize(in Quaternion q, out Quaternion o) => o = q.Normalized;
		#endregion // Quaternion Operations

		#region Creation
		/// <summary>
		/// Creates a quaternion representing rotations in yaw, pitch, and roll.
		/// </summary>
		/// <param name="yaw">The yaw (left/right rotation around y-axis), in radians.</param>
		/// <param name="pitch">The pitch (up/down rotation around x-axis), in radians.</param>
		/// <param name="roll">The roll (rotation around z-axis), in radians.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Quaternion CreateYawPitchRoll(float yaw, float pitch, float roll)
		{
			CreateYawPitchRoll(yaw, pitch, roll, out Quaternion o);
			return o;
		}

		/// <summary>
		/// Creates a quaternion representing rotations in yaw, pitch, and roll.
		/// </summary>
		/// <param name="yaw">The yaw (left/right rotation around y-axis), in radians.</param>
		/// <param name="pitch">The pitch (up/down rotation around x-axis), in radians.</param>
		/// <param name="roll">The roll (rotation around z-axis), in radians.</param>
		/// <param name="o">The output quaternion.</param>
		public static void CreateYawPitchRoll(float yaw, float pitch, float roll, out Quaternion o)
		{
			float hy = yaw / 2, hp = pitch / 2, hr = roll / 2;

			float sy = MathF.Sin(hy), cy = MathF.Cos(hy);
			float sp = MathF.Sin(hp), cp = MathF.Cos(hp);
			float sr = MathF.Sin(hr), cr = MathF.Cos(hr);

			o.X = (cy * sp * cr) + (sy * cp * sr);
			o.Y = (sy * cp * cr) - (cy * sp * sr);
			o.Z = (cy * cp * sr) - (sy * sp * cr);
			o.W = (cy * cp * cr) + (sy * sp * sr);
		}

		/// <summary>
		/// Creates a quaternion representing a rotation around an axis.
		/// </summary>
		/// <param name="axis">The axis of rotation.</param>
		/// <param name="a">The rotation in radians.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Quaternion CreateAxisRotation(in Vec3 axis, float a)
		{
			CreateAxisRotation(axis, a, out Quaternion o);
			return o;
		}

		/// <summary>
		/// Creates a quaternion representing a rotation around an axis.
		/// </summary>
		/// <param name="axis">The axis of rotation.</param>
		/// <param name="a">The rotation in radians.</param>
		/// <param name="o">The output quaternion.</param>
		public static void CreateAxisRotation(in Vec3 axis, float a, out Quaternion o)
		{
			float ha = a / 2;
			float s = MathF.Sin(ha), c = MathF.Cos(ha);
			o.X = axis.X * s;
			o.Y = axis.Y * s;
			o.Z = axis.Z * s;
			o.W = c;
		}

		/// <summary>
		/// Creates a quaternion from the rotation components of the matrix.
		/// </summary>
		/// <param name="m">The matrix extract the rotation components from.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Quaternion CreateFromRotationMatrix(in Matrix m)
		{
			CreateFromRotationMatrix(m, out Quaternion o);
			return o;
		}

		/// <summary>
		/// Creates a quaternion from the rotation components of the matrix.
		/// </summary>
		/// <param name="m">The matrix extract the rotation components from.</param>
		/// <param name="o">The output quaternion.</param>
		public static void CreateFromRotationMatrix(in Matrix m, out Quaternion o)
		{
			float sqrt, half;
			float scale = m.M00 + m.M11 + m.M22;

			if (scale > 0)
			{
				sqrt = MathF.Sqrt(scale + 1);
				half = 0.5f / sqrt;
				o.X = (m.M12 - m.M21) * half;
				o.Y = (m.M20 - m.M02) * half;
				o.Z = (m.M01 - m.M10) * half;
				o.W = sqrt / 2;
			}
			else if ((m.M00 >= m.M11) && (m.M00 >= m.M22))
			{
				sqrt = MathF.Sqrt(1 + m.M00 - m.M11 - m.M22);
				half = 0.5f / sqrt;
				o.X = sqrt / 2;
				o.Y = (m.M01 + m.M10) * half;
				o.Z = (m.M02 + m.M20) * half;
				o.W = (m.M12 - m.M21) * half;
			}
			else if (m.M11 > m.M22)
			{
				sqrt = MathF.Sqrt(1 + m.M11 - m.M00 - m.M22);
				half = 0.5f / sqrt;
				o.X = (m.M01 + m.M10) * half;
				o.Y = sqrt / 2;
				o.Z = (m.M12 + m.M21) * half;
				o.W = (m.M20 - m.M02) * half;
			}
			else
			{
				sqrt = MathF.Sqrt(1 + m.M22 - m.M00 - m.M11);
				half = 0.5f / sqrt;
				o.X = (m.M20 + m.M02) * half;
				o.Y = (m.M21 + m.M12) * half;
				o.Z = sqrt / 2;
				o.W = (m.M01 - m.M10) * half;
			}
		}
		#endregion // Creation

		#region Operators
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Quaternion l, in Quaternion r) => 
			(l.X == r.X) && (l.Y == r.Y) && (l.Z == r.Z) && (l.W == r.W);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Quaternion l, in Quaternion r) =>
			(l.X != r.X) || (l.Y != r.Y) || (l.Z != r.Z) || (l.W != r.W);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Quaternion operator + (in Quaternion l, in Quaternion r)
		{
			Add(l, r, out Quaternion o);
			return o;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Quaternion operator - (in Quaternion l, in Quaternion r)
		{
			Subtract(l, r, out Quaternion o);
			return o;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Quaternion operator - (in Quaternion m)
		{
			Negate(m, out Quaternion o);
			return o;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Quaternion operator * (in Quaternion l, float r)
		{
			Multiply(l, r, out Quaternion o);
			return o;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Quaternion operator * (in Quaternion l, in Quaternion r)
		{
			Multiply(l, r, out Quaternion o);
			return o;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Quaternion operator / (in Quaternion l, float r)
		{
			Divide(l, r, out Quaternion o);
			return o;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Quaternion operator / (in Quaternion l, in Quaternion r)
		{
			Divide(l, r, out Quaternion o);
			return o;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Vec4 (in Quaternion q) => 
			new Vec4(q.X, q.Y, q.Z, q.W);
		#endregion // Operators

		#region Tuples
		public readonly void Deconstruct(out float x, out float y, out float z, out float w)
		{
			x = X;
			y = Y;
			z = Z;
			w = W;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Quaternion (in (float x, float y, float z, float w) tup) =>
			new Quaternion(tup.x, tup.y, tup.z, tup.w);
		#endregion // Tuples
	}
}
