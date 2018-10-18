using System;

namespace Spectrum
{
	/// <summary>
	/// Efficient mathematical object for representing rotations in 3D space.
	/// </summary>
	public struct Quaternion : IEquatable<Quaternion>
	{
		/// <summary>
		/// The identity quaternion representing no rotations.
		/// </summary>
		public static readonly Quaternion Identity = new Quaternion(0, 0, 0, 1);

		#region Fields
		/// <summary>
		/// The x-component of the rotation.
		/// </summary>
		public float X;
		/// <summary>
		/// The y-component of the rotation.
		/// </summary>
		public float Y;
		/// <summary>
		/// The z-component of the rotation.
		/// </summary>
		public float Z;
		/// <summary>
		/// The magnitude of the rotation.
		/// </summary>
		public float W;
		#endregion // Fields

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

		#region Overrides
		public bool Equals(Quaternion q)
		{
			return q.X == X && q.Y == Y && q.Z == Z && q.W == W;
		}

		public override bool Equals(object obj)
		{
			return (obj as Quaternion?)?.Equals(this) ?? false;
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

		#region Quaternion Operations
		/// <summary>
		/// Adds the quaternions component-wise.
		/// </summary>
		/// <param name="l">The first quaternion to add.</param>
		/// <param name="r">The second quaternion to add.</param>
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
		/// Multiplies two quaternion values using standard quaternion multiplication.
		/// </summary>
		/// <param name="l">The first quaternion.</param>
		/// <param name="r">The second quaternion.</param>
		public static Quaternion Multiply(in Quaternion l, in Quaternion r)
		{
			Multiply(l, r, out Quaternion o);
			return o;
		}

		/// <summary>
		/// Multiplies two quaternion values using standard quaternion multiplication.
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
		public static Quaternion Multiply(in Quaternion l, float r) => new Quaternion(l.X * r, l.Y * r, l.Z * r, l.W * r);

		/// <summary>
		/// Performs scalar multiplication of the quaternion.
		/// </summary>
		/// <param name="l">The quaternion to multiply.</param>
		/// <param name="r">The scalar to multiply.</param>
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
		public static Quaternion Divide(in Quaternion l, float r) => new Quaternion(l.X / r, l.Y / r, l.Z / r, l.W / r);

		/// <summary>
		/// Performs scalar division of the quaternion.
		/// </summary>
		/// <param name="l">The quaternion numerator.</param>
		/// <param name="r">The scalar denominator.</param>
		public static void Divide(in Quaternion l, float r, out Quaternion o)
		{
			o.X = l.X / r;
			o.Y = l.Y / r;
			o.Z = l.Z / r;
			o.W = l.W / r;
		}
		#endregion // Quaternion Operations
	}
}
