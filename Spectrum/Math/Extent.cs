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
	/// Describes the size of a rectangular area with integer dimensions.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size = 2*sizeof(uint))]
	public struct Extent : IEquatable<Extent>
	{
		/// <summary>
		/// An area of zero dimensions.
		/// </summary>
		public static readonly Extent Zero = new Extent(0, 0);

		#region Fields
		/// <summary>
		/// The width of the area (x-axis dimension).
		/// </summary>
		[FieldOffset(0)]
		public uint Width;
		/// <summary>
		/// The height of the area (y-axis dimension).
		/// </summary>
		[FieldOffset(sizeof(uint))]
		public uint Height;

		/// <summary>
		/// The total area of the described dimensions.
		/// </summary>
		public readonly uint Area => Width * Height;
		#endregion // Fields

		/// <summary>
		/// Constructs a new size.
		/// </summary>
		/// <param name="w">The width of the new area.</param>
		/// <param name="h">The height of the new area.</param>
		public Extent(uint w, uint h)
		{
			Width = w;
			Height = h;
		}

		#region Overrides
		public readonly override bool Equals(object obj) => (obj is Extent) && ((Extent)obj == this);

		public readonly override int GetHashCode()
		{
			unchecked
			{
				uint hash = 17;
				hash = (hash * 23) + Width;
				hash = (hash * 23) + Height;
				return (int)hash;
			}
		}

		public readonly override string ToString() => $"{{{Width} {Height}}}";

		readonly bool IEquatable<Extent>.Equals(Extent other) =>
			(Width == other.Width) && (Height == other.Height);
		#endregion // Overrides

		#region Operators
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Extent l, in Extent r) => (l.Width == r.Width) && (l.Height == r.Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Extent l, in Extent r) => (l.Width != r.Width) || (l.Height != r.Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Extent operator * (in Extent l, uint r) => new Extent(l.Width * r, l.Height * r);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Extent operator * (uint l, in Extent r) => new Extent(l * r.Width, l * r.Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Extent operator / (in Extent l, uint r) => new Extent(l.Width / r, l.Height / r);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static explicit operator Point (in Extent e) => new Point((int)e.Width, (int)e.Height);
		#endregion // Operators

		#region Tuples
		public readonly void Deconstruct(out uint w, out uint h)
		{
			w = Width;
			h = Height;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Extent (in (uint w, uint h) tup) =>
			new Extent(tup.w, tup.h);
		#endregion // Tuples
	}
}
