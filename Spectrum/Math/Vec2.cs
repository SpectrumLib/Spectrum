﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Spectrum
{
	/// <summary>
	/// Describes a 2-component number in cartesion space.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size=(2*sizeof(float)))]
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
		/// The x-coordinate of the vector.
		/// </summary>
		[FieldOffset(0)]
		public float X;
		/// <summary>
		/// The y-coordinate of the vector.
		/// </summary>
		[FieldOffset(sizeof(float))]
		public float Y;
		#endregion // Fields

		/// <summary>
		/// Creates a new vector with both components with the same value.
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

		#region Overrides
		bool IEquatable<Vec2>.Equals(Vec2 v)
		{
			return X == v.X && Y == v.Y;
		}

		public override bool Equals(object obj)
		{
			return (obj as Vec2?)?.Equals(this) ?? false;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + X.GetHashCode();
				hash = (hash * 23) + Y.GetHashCode();
				return hash;
			}
		}

		public override string ToString()
		{
			return $"{{{X} {Y}}}";
		}
		#endregion // Overrides

		#region Length
		/// <summary>
		/// Gets the length of the vector.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float Length() => Mathf.Sqrt(X * X + Y * Y);

		/// <summary>
		/// Gets the square of the length of the vector.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float LengthSquared() => X * X + Y * Y;

		/// <summary>
		/// Gets the length of the vector.
		/// </summary>
		/// <param name="vec">The vector to get the length of.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Length(in Vec2 vec) => Mathf.Sqrt(vec.X * vec.X + vec.Y * vec.Y);

		/// <summary>
		/// Gets the square of the length of the vector.
		/// </summary>
		/// <param name="vec">The vector to get the squared length of.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float LengthSquared(in Vec2 vec) => vec.X * vec.X + vec.Y * vec.Y;

		/// <summary>
		/// Gets the distance between this and another vector.
		/// </summary>
		/// <param name="v">The other vector to get the distance to.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float Distance(in Vec2 v)
		{
			float dx = v.X - X, dy = v.Y - Y;
			return Mathf.Sqrt(dx * dx + dy * dy);
		}

		/// <summary>
		/// Gets the distance between two vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Distance(in Vec2 l, in Vec2 r)
		{
			float dx = l.X - r.X, dy = l.Y - r.Y;
			return Mathf.Sqrt(dx * dx + dy * dy);
		}

		/// <summary>
		/// Gets the distance squared between this and another vector.
		/// </summary>
		/// <param name="v">The other vector to get the distance to.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float DistanceSquared(in Vec2 v)
		{
			float dx = v.X - X, dy = v.Y - Y;
			return dx * dx + dy * dy;
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
		#endregion // Length

		#region Base Mathematic Operations
		/// <summary>
		/// Gives the component-wise sum of the vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 Add(in Vec2 l, in Vec2 r) => new Vec2(l.X + r.X, l.Y + r.Y);

		/// <summary>
		/// Gives the component-wise sum of the vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <param name="o">The output vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Add(in Vec2 l, in Vec2 r, out Vec2 o)
		{
			o.X = l.X + r.X;
			o.Y = l.Y + r.Y;
		}

		/// <summary>
		/// Gives the component-wise difference of the vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 Subtract(in Vec2 l, in Vec2 r) => new Vec2(l.X - r.X, l.Y - r.Y);

		/// <summary>
		/// Gives the component-wise difference of the vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <param name="o">The output vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Subtract(in Vec2 l, in Vec2 r, out Vec2 o)
		{
			o.X = l.X - r.X;
			o.Y = l.Y - r.Y;
		}

		/// <summary>
		/// Gives the component-wise product of the vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 Multiply(in Vec2 l, in Vec2 r) => new Vec2(l.X * r.X, l.Y * r.Y);

		/// <summary>
		/// Gives the component-wise product of the vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <param name="o">The output vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Multiply(in Vec2 l, in Vec2 r, out Vec2 o)
		{
			o.X = l.X * r.X;
			o.Y = l.Y * r.Y;
		}

		/// <summary>
		/// Gives the component-wise product of the vector and a scalar.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 Multiply(in Vec2 l, float r) => new Vec2(l.X * r, l.Y * r);

		/// <summary>
		/// Gives the component-wise product of the vector and a scalar.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <param name="o">The output vector.</param>
		public static void Multiply(in Vec2 l, float r, out Vec2 o)
		{
			o.X = l.X * r;
			o.Y = l.Y * r;
		}

		/// <summary>
		/// Gives the component-wise quotient of the vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 Divide(in Vec2 l, in Vec2 r) => new Vec2(l.X / r.X, l.Y / r.Y);

		/// <summary>
		/// Gives the component-wise quotient of the vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <param name="o">The output vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Divide(in Vec2 l, in Vec2 r, out Vec2 o)
		{
			o.X = l.X / r.X;
			o.Y = l.Y / r.Y;
		}

		/// <summary>
		/// Gives the component-wise quotient of the vector and a scalar.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 Divide(in Vec2 l, float r) => new Vec2(l.X / r, l.Y / r);

		/// <summary>
		/// Gives the component-wise quotient of the vector and a scalar.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <param name="o">The output vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Divide(in Vec2 l, float r, out Vec2 o)
		{
			o.X = l.X / r;
			o.Y = l.Y / r;
		}
		#endregion // Base Mathematic Operations

		#region Vector Functions
		/// <summary>
		/// Returns the vector in the same direction with a length of one.
		/// </summary>
		/// <param name="vec">The vector to normalize.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 Normalize(in Vec2 vec)
		{
			float len = Mathf.Sqrt(vec.X * vec.X + vec.Y * vec.Y);
			return new Vec2(vec.X / len, vec.Y / len);
		}

		/// <summary>
		/// Returns the vector in the same direction with a length of one.
		/// </summary>
		/// <param name="vec">The vector to normalize.</param>
		/// <param name="o">The normalized vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Normalize(in Vec2 vec, out Vec2 o)
		{
			float len = Mathf.Sqrt(vec.X * vec.X + vec.Y * vec.Y);
			o = new Vec2(vec.X / len, vec.Y / len);
		}

		/// <summary>
		/// Calculates the dot product of this vector with another vector.
		/// </summary>
		/// <param name="v">The vector to dot product with.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float Dot(in Vec2 v) => X * v.X + Y * v.Y;

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
		public static Vec2 Project(in Vec2 l, in Vec2 r)
		{
			float rlen = Mathf.Sqrt(r.X * r.X + r.Y * r.Y);
			Vec2 unitr = new Vec2(r.X / rlen, r.Y / rlen);
			float a = l.X * unitr.X + l.Y * unitr.Y;
			return new Vec2(unitr.X * a, unitr.Y * a);
		}

		/// <summary>
		/// Projects the first vector onto the second vector.
		/// </summary>
		/// <param name="l">The vector to project.</param>
		/// <param name="r">The vector to project onto.</param>
		/// <param name="o">The projected vector.</param>
		public static void Project(in Vec2 l, in Vec2 r, out Vec2 o)
		{
			float rlen = Mathf.Sqrt(r.X * r.X + r.Y * r.Y);
			Vec2 unitr = new Vec2(r.X / rlen, r.Y / rlen);
			float a = l.X * unitr.X + l.Y * unitr.Y;
			o.X = unitr.X * a;
			o.Y = unitr.Y * a;
		}

		/// <summary>
		/// Calculates the angle (in radians) between the two vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		public static float AngleBetween(in Vec2 l, in Vec2 r)
		{
			float dot = l.X * r.X + l.Y * r.Y;
			float llen = Mathf.Sqrt(l.X * l.X + l.Y * l.Y);
			float rlen = Mathf.Sqrt(r.X * r.X + r.Y * r.Y);
			return Mathf.Acos(dot / (llen * rlen));
		}

		/// <summary>
		/// Compares two vectors to see if they are equal within a certain limit.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <param name="eps">The maximum difference between elements to be considered nearly equal.</param>
		public static bool NearlyEqual(in Vec2 l, in Vec2 r, float eps = 1e-5f)
		{
			return MathUtils.NearlyEqual(l.X, r.X, eps) && MathUtils.NearlyEqual(l.Y, r.Y, eps);
		}
		#endregion // Vector Functions

		#region Operators
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Vec2 l, in Vec2 r)
		{
			return (l.X == r.X) && (l.Y == r.Y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Vec2 l, in Vec2 r)
		{
			return (l.X != r.X) || (l.Y != r.Y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 operator + (in Vec2 l, in Vec2 r)
		{
			return new Vec2(l.X + r.X, l.Y + r.Y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 operator - (in Vec2 l, in Vec2 r)
		{
			return new Vec2(l.X - r.X, l.Y - r.Y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 operator * (in Vec2 l, in Vec2 r)
		{
			return new Vec2(l.X * r.X, l.Y * r.Y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 operator * (in Vec2 l, float r)
		{
			return new Vec2(l.X * r, l.Y * r);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 operator * (float l, in Vec2 r)
		{
			return new Vec2(l * r.X, l * r.Y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 operator / (in Vec2 l, in Vec2 r)
		{
			return new Vec2(l.X / r.X, l.Y / r.Y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 operator / (in Vec2 l, float r)
		{
			return new Vec2(l.X / r, l.Y / r);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 operator - (in Vec2 v)
		{
			return new Vec2(-v.X, -v.Y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Point (in Vec2 v)
		{
			return new Point((int)v.X, (int)v.Y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Vec3 (in Vec2 v)
		{
			return new Vec3(v.X, v.Y, 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Vec4 (in Vec2 v)
		{
			return new Vec4(v.X, v.Y, 0, 0);
		}
		#endregion // Operators

		#region Min/Max
		/// <summary>
		/// Creates a new vector with the minimum components of the two input vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 Min(in Vec2 l, in Vec2 r)
		{
			return new Vec2(l.X < r.X ? l.X : r.X, l.Y < r.Y ? l.Y : r.Y);
		}
		/// <summary>
		/// Creates a new vector with the minimum components of the two input vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <param name="p">The output value for the minimized vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Min(in Vec2 l, in Vec2 r, out Vec2 p)
		{
			p = new Vec2(l.X < r.X ? l.X : r.X, l.Y < r.Y ? l.Y : r.Y);
		}

		/// <summary>
		/// Creates a new vector with the maximum components of the two input vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 Max(in Vec2 l, in Vec2 r)
		{
			return new Vec2(l.X > r.X ? l.X : r.X, l.Y > r.Y ? l.Y : r.Y);
		}
		/// <summary>
		/// Creates a new vector with the maximum components of the two input vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <param name="p">The output value for the maximized vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Max(in Vec2 l, in Vec2 r, out Vec2 p)
		{
			p = new Vec2(l.X > r.X ? l.X : r.X, l.Y > r.Y ? l.Y : r.Y);
		}
		#endregion // Min/Max
	}
}
