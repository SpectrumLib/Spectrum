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
	/// Describes a 2-component number in cartesian space.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Pack=0, Size=2*sizeof(float))]
	public struct Vec2 : IEquatable<Vec2>
	{
		#region Vector Constants
		/// <summary>
		/// The vector with both components as zero.
		/// </summary>
		public static readonly Vec2 Zero = new Vec2(0, 0);
		/// <summary>
		/// The vector with both components as one.
		/// </summary>
		public static readonly Vec2 One = new Vec2(1, 1);
		/// <summary>
		/// A unit vector along the positive x-axis.
		/// </summary>
		public static readonly Vec2 UnitX = new Vec2(1, 0);
		/// <summary>
		/// A unit vector along the positive y-axis.
		/// </summary>
		public static readonly Vec2 UnitY = new Vec2(0, 1);
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
		/// Gets the length of the vector.
		/// </summary>
		public readonly float Length => MathF.Sqrt(X * X + Y * Y);
		/// <summary>
		/// Gets the square of the length of the vector.
		/// </summary>
		public readonly float LengthSquared => X * X + Y * Y;
		/// <summary>
		/// Gets the normalized version of this vector.
		/// </summary>
		public readonly Vec2 Normalized => this * (1f / Length);
		#endregion // Fields

		#region Ctor
		/// <summary>
		/// Creates a new vector with the same value for both components.
		/// </summary>
		/// <param name="f">The value for both components.</param>
		public Vec2(float f)
		{
			X = Y = f;
		}

		/// <summary>
		/// Creates a new vector with the given components.
		/// </summary>
		/// <param name="x">The x-component.</param>
		/// <param name="y">The y-component.</param>
		public Vec2(float x, float y)
		{
			X = x;
			Y = y;
		}
		#endregion // Ctor

		#region Overrides
		readonly bool IEquatable<Vec2>.Equals(Vec2 v) => v == this;

		public readonly override bool Equals(object obj) => (obj is Vec2) && ((Vec2)obj == this);

		public readonly override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + X.GetHashCode();
				hash = (hash * 23) + Y.GetHashCode();
				return hash;
			}
		}

		public readonly override string ToString() => $"{{{X} {Y}}}";
		#endregion // Overrides

		#region Distance
		/// <summary>
		/// Gets the distance between two vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Distance(in Vec2 l, in Vec2 r)
		{
			float dx = l.X - r.X, dy = l.Y - r.Y;
			return MathF.Sqrt(dx * dx + dy * dy);
		}

		/// <summary>
		/// Gets the distance squared between two vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float DistanceSquared(in Vec2 l, in Vec2 r)
		{
			float dx = l.X - r.X, dy = l.Y - r.Y;
			return dx * dx + dy * dy;
		}
		#endregion // Distance

		#region Vector Functions
		/// <summary>
		/// Calculates the normalized vector.
		/// </summary>
		/// <param name="l">The vector to normalize.</param>
		/// <returns>The normalized vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 Normalize(in Vec2 l) => l.Normalized;

		/// <summary>
		/// Calculates the normalized vector.
		/// </summary>
		/// <param name="l">The vector to normalize.</param>
		/// <param name="o">The normalized vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Normalize(in Vec2 l, out Vec2 o) => o = l.Normalized;
		
		/// <summary>
		/// Calculates the dot product of the two vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Dot(in Vec2 l, in Vec2 r) => l.X * r.X + l.Y * r.Y;

		/// <summary>
		/// Projects the first vector onto the second vector.
		/// </summary>
		/// <param name="l">The vector to project.</param>
		/// <param name="r">The vector to project onto.</param>
		/// <returns>The projected vector.</returns>
		public static Vec2 Project(in Vec2 l, in Vec2 r)
		{
			Project(l, r, out var o);
			return o;
		}

		/// <summary>
		/// Projects the first vector onto the second vector.
		/// </summary>
		/// <param name="l">The vector to project.</param>
		/// <param name="r">The vector to project onto.</param>
		/// <param name="o">The projected vector.</param>
		public static void Project(in Vec2 l, in Vec2 r, out Vec2 o)
		{
			float rlen = MathF.Sqrt(r.X * r.X + r.Y * r.Y);
			Vec2 unitr = new Vec2(r.X / rlen, r.Y / rlen);
			float a = l.X * unitr.X + l.Y * unitr.Y;
			o.X = unitr.X * a;
			o.Y = unitr.Y * a;
		}

		/// <summary>
		/// Reflects a vector over the axis defined by the second vector.
		/// </summary>
		/// <param name="v">The vector to reflect.</param>
		/// <param name="n">The axis vector to reflect around, must be normalized.</param>
		/// <returns>The output reflected vector.</returns>
		public static Vec2 Reflect(in Vec2 v, in Vec2 n)
		{
			float f = 2 * ((v.X * n.X) + (v.Y * n.Y));
			return new Vec2(v.X - (n.X * f), v.Y - (n.Y * f));
		}

		/// <summary>
		/// Reflects a vector over the axis defined by the second vector.
		/// </summary>
		/// <param name="v">The vector to reflect.</param>
		/// <param name="n">The axis vector to reflect around, must be normalized.</param>
		/// <param name="o">The output reflected vector.</param>
		public static void Reflect(in Vec2 v, in Vec2 n, out Vec2 o)
		{
			float f = 2 * ((v.X * n.X) + (v.Y * n.Y));
			o = new Vec2(v.X - (n.X * f), v.Y - (n.Y * f));
		}

		/// <summary>
		/// Refracts a vector as is passing between two media of different refractive indices.
		/// </summary>
		/// <param name="i">The incident vector, should be normalized.</param>
		/// <param name="n">The surface normal, should be normalized.</param>
		/// <param name="eta">The ratio of refractive indices.</param>
		/// <returns>The output refracted vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 Refract(in Vec2 i, in Vec2 n, float eta)
		{
			Refract(i, n, eta, out var o);
			return o;
		}

		/// <summary>
		/// Refracts a vector as is passing between two media of different refractive indices.
		/// </summary>
		/// <param name="i">The incident vector, should be normalized.</param>
		/// <param name="n">The surface normal, should be normalized.</param>
		/// <param name="eta">The ratio of refractive indices.</param>
		/// <param name="o">The output refracted vector.</param>
		public static void Refract(in Vec2 i, in Vec2 n, float eta, out Vec2 o)
		{
			float d = Dot(n, i);
			float k = 1 - (eta * eta * (1 - (d * d)));
			o = (k < 0) ? Vec2.Zero : ((i * eta) - (n * ((eta * d) + MathF.Sqrt(k))));
		}

		/// <summary>
		/// Calculates the angle (in radians) between the two vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		public static float AngleBetween(in Vec2 l, in Vec2 r)
		{
			float dot = l.X * r.X + l.Y * r.Y;
			float llen = MathF.Sqrt(l.X * l.X + l.Y * l.Y);
			float rlen = MathF.Sqrt(r.X * r.X + r.Y * r.Y);
			return MathF.Acos(dot / (llen * rlen));
		}

		/// <summary>
		/// Compares two vectors to see if they are equal within a certain relative limit.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <param name="eps">The maximum relative difference between elements to be considered equal.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool NearlyEqual(in Vec2 l, in Vec2 r, float eps = MathHelper.MAX_REL_EPS_F) => 
			MathHelper.NearlyEqual(l.X, r.X, eps) && MathHelper.NearlyEqual(l.Y, r.Y, eps);
		#endregion // Vector Functions

		#region Operators
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Vec2 l, in Vec2 r) => (l.X == r.X) && (l.Y == r.Y);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Vec2 l, in Vec2 r) => (l.X != r.X) || (l.Y != r.Y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 operator + (in Vec2 l, in Vec2 r) => new Vec2(l.X + r.X, l.Y + r.Y);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 operator - (in Vec2 l, in Vec2 r) => new Vec2(l.X - r.X, l.Y - r.Y);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 operator * (in Vec2 l, in Vec2 r) => new Vec2(l.X * r.X, l.Y * r.Y);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 operator * (in Vec2 l, float r) => new Vec2(l.X * r, l.Y * r);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 operator * (float l, in Vec2 r) => new Vec2(l * r.X, l * r.Y);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 operator /(in Vec2 l, in Vec2 r) => new Vec2(l.X / r.X, l.Y / r.Y);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 operator / (in Vec2 l, float r) => new Vec2(l.X / r, l.Y / r);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 operator - (in Vec2 v) => new Vec2(-v.X, -v.Y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Point (in Vec2 v) => new Point((int)v.X, (int)v.Y);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Vec2 (in Vec3 v) => new Vec2(v.X, v.Y);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Vec2 (in Vec4 v) => new Vec2(v.X, v.Y);
		#endregion // Operators

		#region Standard Math
		/// <summary>
		/// Component-wise maximum of the two input vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <returns>The output value for the minimized vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 Min(in Vec2 l, in Vec2 r) =>
			new Vec2(l.X < r.X ? l.X : r.X, l.Y < r.Y ? l.Y : r.Y);

		/// <summary>
		/// Component-wise maximum of the two input vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <param name="p">The output value for the minimized vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Min(in Vec2 l, in Vec2 r, out Vec2 p) => p = 
			new Vec2(l.X < r.X ? l.X : r.X, l.Y < r.Y ? l.Y : r.Y);

		/// <summary>
		/// Component-wise minimum of the two input vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <returns>The output value for the maximized vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 Max(in Vec2 l, in Vec2 r) =>
			new Vec2(l.X > r.X ? l.X : r.X, l.Y > r.Y ? l.Y : r.Y);

		/// <summary>
		/// Component-wise minimum of the two input vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <param name="p">The output value for the maximized vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Max(in Vec2 l, in Vec2 r, out Vec2 p) => p = 
			new Vec2(l.X > r.X ? l.X : r.X, l.Y > r.Y ? l.Y : r.Y);

		/// <summary>
		/// Component-wise clamp between of the two limiting vectors.
		/// </summary>
		/// <param name="val">The vector to clamp.</param>
		/// <param name="min">The minimum bounding vector.</param>
		/// <param name="max">The maximum bounding vector.</param>
		/// <returns>The output clamped vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 Clamp(in Vec2 val, in Vec2 min, in Vec2 max) =>
			new Vec2(Math.Clamp(val.X, min.X, max.X), Math.Clamp(val.Y, min.Y, max.Y));

		/// <summary>
		/// Component-wise clamp between of the two limiting vectors.
		/// </summary>
		/// <param name="val">The vector to clamp.</param>
		/// <param name="min">The minimum bounding vector.</param>
		/// <param name="max">The maximum bounding vector.</param>
		/// <param name="p">The output clamped vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Clamp(in Vec2 val, in Vec2 min, in Vec2 max, out Vec2 p) => p =
			new Vec2(Math.Clamp(val.X, min.X, max.X), Math.Clamp(val.Y, min.Y, max.Y));

		/// <summary>
		/// Component-wise rounding towards positive infinity.
		/// </summary>
		/// <param name="v">The vector to round.</param>
		/// <returns>The output rounded vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 Ceiling(in Vec2 v) =>
			new Vec2(MathF.Ceiling(v.X), MathF.Ceiling(v.Y));

		/// <summary>
		/// Component-wise rounding towards positive infinity.
		/// </summary>
		/// <param name="v">The vector to round.</param>
		/// <param name="o">The output rounded vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Ceiling(in Vec2 v, out Vec2 o) => o =
			new Vec2(MathF.Ceiling(v.X), MathF.Ceiling(v.Y));

		/// <summary>
		/// Component-wise rounding towards negative infinity.
		/// </summary>
		/// <param name="v">The vector to round.</param>
		/// <returns>The output rounded vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 Floor(in Vec2 v) =>
			new Vec2(MathF.Floor(v.X), MathF.Floor(v.Y));

		/// <summary>
		/// Component-wise rounding towards negative infinity.
		/// </summary>
		/// <param name="v">The vector to round.</param>
		/// <param name="o">The output rounded vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Floor(in Vec2 v, out Vec2 o) => o =
			new Vec2(MathF.Floor(v.X), MathF.Floor(v.Y));

		/// <summary>
		/// Component-wise rounding towards the nearest integer.
		/// </summary>
		/// <param name="v">The vector to round.</param>
		/// <returns>The output rounded vector.</returns>
		public static Vec2 Round(in Vec2 v) =>
			new Vec2(MathF.Round(v.X), MathF.Round(v.Y));

		/// <summary>
		/// Component-wise rounding towards the nearest integer.
		/// </summary>
		/// <param name="v">The vector to round.</param>
		/// <param name="o">The output rounded vector.</param>
		public static void Round(in Vec2 v, out Vec2 o) => o =
			new Vec2(MathF.Round(v.X), MathF.Round(v.Y));
		#endregion // Standard Math

		#region Tuples
		public readonly void Deconstruct(out float x, out float y)
		{
			x = X;
			y = Y;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Vec2 (in (float x, float y) tup) =>
			new Vec2(tup.x, tup.y);
		#endregion // Tuples
	}
}
