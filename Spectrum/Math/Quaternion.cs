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

		#region Base Mathematic Operations
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
		/// Multiplies two quaternion values using standard quaternion multiplication. Equivalent to rotating first by
		/// <paramref name="l"/>, and then by <paramref name="r"/>.
		/// </summary>
		/// <param name="l">The first quaternion.</param>
		/// <param name="r">The second quaternion.</param>
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

		/// <summary>
		/// Negates the quaternion components.
		/// </summary>
		/// <param name="q">The quaternion to negate.</param>
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
		#endregion // Base Mathematic Operations

		#region Quaternion Operations
		/// <summary>
		/// Calculates the conjugate to the passed quaternion.
		/// </summary>
		/// <param name="q">The quaternion to conjugate.</param>
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
		public static float Dot(in Quaternion l, in Quaternion r) => (l.X * r.X) + (l.Y * r.Y) + (l.Z * r.Z) + (l.W * r.W);

		/// <summary>
		/// Caluclates the inverse quaterion, which represents the oppposite rotation.
		/// </summary>
		/// <param name="q">The quaternion to invert.</param>
		public static Quaternion Inverse(in Quaternion q)
		{
			Inverse(q, out Quaternion o);
			return o;
		}

		/// <summary>
		/// Caluclates the inverse quaterion, which represents the oppposite rotation.
		/// </summary>
		/// <param name="q">The quaternion to invert.</param>
		/// <param name="o">The output quaternion.</param>
		public static void Inverse(in Quaternion q, out Quaternion o)
		{
			float a = 1 / ((q.X * q.X) + (q.Y * q.Y) + (q.Z * q.Z) + (q.W * q.W));
			o.X = -q.X * a;
			o.Y = -q.Y * a;
			o.Z = -q.Z * a;
			o.W = q.W * a;
		}

		/// <summary>
		/// Gets the magnitude of the quaternion components.
		/// </summary>
		public float Length() => Mathf.Sqrt(X * X + Y * Y + Z * Z + W * W);

		/// <summary>
		/// Gets the magnitude of the quaternion components.
		/// </summary>
		/// <param name="q">The quaternion.</param>
		public static float Length(in Quaternion q) => Mathf.Sqrt(q.X * q.X + q.Y * q.Y + q.Z * q.Z + q.W * q.W);

		/// <summary>
		/// Gets the magnitude squared of the quaternion components.
		/// </summary>
		public float LengthSquared() => X * X + Y * Y + Z * Z + W * W;

		/// <summary>
		/// Gets the magnitude squared of the quaternion components.
		/// </summary>
		/// <param name="q">The quaternion.</param>
		public static float LengthSquared(in Quaternion q) => q.X * q.X + q.Y * q.Y + q.Z * q.Z + q.W * q.W;

		/// <summary>
		/// Normalizes the quaternion so the component magnitudes are one.
		/// </summary>
		/// <param name="q">The quaternion to normalize.</param>
		public static Quaternion Normalize(in Quaternion q)
		{
			Normalize(q, out Quaternion o);
			return o;
		}

		/// <summary>
		/// Normalizes the quaternion so the component magnitudes are one.
		/// </summary>
		/// <param name="q">The quaternion to normalize.</param>
		/// <param name="o">The output quaternion.</param>
		public static void Normalize(in Quaternion q, out Quaternion o)
		{
			float a = 1 / Mathf.Sqrt((q.X * q.X) + (q.Y * q.Y) + (q.Z * q.Z) + (q.W * q.W));
			o.X = q.X * a;
			o.Y = q.Y * a;
			o.Z = q.Z * a;
			o.W = q.W * a;
		}
		#endregion // Quaternion Operations

		#region Creation
		/// <summary>
		/// Creates a quaternion representing rotations in yaw, pitch, and roll.
		/// </summary>
		/// <param name="yaw">The yaw (left/right rotation around y-axis), in radians.</param>
		/// <param name="pitch">The pitch (up/down rotation around x-axis), in radians.</param>
		/// <param name="roll">The roll (rotation around z-axis), in radians.</param>
		public static Quaternion CreateFromYawPitchRoll(float yaw, float pitch, float roll)
		{
			CreateFromYawPitchRoll(yaw, pitch, roll, out Quaternion o);
			return o;
		}

		/// <summary>
		/// Creates a quaternion representing rotations in yaw, pitch, and roll.
		/// </summary>
		/// <param name="yaw">The yaw (left/right rotation around y-axis), in radians.</param>
		/// <param name="pitch">The pitch (up/down rotation around x-axis), in radians.</param>
		/// <param name="roll">The roll (rotation around z-axis), in radians.</param>
		/// <param name="o">The output quaternion.</param>
		public static void CreateFromYawPitchRoll(float yaw, float pitch, float roll, out Quaternion o)
		{
			float hy = yaw / 2, hp = pitch / 2, hr = roll / 2;

			float sy = Mathf.Sin(hy), cy = Mathf.Cos(hy);
			float sp = Mathf.Sin(hp), cp = Mathf.Cos(hp);
			float sr = Mathf.Sin(hr), cr = Mathf.Cos(hr);

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
		public static Quaternion CreateFromAxisAngle(in Vec3 axis, float a)
		{
			CreateFromAxisAngle(axis, a, out Quaternion o);
			return o;
		}

		/// <summary>
		/// Creates a quaternion representing a rotation around an axis.
		/// </summary>
		/// <param name="axis">The axis of rotation.</param>
		/// <param name="a">The rotation in radians.</param>
		/// <param name="o">The output quaternion.</param>
		public static void CreateFromAxisAngle(in Vec3 axis, float a, out Quaternion o)
		{
			float ha = a / 2;
			float s = Mathf.Sin(ha), c = Mathf.Cos(ha);
			o.X = axis.X * s;
			o.Y = axis.Y * s;
			o.Z = axis.Z * s;
			o.W = c;
		}

		/// <summary>
		/// Creates a quaternion from the rotation components of the matrix.
		/// </summary>
		/// <param name="m">The matrix extract the rotation components from.</param>
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
				sqrt = Mathf.Sqrt(scale + 1);
				half = 0.5f / sqrt;
				o.X = (m.M21 - m.M12) * half;
				o.Y = (m.M02 - m.M20) * half;
				o.Z = (m.M10 - m.M01) * half;
				o.W = sqrt / 2;
			}
			else if ((m.M00 >= m.M11) && (m.M00 >= m.M22))
			{
				sqrt = Mathf.Sqrt(1 + m.M00 - m.M11 - m.M22);
				half = 0.5f / sqrt;
				o.X = sqrt / 2;
				o.Y = (m.M10 + m.M01) * half;
				o.Z = (m.M20 + m.M02) * half;
				o.W = (m.M21 - m.M12) * half;
			}
			else if (m.M11 > m.M22)
			{
				sqrt = Mathf.Sqrt(1 + m.M11 - m.M00 - m.M22);
				half = 0.5f / sqrt;
				o.X = (m.M10 + m.M01) * half;
				o.Y = sqrt / 2;
				o.Z = (m.M21 + m.M12) * half;
				o.W = (m.M02 - m.M20) * half;
			}
			else
			{
				sqrt = Mathf.Sqrt(1 + m.M22 - m.M00 - m.M11);
				half = 0.5f / sqrt;
				o.X = (m.M02 + m.M20) * half;
				o.Y = (m.M12 + m.M21) * half;
				o.Z = sqrt / 2;
				o.W = (m.M10 - m.M01) * half;
			}
		}
		#endregion // Creation

		#region Operators
		public static bool operator == (in Quaternion l, in Quaternion r)
		{
			return (l.X == r.X) && (l.Y == r.Y) && (l.Z == r.Z) && (l.W == r.W);
		}

		public static bool operator != (in Quaternion l, in Quaternion r)
		{
			return (l.X != r.X) || (l.Y != r.Y) || (l.Z != r.Z) || (l.W != r.W);
		}

		public static Quaternion operator + (in Quaternion l, in Quaternion r)
		{
			Add(l, r, out Quaternion o);
			return o;
		}

		public static Quaternion operator - (in Quaternion l, in Quaternion r)
		{
			Subtract(l, r, out Quaternion o);
			return o;
		}

		public static Quaternion operator - (in Quaternion m)
		{
			Negate(m, out Quaternion o);
			return o;
		}

		public static Quaternion operator * (in Quaternion l, float r)
		{
			Multiply(l, r, out Quaternion o);
			return o;
		}

		public static Quaternion operator * (in Quaternion l, in Quaternion r)
		{
			Multiply(l, r, out Quaternion o);
			return o;
		}

		public static Quaternion operator / (in Quaternion l, float r)
		{
			Divide(l, r, out Quaternion o);
			return o;
		}

		public static Quaternion operator / (in Quaternion l, in Quaternion r)
		{
			Divide(l, r, out Quaternion o);
			return o;
		}

		public static explicit operator Vec4 (in Quaternion q)
		{
			return new Vec4(q.X, q.Y, q.Z, q.W);
		}
		#endregion // Operators
	}
}
