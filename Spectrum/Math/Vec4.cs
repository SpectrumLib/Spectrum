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
	/// Describes a 4-component number in cartesian space.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Pack=0, Size=4*sizeof(float))]
	public struct Vec4 : IEquatable<Vec4>
	{
		#region Vector Constants
		/// <summary>
		/// The vector with all components as zero.
		/// </summary>
		public static readonly Vec4 Zero = new Vec4(0, 0, 0, 0);
		/// <summary>
		/// The vector with all components as one.
		/// </summary>
		public static readonly Vec4 One = new Vec4(1, 1, 1, 1);
		/// <summary>
		/// A unit vector along the positive x-axis.
		/// </summary>
		public static readonly Vec4 UnitX = new Vec4(1, 0, 0, 0);
		/// <summary>
		/// A unit vector along the positive y-axis.
		/// </summary>
		public static readonly Vec4 UnitY = new Vec4(0, 1, 0, 0);
		/// <summary>
		/// A unit vector along the positive z-axis.
		/// </summary>
		public static readonly Vec4 UnitZ = new Vec4(0, 0, 1, 0);
		/// <summary>
		/// A unit vector along the positive w-axis.
		/// </summary>
		public static readonly Vec4 UnitW = new Vec4(0, 0, 0, 1);
		#endregion // Vector Constants

		#region Fields
		/// <summary>
		/// The x-axis component.
		/// </summary>
		[FieldOffset(0)]
		public float X;
		/// <summary>
		/// The y-axis component.
		/// </summary>
		[FieldOffset(sizeof(float))]
		public float Y;
		/// <summary>
		/// The z-axis component.
		/// </summary>
		[FieldOffset(2*sizeof(float))]
		public float Z;
		/// <summary>
		/// The w-axis component.
		/// </summary>
		[FieldOffset(3*sizeof(float))]
		public float W;

		/// <summary>
		/// Gets the length of the vector.
		/// </summary>
		public readonly float Length => MathF.Sqrt(X * X + Y * Y + Z * Z + W * W);
		/// <summary>
		/// Gets the square of the length of the vector.
		/// </summary>
		public readonly float LengthSquared => X * X + Y * Y + Z * Z + W * W;
		/// <summary>
		/// Gets the normalized version of this vector.
		/// </summary>
		public readonly Vec4 Normalized => this * (1f / Length);
		#endregion // Fields

		#region Ctor
		/// <summary>
		/// Creates a new vector with the same value for all components.
		/// </summary>
		/// <param name="f">The value for all components.</param>
		public Vec4(float f)
		{
			X = Y = Z = W = f;
		}

		/// <summary>
		/// Creates a new vector by adding z and w components to an existing <see cref="Vec2"/>.
		/// </summary>
		/// <param name="v">The vector for the x- and y- components.</param>
		/// <param name="z">The z-component.</param>
		/// <param name="w">The w-component.</param>
		public Vec4(in Vec2 v, float z, float w)
		{
			X = v.X;
			Y = v.Y;
			Z = z;
			W = w;
		}

		/// <summary>
		/// Creates a new vector by adding a w component to an existing <see cref="Vec3"/>.
		/// </summary>
		/// <param name="v">The vector for the x-, y-, and z- components.</param>
		/// <param name="w">The w-component.</param>
		public Vec4(in Vec3 v, float w)
		{
			X = v.X;
			Y = v.Y;
			Z = v.Z;
			W = w;
		}

		/// <summary>
		/// Creates a new vector with the given components.
		/// </summary>
		/// <param name="x">The x-component.</param>
		/// <param name="y">The y-component.</param>
		/// <param name="z">The z-component.</param>
		/// <param name="w">The w-component.</param>
		public Vec4(float x, float y, float z, float w)
		{
			X = x;
			Y = y;
			Z = z;
			W = w;
		}
		#endregion // Ctor

		#region Overrides
		readonly bool IEquatable<Vec4>.Equals(Vec4 v) => v == this;

		public readonly override bool Equals(object obj) => (obj is Vec4) && ((Vec4)obj == this);

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

		public readonly override string ToString() => $"{{{X} {Y} {Z}}}";
		#endregion // Overrides

		#region Distance
		/// <summary>
		/// Gets the distance between two vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Distance(in Vec4 l, in Vec4 r)
		{
			float dx = l.X - r.X, dy = l.Y - r.Y, dz = l.Z - r.Z, dw = l.W - r.W;
			return MathF.Sqrt(dx * dx + dy * dy + dz * dz + dw * dw);
		}

		/// <summary>
		/// Gets the distance squared between two vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float DistanceSquared(in Vec4 l, in Vec4 r)
		{
			float dx = l.X - r.X, dy = l.Y - r.Y, dz = l.Z - r.Z, dw = l.W - r.W;
			return dx * dx + dy * dy + dz * dz + dw * dw;
		}
		#endregion // Distance

		#region Vector Functions
		/// <summary>
		/// Calculates the normalized vector.
		/// </summary>
		/// <param name="l">The vector to normalize.</param>
		/// <returns>The normalized vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 Normalize(in Vec4 l) => l.Normalized;

		/// <summary>
		/// Calculates the normalized vector.
		/// </summary>
		/// <param name="l">The vector to normalize.</param>
		/// <param name="o">The normalized vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Normalize(in Vec4 l, out Vec4 o) => o = l.Normalized;

		/// <summary>
		/// Calculates the dot product of the two vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Dot(in Vec4 l, in Vec4 r) => l.X * r.X + l.Y * r.Y + l.Z * r.Z + l.W * r.W;

		/// <summary>
		/// Compares two vectors to see if they are equal within a certain relative limit.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <param name="eps">The maximum relative difference between elements to be considered equal.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool NearlyEqual(in Vec4 l, in Vec4 r, float eps = MathHelper.MAX_REL_EPS_F) =>
			MathHelper.NearlyEqual(l.X, r.X, eps) && MathHelper.NearlyEqual(l.Y, r.Y, eps) &&
			MathHelper.NearlyEqual(l.Z, r.Z, eps) && MathHelper.NearlyEqual(l.W, r.W, eps);
		#endregion // Vector Functions

		#region Operators
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Vec4 l, in Vec4 r) => (l.X == r.X) && (l.Y == r.Y) && (l.Z == r.Z) && (l.W == r.W);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Vec4 l, in Vec4 r) => (l.X != r.X) || (l.Y != r.Y) || (l.Z == r.Z) || (l.W == r.W);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 operator + (in Vec4 l, in Vec4 r) => new Vec4(l.X + r.X, l.Y + r.Y, l.Z + r.Z, l.W + r.W);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 operator - (in Vec4 l, in Vec4 r) => new Vec4(l.X - r.X, l.Y - r.Y, l.Z - r.Z, l.W - r.W);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 operator * (in Vec4 l, in Vec4 r) => new Vec4(l.X * r.X, l.Y * r.Y, l.Z * r.Z, l.W * r.W);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 operator * (in Vec4 l, float r) => new Vec4(l.X * r, l.Y * r, l.Z * r, l.W * r);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 operator * (float l, in Vec4 r) => new Vec4(l * r.X, l * r.Y, l * r.Z, l * r.W);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 operator /(in Vec4 l, in Vec4 r) => new Vec4(l.X / r.X, l.Y / r.Y, l.Z / r.Z, l.W / r.W);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 operator / (in Vec4 l, float r) => new Vec4(l.X / r, l.Y / r, l.Z / r, l.W / r);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 operator - (in Vec4 v) => new Vec4(-v.X, -v.Y, -v.Z, -v.W);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Vec4 (in Vec2 v) => new Vec4(v.X, v.Y, 0, 0);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Vec4 (in Vec3 v) => new Vec4(v.X, v.Y, v.Z, 0);
		#endregion // Operators

		#region Standard Math
		/// <summary>
		/// Component-wise maximum of the two input vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <returns>The output value for the minimized vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 Min(in Vec4 l, in Vec4 r) =>
			new Vec4(l.X < r.X ? l.X : r.X, l.Y < r.Y ? l.Y : r.Y, l.Z < r.Z ? l.Z : r.Z, l.W < r.W ? l.W : r.W);

		/// <summary>
		/// Component-wise maximum of the two input vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <param name="p">The output value for the minimized vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Min(in Vec4 l, in Vec4 r, out Vec4 p) => p =
			new Vec4(l.X < r.X ? l.X : r.X, l.Y < r.Y ? l.Y : r.Y, l.Z < r.Z ? l.Z : r.Z, l.W < r.W ? l.W : r.W);

		/// <summary>
		/// Component-wise minimum of the two input vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <returns>The output value for the maximized vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 Max(in Vec4 l, in Vec4 r) =>
			new Vec4(l.X > r.X ? l.X : r.X, l.Y > r.Y ? l.Y : r.Y, l.Z > r.Z ? l.Z : r.Z, l.W > r.W ? l.W : r.W);

		/// <summary>
		/// Component-wise minimum of the two input vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <param name="p">The output value for the maximized vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Max(in Vec4 l, in Vec4 r, out Vec4 p) => p =
			new Vec4(l.X > r.X ? l.X : r.X, l.Y > r.Y ? l.Y : r.Y, l.Z > r.Z ? l.Z : r.Z, l.W > r.W ? l.W : r.W);

		/// <summary>
		/// Component-wise clamp between of the two limiting vectors.
		/// </summary>
		/// <param name="val">The vector to clamp.</param>
		/// <param name="min">The minimum bounding vector.</param>
		/// <param name="max">The maximum bounding vector.</param>
		/// <returns>The output clamped vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 Clamp(in Vec4 val, in Vec4 min, in Vec4 max) =>
			new Vec4(Math.Clamp(val.X, min.X, max.X), Math.Clamp(val.Y, min.Y, max.Y), Math.Clamp(val.Z, min.Z, max.Z),
					 Math.Clamp(val.W, min.W, max.W));

		/// <summary>
		/// Component-wise clamp between of the two limiting vectors.
		/// </summary>
		/// <param name="val">The vector to clamp.</param>
		/// <param name="min">The minimum bounding vector.</param>
		/// <param name="max">The maximum bounding vector.</param>
		/// <param name="p">The output clamped vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Clamp(in Vec4 val, in Vec4 min, in Vec4 max, out Vec4 p) => p =
			new Vec4(Math.Clamp(val.X, min.X, max.X), Math.Clamp(val.Y, min.Y, max.Y), Math.Clamp(val.Z, min.Z, max.Z),
					 Math.Clamp(val.W, min.W, max.W));

		/// <summary>
		/// Component-wise rounding towards positive infinity.
		/// </summary>
		/// <param name="v">The vector to round.</param>
		/// <returns>The output rounded vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 Ceiling(in Vec4 v) =>
			new Vec4(MathF.Ceiling(v.X), MathF.Ceiling(v.Y), MathF.Ceiling(v.Z), MathF.Ceiling(v.W));

		/// <summary>
		/// Component-wise rounding towards positive infinity.
		/// </summary>
		/// <param name="v">The vector to round.</param>
		/// <param name="o">The output rounded vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Ceiling(in Vec4 v, out Vec4 o) => o =
			new Vec4(MathF.Ceiling(v.X), MathF.Ceiling(v.Y), MathF.Ceiling(v.Z), MathF.Ceiling(v.W));

		/// <summary>
		/// Component-wise rounding towards negative infinity.
		/// </summary>
		/// <param name="v">The vector to round.</param>
		/// <returns>The output rounded vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 Floor(in Vec4 v) =>
			new Vec4(MathF.Floor(v.X), MathF.Floor(v.Y), MathF.Floor(v.Z), MathF.Floor(v.W));

		/// <summary>
		/// Component-wise rounding towards negative infinity.
		/// </summary>
		/// <param name="v">The vector to round.</param>
		/// <param name="o">The output rounded vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Floor(in Vec4 v, out Vec4 o) => o =
			new Vec4(MathF.Floor(v.X), MathF.Floor(v.Y), MathF.Floor(v.Z), MathF.Floor(v.W));

		/// <summary>
		/// Component-wise rounding towards the nearest integer.
		/// </summary>
		/// <param name="v">The vector to round.</param>
		/// <returns>The output rounded vector.</returns>
		public static Vec4 Round(in Vec4 v) =>
			new Vec4(MathF.Round(v.X), MathF.Round(v.Y), MathF.Round(v.Z), MathF.Round(v.W));

		/// <summary>
		/// Component-wise rounding towards the nearest integer.
		/// </summary>
		/// <param name="v">The vector to round.</param>
		/// <param name="o">The output rounded vector.</param>
		public static void Round(in Vec4 v, out Vec4 o) => o =
			new Vec4(MathF.Round(v.X), MathF.Round(v.Y), MathF.Round(v.Z), MathF.Round(v.W));
		#endregion // Standard Math

		#region Tuples
		public readonly void Deconstruct(out float x, out float y, out float z, out float w)
		{
			x = X;
			y = Y;
			z = Z;
			w = W;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Vec4 (in (float x, float y, float z, float w) tup) =>
			new Vec4(tup.x, tup.y, tup.z, tup.w);
		#endregion // Tuples
	}
}
