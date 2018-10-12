using System;
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
		/// <summary>
		/// The vector with both components as zero.
		/// </summary>
		public static readonly Vec2 Zero = new Vec2(0, 0);
		/// <summary>
		/// A unit vector along the positive x-axis.
		/// </summary>
		public static readonly Vec2 UnitX = new Vec2(1, 0);
		/// <summary>
		/// A unit vector along the positive y-axis.
		/// </summary>
		public static readonly Vec2 UnitY = new Vec2(0, 1);
		/// <summary>
		/// The vector with both components as one.
		/// </summary>
		public static readonly Vec2 One = new Vec2(1, 1);

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
		public float Length() => (float)Math.Sqrt(X * X + Y * Y);

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
		public static float Length(in Vec2 vec) => (float)Math.Sqrt(vec.X * vec.X + vec.Y * vec.Y);

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
		public float DistanceTo(in Vec2 v)
		{
			float dx = v.X - X, dy = v.Y - Y;
			return (float)Math.Sqrt(dx * dx + dy * dy);
		}

		/// <summary>
		/// Gets the distance between two vectors.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float DistanceTo(in Vec2 l, ref Vec2 r)
		{
			float dx = l.X - r.X, dy = l.Y - r.Y;
			return (float)Math.Sqrt(dx * dx + dy * dy);
		}
		#endregion // Length

		#region Vector Functions
		/// <summary>
		/// Returns the vector in the same direction with a length of one.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vec2 Normalized()
		{
			float len = Length();
			return new Vec2(X / len, Y / len);
		}

		/// <summary>
		/// Returns the vector in the same direction with a length of one.
		/// </summary>
		/// <param name="vec">The vector to normalize.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec2 Normalized(in Vec2 vec)
		{
			float len = vec.Length();
			return new Vec2(vec.X / len, vec.Y / len);
		}

		/// <summary>
		/// Returns the vector in the same direction with a length of one.
		/// </summary>
		/// <param name="vec">The vector to normalize.</param>
		/// <param name="o">The normalized vector.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Normalized(in Vec2 vec, out Vec2 o)
		{
			float len = vec.Length();
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
			float rlen = (float)Math.Sqrt(r.X * r.X + r.Y * r.Y);
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
			float rlen = (float)Math.Sqrt(r.X * r.X + r.Y * r.Y);
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
			float llen = (float)Math.Sqrt(l.X * l.X + l.Y * l.Y);
			float rlen = (float)Math.Sqrt(r.X * r.X + r.Y * r.Y);
			return (float)Math.Acos(dot / (llen * rlen));
		}

		/// <summary>
		/// Compares two vectors to see if they are equal within a certain limit.
		/// </summary>
		/// <param name="l">The first vector.</param>
		/// <param name="r">The second vector.</param>
		/// <param name="eps">The maximum difference between elements to be considered nearly equal.</param>
		public static bool NearlyEqual(in Vec2 l, in Vec2 r, float eps = 1e-5f)
		{
			return Mathf.NearlyEqual(l.X, r.X, eps) && Mathf.NearlyEqual(l.Y, r.Y, eps);
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
