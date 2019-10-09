/*
 * GNU LGPLv3 License - Copyright (c) The Spectrum Team
 * This file is subject to the terms and conditions of the GNU LGPLv3 license, the text of which can be found in the
 * 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/LGPL-3.0>.
 */
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Spectrum
{
	/// <summary>
	/// Describes a 3-component number in cartesian space.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Pack=0, Size=3*sizeof(float))]
	public struct Vec3 : IEquatable<Vec3>
	{
		#region Vector Constants
		/// <summary>
		/// The vector with all components as zero.
		/// </summary>
		public static readonly Vec3 Zero = new Vec3(0, 0, 0);
		/// <summary>
		/// The vector with all components as one.
		/// </summary>
		public static readonly Vec3 One = new Vec3(1, 1, 1);
		/// <summary>
		/// A unit vector along the positive x-axis.
		/// </summary>
		public static readonly Vec3 UnitX = new Vec3(1, 0, 0);
		/// <summary>
		/// A unit vector along the positive y-axis.
		/// </summary>
		public static readonly Vec3 UnitY = new Vec3(0, 1, 0);
		/// <summary>
		/// A unit vector along the positive z-axis.
		/// </summary>
		public static readonly Vec3 UnitZ = new Vec3(0, 0, 1);
		/// <summary>
		/// Unit vector pointing "right" in right hand coordinates (+x).
		/// </summary>
		public static readonly Vec3 Right = new Vec3(1, 0, 0);
		/// <summary>
		/// Unit vector pointing "left" in right hand coordinates (-x).
		/// </summary>
		public static readonly Vec3 Left = new Vec3(-1, 0, 0);
		/// <summary>
		/// Unit vector pointing "up" in right hand coordinates (+y).
		/// </summary>
		public static readonly Vec3 Up = new Vec3(0, 1, 0);
		/// <summary>
		/// Unit vector pointing "down" in right hand coordinates (-y).
		/// </summary>
		public static readonly Vec3 Down = new Vec3(0, -1, 0);
		/// <summary>
		/// Unit vector pointing "backward" in right hand coordinates (+z).
		/// </summary>
		public static readonly Vec3 Backward = new Vec3(0, 0, 1);
		/// <summary>
		/// Unit vector pointing "forward" in right hand coordinates (-z).
		/// </summary>
		public static readonly Vec3 Forward = new Vec3(0, 0, -1);
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
		/// Gets the length of the vector.
		/// </summary>
		public readonly float Length => MathF.Sqrt(X * X + Y * Y + Z * Z);
		/// <summary>
		/// Gets the square of the length of the vector.
		/// </summary>
		public readonly float LengthSquared => X * X + Y * Y + Z * Z;
		/// <summary>
		/// Gets the normalized version of this vector.
		/// </summary>
		public readonly Vec3 Normalized => this * (1f / Length);
		#endregion // Fields

		#region Ctor
		/// <summary>
		/// Creates a new vector with the same value for all components.
		/// </summary>
		/// <param name="f">The value for all components.</param>
		public Vec3(float f)
		{
			X = Y = Z = f;
		}

		/// <summary>
		/// Creates a new vector by adding a z component to an existing <see cref="Vec2"/>.
		/// </summary>
		/// <param name="v">The vector for the x- and y- components.</param>
		/// <param name="z">The z-component.</param>
		public Vec3(in Vec2 v, float z)
		{
			X = v.X;
			Y = v.Y;
			Z = z;
		}

		/// <summary>
		/// Creates a new vector with the given components.
		/// </summary>
		/// <param name="x">The x-component.</param>
		/// <param name="y">The y-component.</param>
		/// <param name="z">The z-component.</param>
		public Vec3(float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;
		}
		#endregion // Ctor

		#region Overrides
		readonly bool IEquatable<Vec3>.Equals(Vec3 v) => v == this;

		public readonly override bool Equals(object obj) => (obj is Vec3) && ((Vec3)obj == this);

		public readonly override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + X.GetHashCode();
				hash = (hash * 23) + Y.GetHashCode();
				hash = (hash * 23) + Z.GetHashCode();
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
		public static float Distance(in Vec3 l, in Vec3 r)
		{
			float dx = l.X - r.X, dy = l.Y - r.Y, dz = l.Z - r.Z;
			return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
		}

		/// <summary>
		/// Gets the distance squared between two vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float DistanceSquared(in Vec3 l, in Vec3 r)
		{
			float dx = l.X - r.X, dy = l.Y - r.Y, dz = l.Z - r.Z;
			return dx * dx + dy * dy + dz * dz;
		}
		#endregion // Distance

		#region Vector Functions
		/// <summary>
		/// Calculates the normalized vector.
		/// </summary>
		/// <param name="l">The vector to normalize.</param>
		/// <returns>The normalized vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 Normalize(in Vec3 l) => l.Normalized;

		/// <summary>
		/// Calculates the normalized vector.
		/// </summary>
		/// <param name="l">The vector to normalize.</param>
		/// <param name="o">The normalized vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Normalize(in Vec3 l, out Vec3 o) => o = l.Normalized;

		/// <summary>
		/// Calculates the dot product of the two vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Dot(in Vec3 l, in Vec3 r) => l.X * r.X + l.Y * r.Y + l.Z * r.Z;

		/// <summary>
		/// Calculates the right hand cross product of the vectors.
		/// </summary>
		/// <param name="l">The first vector to cross.</param>
		/// <param name="r">The second vector to cross.</param>
		/// <returns>The cross product.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 Cross(in Vec3 l, in Vec3 r)
		{
			Cross(l, r, out var o);
			return o;
		}

		/// <summary>
		/// Calculates the right hand cross product of the vectors.
		/// </summary>
		/// <param name="l">The first vector to cross.</param>
		/// <param name="r">The second vector to cross.</param>
		/// <param name="o">The cross product.</param>
		public static void Cross(in Vec3 l, in Vec3 r, out Vec3 o)
		{
			o.X = (l.Y * r.Z) - (l.Z * r.Y);
			o.Y = (l.X * r.Z) - (l.Z * r.X);
			o.Z = (l.X * r.Y) - (l.Y * r.X);
		}

		/// <summary>
		/// Projects the first vector onto the second vector.
		/// </summary>
		/// <param name="l">The vector to project.</param>
		/// <param name="r">The vector to project onto.</param>
		/// <returns>The projected vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 Project(in Vec3 l, in Vec3 r)
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
		public static void Project(in Vec3 l, in Vec3 r, out Vec3 o)
		{
			float rlen = MathF.Sqrt(r.X * r.X + r.Y * r.Y + r.Z * r.Z);
			Vec3 unitr = new Vec3(r.X / rlen, r.Y / rlen, r.Z / rlen);
			float a = l.X * unitr.X + l.Y * unitr.Y + l.Z * unitr.Z;
			o.X = unitr.X * a;
			o.Y = unitr.Y * a;
			o.Z = unitr.Z * a;
		}

		/// <summary>
		/// Reflects a vector over the axis defined by the second vector.
		/// </summary>
		/// <param name="v">The vector to reflect.</param>
		/// <param name="n">The axis vector to reflect around, must be normalized.</param>
		/// <returns>The output reflected vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 Reflect(in Vec3 v, in Vec3 n)
		{
			Reflect(v, n, out var o);
			return o;
		}

		/// <summary>
		/// Reflects a vector over the axis defined by the second vector.
		/// </summary>
		/// <param name="v">The vector to reflect.</param>
		/// <param name="n">The axis vector to reflect around, must be normalized.</param>
		/// <param name="o">The output reflected vector.</param>
		public static void Reflect(in Vec3 v, in Vec3 n, out Vec3 o)
		{
			float f = 2 * ((v.X * n.X) + (v.Y * n.Y) + (v.Z * n.Z));
			o = new Vec3(v.X - (n.X * f), v.Y - (n.Y * f), v.Z - (n.Z * f));
		}

		/// <summary>
		/// Refracts a vector as is passing between two media of different refractive indices.
		/// </summary>
		/// <param name="i">The incident vector, should be normalized.</param>
		/// <param name="n">The surface normal, should be normalized.</param>
		/// <param name="eta">The ratio of refractive indices.</param>
		/// <returns>The output refracted vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 Refract(in Vec3 i, in Vec3 n, float eta)
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
		public static void Refract(in Vec3 i, in Vec3 n, float eta, out Vec3 o)
		{
			float d = Dot(n, i);
			float k = 1 - (eta * eta * (1 - (d * d)));
			o = (k < 0) ? Vec3.Zero : ((i * eta) - (n * ((eta * d) + MathF.Sqrt(k))));
		}

		/// <summary>
		/// Calculates the angle (in radians) between the two vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		public static float AngleBetween(in Vec3 l, in Vec3 r)
		{
			float dot = l.X * r.X + l.Y * r.Y + l.Z * r.Z;
			float llen = MathF.Sqrt(l.X * l.X + l.Y * l.Y + l.Z * l.Z);
			float rlen = MathF.Sqrt(r.X * r.X + r.Y * r.Y + r.Z * r.Z);
			return MathF.Acos(dot / (llen * rlen));
		}

		/// <summary>
		/// Compares two vectors to see if they are equal within a certain relative limit.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <param name="eps">The maximum relative difference between elements to be considered equal.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool NearlyEqual(in Vec3 l, in Vec3 r, float eps = MathHelper.MAX_REL_EPS_F) =>
			MathHelper.NearlyEqual(l.X, r.X, eps) && MathHelper.NearlyEqual(l.Y, r.Y, eps) &&
			MathHelper.NearlyEqual(l.Z, r.Z, eps);
		#endregion // Vector Functions

		#region Operators
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Vec3 l, in Vec3 r) => (l.X == r.X) && (l.Y == r.Y) && (l.Z == r.Z);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Vec3 l, in Vec3 r) => (l.X != r.X) || (l.Y != r.Y) || (l.Z == r.Z);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 operator + (in Vec3 l, in Vec3 r) => new Vec3(l.X + r.X, l.Y + r.Y, l.Z + r.Z);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 operator - (in Vec3 l, in Vec3 r) => new Vec3(l.X - r.X, l.Y - r.Y, l.Z - r.Z);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 operator * (in Vec3 l, in Vec3 r) => new Vec3(l.X * r.X, l.Y * r.Y, l.Z * r.Z);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 operator * (in Vec3 l, float r) => new Vec3(l.X * r, l.Y * r, l.Z * r);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 operator * (float l, in Vec3 r) => new Vec3(l * r.X, l * r.Y, l * r.Z);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 operator /(in Vec3 l, in Vec3 r) => new Vec3(l.X / r.X, l.Y / r.Y, l.Z / r.Z);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 operator / (in Vec3 l, float r) => new Vec3(l.X / r, l.Y / r, l.Z / r);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 operator - (in Vec3 v) => new Vec3(-v.X, -v.Y, -v.Z);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Point3 (in Vec3 v) => new Point3((int)v.X, (int)v.Y, (int)v.Z);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Vec3 (in Vec2 v) => new Vec3(v.X, v.Y, 0);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Vec3 (in Vec4 v) => new Vec3(v.X, v.Y, v.Z);
		#endregion // Operators

		#region Standard Math
		/// <summary>
		/// Component-wise maximum of the two input vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <returns>The output value for the minimized vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 Min(in Vec3 l, in Vec3 r) =>
			new Vec3(l.X < r.X ? l.X : r.X, l.Y < r.Y ? l.Y : r.Y, l.Z < r.Z ? l.Z : r.Z);

		/// <summary>
		/// Component-wise maximum of the two input vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <param name="p">The output value for the minimized vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Min(in Vec3 l, in Vec3 r, out Vec3 p) => p =
			new Vec3(l.X < r.X ? l.X : r.X, l.Y < r.Y ? l.Y : r.Y, l.Z < r.Z ? l.Z : r.Z);

		/// <summary>
		/// Component-wise minimum of the two input vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <returns>The output value for the maximized vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 Max(in Vec3 l, in Vec3 r) =>
			new Vec3(l.X > r.X ? l.X : r.X, l.Y > r.Y ? l.Y : r.Y, l.Z > r.Z ? l.Z : r.Z);

		/// <summary>
		/// Component-wise minimum of the two input vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <param name="p">The output value for the maximized vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Max(in Vec3 l, in Vec3 r, out Vec3 p) => p =
			new Vec3(l.X > r.X ? l.X : r.X, l.Y > r.Y ? l.Y : r.Y, l.Z > r.Z ? l.Z : r.Z);

		/// <summary>
		/// Component-wise clamp between of the two limiting vectors.
		/// </summary>
		/// <param name="val">The vector to clamp.</param>
		/// <param name="min">The minimum bounding vector.</param>
		/// <param name="max">The maximum bounding vector.</param>
		/// <returns>The output clamped vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 Clamp(in Vec3 val, in Vec3 min, in Vec3 max) =>
			new Vec3(Math.Clamp(val.X, min.X, max.X), Math.Clamp(val.Y, min.Y, max.Y), Math.Clamp(val.Z, min.Z, max.Z));

		/// <summary>
		/// Component-wise clamp between of the two limiting vectors.
		/// </summary>
		/// <param name="val">The vector to clamp.</param>
		/// <param name="min">The minimum bounding vector.</param>
		/// <param name="max">The maximum bounding vector.</param>
		/// <param name="p">The output clamped vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Clamp(in Vec3 val, in Vec3 min, in Vec3 max, out Vec3 p) => p =
			new Vec3(Math.Clamp(val.X, min.X, max.X), Math.Clamp(val.Y, min.Y, max.Y), Math.Clamp(val.Z, min.Z, max.Z));

		/// <summary>
		/// Component-wise rounding towards positive infinity.
		/// </summary>
		/// <param name="v">The vector to round.</param>
		/// <returns>The output rounded vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 Ceiling(in Vec3 v) =>
			new Vec3(MathF.Ceiling(v.X), MathF.Ceiling(v.Y), MathF.Ceiling(v.Z));

		/// <summary>
		/// Component-wise rounding towards positive infinity.
		/// </summary>
		/// <param name="v">The vector to round.</param>
		/// <param name="o">The output rounded vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Ceiling(in Vec3 v, out Vec3 o) => o =
			new Vec3(MathF.Ceiling(v.X), MathF.Ceiling(v.Y), MathF.Ceiling(v.Z));

		/// <summary>
		/// Component-wise rounding towards negative infinity.
		/// </summary>
		/// <param name="v">The vector to round.</param>
		/// <returns>The output rounded vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 Floor(in Vec3 v) =>
			new Vec3(MathF.Floor(v.X), MathF.Floor(v.Y), MathF.Floor(v.Z));

		/// <summary>
		/// Component-wise rounding towards negative infinity.
		/// </summary>
		/// <param name="v">The vector to round.</param>
		/// <param name="o">The output rounded vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Floor(in Vec3 v, out Vec3 o) => o =
			new Vec3(MathF.Floor(v.X), MathF.Floor(v.Y), MathF.Floor(v.Z));

		/// <summary>
		/// Component-wise rounding towards the nearest integer.
		/// </summary>
		/// <param name="v">The vector to round.</param>
		/// <returns>The output rounded vector.</returns>
		public static Vec3 Round(in Vec3 v) =>
			new Vec3(MathF.Round(v.X), MathF.Round(v.Y), MathF.Round(v.Z));

		/// <summary>
		/// Component-wise rounding towards the nearest integer.
		/// </summary>
		/// <param name="v">The vector to round.</param>
		/// <param name="o">The output rounded vector.</param>
		public static void Round(in Vec3 v, out Vec3 o) => o =
			new Vec3(MathF.Round(v.X), MathF.Round(v.Y), MathF.Round(v.Z));
		#endregion // Standard Math

		#region Tuples
		public readonly void Deconstruct(out float x, out float y, out float z)
		{
			x = X;
			y = Y;
			z = Z;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Vec3 (in (float x, float y, float z) tup) =>
			new Vec3(tup.x, tup.y, tup.z);
		#endregion // Tuples
	}
}
