using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Spectrum
{
	/// <summary>
	/// Describes a 4-component number in cartesion space.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size = (4 * sizeof(float)))]
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
		/// The x-coordinate of the vector.
		/// </summary>
		[FieldOffset(0)]
		public float X;
		/// <summary>
		/// The y-coordinate of the vector.
		/// </summary>
		[FieldOffset(sizeof(float))]
		public float Y;
		/// <summary>
		/// The z-coordinate of the vector.
		/// </summary>
		[FieldOffset(2 * sizeof(float))]
		public float Z;
		/// <summary>
		/// The w-coordinate of the vector.
		/// </summary>
		[FieldOffset(3 * sizeof(float))]
		public float W;
		#endregion // Fields

		/// <summary>
		/// Creates a new vector with all components with the same value.
		/// </summary>
		/// <param name="f">The value for all components.</param>
		public Vec4(float f)
		{
			X = Y = Z = W = f;
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

		/// <summary>
		/// Creates a vector from a 2D vector appended with z-axis and w-axis coordinates.
		/// </summary>
		/// <param name="v">The 2D vector for the X and Y coordinates.</param>
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
		/// Creates a vector from a 3D vector appended with a w-axis coordinate.
		/// </summary>
		/// <param name="v">The 2D vector for the X, Y, and Z coordinates.</param>
		/// <param name="w">The w-component.</param>
		public Vec4(in Vec3 v, float w)
		{
			X = v.X;
			Y = v.Y;
			Z = v.Z;
			W = w;
		}

		#region Overrides
		bool IEquatable<Vec4>.Equals(Vec4 v)
		{
			return X == v.X && Y == v.Y && Z == v.Z && W == v.W;
		}

		public override bool Equals(object obj)
		{
			return (obj as Vec4?)?.Equals(this) ?? false;
		}

		public override int GetHashCode()
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

		public override string ToString()
		{
			return $"{{{X} {Y} {Z} {W}}}";
		}
		#endregion // Overrides

		#region Length
		/// <summary>
		/// Gets the length of the vector.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float Length() => Mathf.Sqrt(X * X + Y * Y + Z * Z + W * W);

		/// <summary>
		/// Gets the square of the length of the vector.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float LengthSquared() => X * X + Y * Y + Z * Z + W * W;

		/// <summary>
		/// Gets the length of the vector.
		/// </summary>
		/// <param name="vec">The vector to get the length of.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Length(in Vec4 vec) => Mathf.Sqrt(vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z + vec.W * vec.W);

		/// <summary>
		/// Gets the square of the length of the vector.
		/// </summary>
		/// <param name="vec">The vector to get the squared length of.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float LengthSquared(in Vec4 vec) => vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z + vec.W * vec.W;

		/// <summary>
		/// Gets the distance between this and another vector.
		/// </summary>
		/// <param name="v">The other vector to get the distance to.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float Distance(in Vec4 v)
		{
			float dx = v.X - X, dy = v.Y - Y, dz = v.Z - Z, dw = v.W - W;
			return Mathf.Sqrt(dx * dx + dy * dy + dz * dz + dw * dw);
		}

		/// <summary>
		/// Gets the distance between two vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Distance(in Vec4 l, in Vec4 r)
		{
			float dx = l.X - r.X, dy = l.Y - r.Y, dz = l.Z - r.Z, dw = l.W - r.W;
			return Mathf.Sqrt(dx * dx + dy * dy + dz * dz + dw * dw);
		}

		/// <summary>
		/// Gets the distance squared between this and another vector.
		/// </summary>
		/// <param name="v">The other vector to get the distance to.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float DistanceSquared(in Vec4 v)
		{
			float dx = v.X - X, dy = v.Y - Y, dz = v.Z - Z, dw = v.W - W;
			return dx * dx + dy * dy + dz * dz + dw * dw;
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
		#endregion // Length

		#region Vector Functions
		/// <summary>
		/// Returns the vector in the same direction with a length of one.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vec4 Normalized()
		{
			float len = Mathf.Sqrt(X * X + Y * Y + Z * Z + W * W);
			return new Vec4(X / len, Y / len, Z / len, W / len);
		}

		/// <summary>
		/// Returns the vector in the same direction with a length of one.
		/// </summary>
		/// <param name="vec">The vector to normalize.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 Normalized(in Vec4 vec)
		{
			float len = Mathf.Sqrt(vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z + vec.W * vec.W);
			return new Vec4(vec.X / len, vec.Y / len, vec.Z / len, vec.W / len);
		}

		/// <summary>
		/// Returns the vector in the same direction with a length of one.
		/// </summary>
		/// <param name="vec">The vector to normalize.</param>
		/// <param name="o">The normalized vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Normalized(in Vec4 vec, out Vec4 o)
		{
			float len = Mathf.Sqrt(vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z + vec.W * vec.W);
			o = new Vec4(vec.X / len, vec.Y / len, vec.Z / len, vec.W / len);
		}

		/// <summary>
		/// Calculates the dot product of this vector with another vector.
		/// </summary>
		/// <param name="v">The vector to dot product with.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float Dot(in Vec4 v) => X * v.X + Y * v.Y + Z * v.Z + W * v.W;

		/// <summary>
		/// Calculates the dot product of the two vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Dot(in Vec4 l, in Vec4 r) => l.X * r.X + l.Y * r.Y + l.Z * r.Z + l.W * r.W;

		/// <summary>
		/// Compares two vectors to see if they are equal within a certain limit.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <param name="eps">The maximum difference between elements to be considered nearly equal.</param>
		public static bool NearlyEqual(in Vec4 l, in Vec4 r, float eps = 1e-5f)
		{
			return MathUtils.NearlyEqual(l.X, r.X, eps) && MathUtils.NearlyEqual(l.Y, r.Y, eps) &&
				MathUtils.NearlyEqual(l.Z, r.Z, eps) && MathUtils.NearlyEqual(l.W, r.W, eps);
		}
		#endregion // Vector Functions

		#region Operators
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Vec4 l, in Vec4 r)
		{
			return (l.X == r.X) && (l.Y == r.Y) && (l.Z == r.Z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Vec4 l, in Vec4 r)
		{
			return (l.X != r.X) || (l.Y != r.Y) || (l.Z != r.Z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 operator + (in Vec4 l, in Vec4 r)
		{
			return new Vec4(l.X + r.X, l.Y + r.Y, l.Z + r.Z, l.W + r.W);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 operator - (in Vec4 l, in Vec4 r)
		{
			return new Vec4(l.X - r.X, l.Y - r.Y, l.Z - r.Z, l.W - r.W);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 operator * (in Vec4 l, in Vec4 r)
		{
			return new Vec4(l.X * r.X, l.Y * r.Y, l.Z * r.Z, l.W * r.W);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 operator * (in Vec4 l, float r)
		{
			return new Vec4(l.X * r, l.Y * r, l.Z * r, l.W * r);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 operator * (float l, in Vec4 r)
		{
			return new Vec4(l * r.X, l * r.Y, l * r.Z, l * r.W);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 operator / (in Vec4 l, in Vec4 r)
		{
			return new Vec4(l.X / r.X, l.Y / r.Y, l.Z / r.Z, l.W / r.W);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 operator / (in Vec4 l, float r)
		{
			return new Vec4(l.X / r, l.Y / r, l.Z / r, l.W / r);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 operator - (in Vec4 v)
		{
			return new Vec4(-v.X, -v.Y, -v.Z, -v.W);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Vec2 (in Vec4 v)
		{
			return new Vec2(v.X, v.Y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Vec3 (in Vec4 v)
		{
			return new Vec3(v.X, v.Y, v.Z);
		}
		#endregion // Operators

		#region Min/Max
		/// <summary>
		/// Creates a new vector with the minimum components of the two input vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 Min(in Vec4 l, in Vec4 r)
		{
			return new Vec4(l.X < r.X ? l.X : r.X, l.Y < r.Y ? l.Y : r.Y, l.Z < r.Z ? l.Z : r.Z, l.W < r.W ? l.W : r.W);
		}
		/// <summary>
		/// Creates a new vector with the minimum components of the two input vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <param name="p">The output value for the minimized vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Min(in Vec4 l, in Vec4 r, out Vec4 p)
		{
			p = new Vec4(l.X < r.X ? l.X : r.X, l.Y < r.Y ? l.Y : r.Y, l.Z < r.Z ? l.Z : r.Z, l.W < r.W ? l.W : r.W);
		}

		/// <summary>
		/// Creates a new vector with the maximum components of the two input vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4 Max(in Vec4 l, in Vec4 r)
		{
			return new Vec4(l.X > r.X ? l.X : r.X, l.Y > r.Y ? l.Y : r.Y, l.Z > r.Z ? l.Z : r.Z, l.W > r.W ? l.W : r.W);
		}
		/// <summary>
		/// Creates a new vector with the maximum components of the two input vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <param name="p">The output value for the maximized vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Max(in Vec4 l, in Vec4 r, out Vec4 p)
		{
			p = new Vec4(l.X > r.X ? l.X : r.X, l.Y > r.Y ? l.Y : r.Y, l.Z > r.Z ? l.Z : r.Z, l.W > r.W ? l.W : r.W);
		}
		#endregion // Min/Max
	}
}
