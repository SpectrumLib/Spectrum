using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Spectrum
{
	/// <summary>
	/// Describes a 3-component number in cartesion space.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size=(3*sizeof(float)))]
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
		[FieldOffset(2*sizeof(float))]
		public float Z;
		#endregion // Fields

		/// <summary>
		/// Creates a new vector with all components with the same value.
		/// </summary>
		/// <param name="f">The value for all components.</param>
		public Vec3(float f)
		{
			X = Y = Z = f;
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

		/// <summary>
		/// Creates a vector from a 2D vector appended with a z-axis coordinate.
		/// </summary>
		/// <param name="v">The 2D vector for the X and Y coordinates.</param>
		/// <param name="z">The z-component.</param>
		public Vec3(in Vec2 v, float z)
		{
			X = v.X;
			Y = v.Y;
			Z = z;
		}

		#region Overrides
		bool IEquatable<Vec3>.Equals(Vec3 v)
		{
			return X == v.X && Y == v.Y && Z == v.Z;
		}

		public override bool Equals(object obj)
		{
			return (obj as Vec3?)?.Equals(this) ?? false;
		}

		public override int GetHashCode()
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

		public override string ToString()
		{
			return $"{{{X} {Y} {Z}}}";
		}
		#endregion // Overrides

		#region Length
		/// <summary>
		/// Gets the length of the vector.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float Length() => Mathf.Sqrt(X * X + Y * Y + Z * Z);

		/// <summary>
		/// Gets the square of the length of the vector.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float LengthSquared() => X * X + Y * Y + Z * Z;

		/// <summary>
		/// Gets the length of the vector.
		/// </summary>
		/// <param name="vec">The vector to get the length of.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Length(in Vec3 vec) => Mathf.Sqrt(vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z);

		/// <summary>
		/// Gets the square of the length of the vector.
		/// </summary>
		/// <param name="vec">The vector to get the squared length of.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float LengthSquared(in Vec3 vec) => vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z;

		/// <summary>
		/// Gets the distance between this and another vector.
		/// </summary>
		/// <param name="v">The other vector to get the distance to.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float Distance(in Vec3 v)
		{
			float dx = v.X - X, dy = v.Y - Y, dz = v.Z - Z;
			return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
		}

		/// <summary>
		/// Gets the distance between two vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Distance(in Vec3 l, in Vec3 r)
		{
			float dx = l.X - r.X, dy = l.Y - r.Y, dz = l.Z - r.Z;
			return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
		}

		/// <summary>
		/// Gets the distance squared between this and another vector.
		/// </summary>
		/// <param name="v">The other vector to get the distance to.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float DistanceSquared(in Vec3 v)
		{
			float dx = v.X - X, dy = v.Y - Y, dz = v.Z - Z;
			return dx * dx + dy * dy + dz * dz;
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
		#endregion // Length

		#region Vector Functions
		/// <summary>
		/// Returns the vector in the same direction with a length of one.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vec3 Normalized()
		{
			float len = Mathf.Sqrt(X * X + Y * Y + Z * Z);
			return new Vec3(X / len, Y / len, Z / len);
		}

		/// <summary>
		/// Returns the vector in the same direction with a length of one.
		/// </summary>
		/// <param name="vec">The vector to normalize.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 Normalized(in Vec3 vec)
		{
			float len = Mathf.Sqrt(vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z);
			return new Vec3(vec.X / len, vec.Y / len, vec.Z / len);
		}

		/// <summary>
		/// Returns the vector in the same direction with a length of one.
		/// </summary>
		/// <param name="vec">The vector to normalize.</param>
		/// <param name="o">The normalized vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Normalized(in Vec3 vec, out Vec3 o)
		{
			float len = Mathf.Sqrt(vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z);
			o = new Vec3(vec.X / len, vec.Y / len, vec.Z / len);
		}

		/// <summary>
		/// Calculates the dot product of this vector with another vector.
		/// </summary>
		/// <param name="v">The vector to dot product with.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float Dot(in Vec3 v) => X * v.X + Y * v.Y + Z * v.Z;

		/// <summary>
		/// Calculates the dot product of the two vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Dot(in Vec3 l, in Vec3 r) => l.X * r.X + l.Y * r.Y + l.Z * r.Z;

		/// <summary>
		/// Calculates the cross product if this vector with another vector.
		/// </summary>
		/// <param name="v">The vector to cross product with.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vec3 Cross(in Vec3 v) => new Vec3(Y * v.Z - Z * v.Y, X * v.Z - Z * v.X, X * v.Y - Y * v.X);

		/// <summary>
		/// Calculates the cross product of the two vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 Cross(in Vec3 l, in Vec3 r) => new Vec3(l.Y * r.Z - l.Z * r.Y, l.X * r.Z - l.Z * r.X, l.X * r.Y - l.Y * r.X);

		/// <summary>
		/// Calculates the cross product of the two vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <param name="o">The cross product.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Cross(in Vec3 l, in Vec3 r, out Vec3 o)
		{
			o = new Vec3(l.Y * r.Z - l.Z * r.Y, l.X * r.Z - l.Z * r.X, l.X * r.Y - l.Y * r.X);
		}

		/// <summary>
		/// Projects the first vector onto the second vector.
		/// </summary>
		/// <param name="l">The vector to project.</param>
		/// <param name="r">The vector to project onto.</param>
		public static Vec3 Project(in Vec3 l, in Vec3 r)
		{
			float rlen = Mathf.Sqrt(r.X * r.X + r.Y * r.Y + r.Z * r.Z);
			Vec3 unitr = new Vec3(r.X / rlen, r.Y / rlen, r.Z / rlen);
			float a = l.X * unitr.X + l.Y * unitr.Y + l.Z * unitr.Z;
			return new Vec3(unitr.X * a, unitr.Y * a, unitr.Z * a);
		}

		/// <summary>
		/// Projects the first vector onto the second vector.
		/// </summary>
		/// <param name="l">The vector to project.</param>
		/// <param name="r">The vector to project onto.</param>
		/// <param name="o">The projected vector.</param>
		public static void Project(in Vec3 l, in Vec3 r, out Vec3 o)
		{
			float rlen = Mathf.Sqrt(r.X * r.X + r.Y * r.Y + r.Z * r.Z);
			Vec3 unitr = new Vec3(r.X / rlen, r.Y / rlen, r.Z / rlen);
			float a = l.X * unitr.X + l.Y * unitr.Y + l.Z * unitr.Z;
			o.X = unitr.X * a;
			o.Y = unitr.Y * a;
			o.Z = unitr.Z * a;
		}

		/// <summary>
		/// Calculates the angle (in radians) between the two vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		public static float AngleBetween(in Vec3 l, in Vec3 r)
		{
			float dot = l.X * r.X + l.Y * r.Y + l.Z * r.Z;
			float llen = Mathf.Sqrt(l.X * l.X + l.Y * l.Y + l.Z * l.Z);
			float rlen = Mathf.Sqrt(r.X * r.X + r.Y * r.Y + r.Z * r.Z);
			return Mathf.Acos(dot / (llen * rlen));
		}

		/// <summary>
		/// Compares two vectors to see if they are equal within a certain limit.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <param name="eps">The maximum difference between elements to be considered nearly equal.</param>
		public static bool NearlyEqual(in Vec3 l, in Vec3 r, float eps = 1e-5f)
		{
			return MathUtils.NearlyEqual(l.X, r.X, eps) && MathUtils.NearlyEqual(l.Y, r.Y, eps) &&
				MathUtils.NearlyEqual(l.Z, r.Z, eps);
		}
		#endregion // Vector Functions

		#region Operators
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Vec3 l, in Vec3 r)
		{
			return (l.X == r.X) && (l.Y == r.Y) && (l.Z == r.Z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Vec3 l, in Vec3 r)
		{
			return (l.X != r.X) || (l.Y != r.Y) || (l.Z != r.Z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 operator + (in Vec3 l, in Vec3 r)
		{
			return new Vec3(l.X + r.X, l.Y + r.Y, l.Z + r.Z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 operator - (in Vec3 l, in Vec3 r)
		{
			return new Vec3(l.X - r.X, l.Y - r.Y, l.Z - r.Z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 operator * (in Vec3 l, in Vec3 r)
		{
			return new Vec3(l.X * r.X, l.Y * r.Y, l.Z * r.Z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 operator * (in Vec3 l, float r)
		{
			return new Vec3(l.X * r, l.Y * r, l.Z * r);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 operator * (float l, in Vec3 r)
		{
			return new Vec3(l * r.X, l * r.Y, l * r.Z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 operator / (in Vec3 l, in Vec3 r)
		{
			return new Vec3(l.X / r.X, l.Y / r.Y, l.Z / r.Z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 operator / (in Vec3 l, float r)
		{
			return new Vec3(l.X / r, l.Y / r, l.Z / r);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 operator - (in Vec3 v)
		{
			return new Vec3(-v.X, -v.Y, -v.Z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Vec2 (in Vec3 v)
		{
			return new Vec2(v.X, v.Y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Vec4 (in Vec3 v)
		{
			return new Vec4(v.X, v.Y, v.Z, 0);
		}
		#endregion // Operators

		#region Min/Max
		/// <summary>
		/// Creates a new vector with the minimum components of the two input vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 Min(in Vec3 l, in Vec3 r)
		{
			return new Vec3(l.X < r.X ? l.X : r.X, l.Y < r.Y ? l.Y : r.Y, l.Z < r.Z ? l.Z : r.Z);
		}
		/// <summary>
		/// Creates a new vector with the minimum components of the two input vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <param name="p">The output value for the minimized vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Min(in Vec3 l, in Vec3 r, out Vec3 p)
		{
			p = new Vec3(l.X < r.X ? l.X : r.X, l.Y < r.Y ? l.Y : r.Y, l.Z < r.Z ? l.Z : r.Z);
		}

		/// <summary>
		/// Creates a new vector with the maximum components of the two input vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec3 Max(in Vec3 l, in Vec3 r)
		{
			return new Vec3(l.X > r.X ? l.X : r.X, l.Y > r.Y ? l.Y : r.Y, l.Z > r.Z ? l.Z : r.Z);
		}
		/// <summary>
		/// Creates a new vector with the maximum components of the two input vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <param name="p">The output value for the maximized vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Max(in Vec3 l, in Vec3 r, out Vec3 p)
		{
			p = new Vec3(l.X > r.X ? l.X : r.X, l.Y > r.Y ? l.Y : r.Y, l.Z > r.Z ? l.Z : r.Z);
		}
		#endregion // Min/Max
	}
}
