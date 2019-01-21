﻿using System;
using System.Runtime.CompilerServices;
using Vk = VulkanCore;

namespace Spectrum
{
	/// <summary>
	/// Represents a 32-bit RGBA color, with 8 bits per channel.
	/// </summary>
	public struct Color : IEquatable<Color>
	{
		private const uint R_MASK_I = 0x00FFFFFF;
		private const uint G_MASK_I = 0xFF00FFFF;
		private const uint B_MASK_I = 0xFFFF00FF;
		private const uint A_MASK_I = 0xFFFFFF00;
		private const int R_SHIFT = 24;
		private const int G_SHIFT = 16;
		private const int B_SHIFT = 8;

		#region Fields
		// The backing value containing the color channels packed as a 32-bit integer as 0xRRGGBBAA
		private uint _value;

		/// <summary>
		/// The value of the red channel, in the range [0, 255].
		/// </summary>
		public byte R
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (byte)((_value >> R_SHIFT) & 0xFF);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _value = (_value & R_MASK_I) | ((uint)value << R_SHIFT);
		}
		/// <summary>
		/// The value of the green channel, in the range [0, 255].
		/// </summary>
		public byte G
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (byte)((_value >> G_SHIFT) & 0xFF);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _value = (_value & G_MASK_I) | ((uint)value << G_SHIFT);
		}
		/// <summary>
		/// The value of the blue channel, in the range [0, 255].
		/// </summary>
		public byte B
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (byte)((_value >> B_SHIFT) & 0xFF);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _value = (_value & B_MASK_I) | ((uint)value << B_SHIFT);
		}
		/// <summary>
		/// The value of the alpha channel, in the range [0, 255].
		/// </summary>
		public byte A
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (byte)(_value & 0xFF);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _value = (_value & A_MASK_I) | value;
		}

		/// <summary>
		/// The value of the red channel, in the range [0, 1].
		/// </summary>
		public float RFloat => ((_value >> R_SHIFT) & 0xFF) / 255f;
		/// <summary>
		/// The value of the green channel, in the range [0, 1].
		/// </summary>
		public float GFloat => ((_value >> G_SHIFT) & 0xFF) / 255f;
		/// <summary>
		/// The value of the blue channel, in the range [0, 1].
		/// </summary>
		public float BFloat => ((_value >> B_SHIFT) & 0xFF) / 255f;
		/// <summary>
		/// The value of the alpha channel, in the range [0, 1].
		/// </summary>
		public float AFloat => (_value & 0xFF) / 255f;
		#endregion // Fields

		#region Constructors
		/// <summary>
		/// Creates a color from a packed integer of the form 0xRRGGBBAA.
		/// </summary>
		/// <param name="packedValue">The packed channel values.</param>
		public Color(uint packedValue)
		{
			_value = packedValue;
		}

		/// <summary>
		/// Creates a color from separate channel values in the range [0, 255].
		/// </summary>
		/// <param name="r">The red channel value.</param>
		/// <param name="g">The green channel value.</param>
		/// <param name="b">The blue channel value.</param>
		/// <param name="a">The alpha channel value.</param>
		public Color(byte r, byte g, byte b, byte a = 0xFF)
		{
			_value =
				(uint)(r << R_SHIFT) |
				(uint)(g << G_SHIFT) |
				(uint)(b << B_SHIFT) |
				a;
		}

		/// <summary>
		/// Creates a color from separate channel values in the range [0, 1].
		/// </summary>
		/// <param name="r">The red channel value.</param>
		/// <param name="g">The green channel value.</param>
		/// <param name="b">The blue channel value.</param>
		/// <param name="a">The alpha channel value.</param>
		public Color(float r, float g, float b, float a = 1.0f)
		{
			uint rc = (uint)(Mathf.UnitClamp(r) * 0xFF),
				 gc = (uint)(Mathf.UnitClamp(g) * 0xFF),
				 bc = (uint)(Mathf.UnitClamp(b) * 0xFF),
				 ac = (uint)(Mathf.UnitClamp(a) * 0xFF);

			_value =
				(rc << R_SHIFT) |
				(gc << G_SHIFT) |
				(bc << B_SHIFT) |
				ac;
		}

		/// <summary>
		/// Creates a gray-scale color with identical RGB channels.
		/// </summary>
		/// <param name="val">The gray-scale value to set the RGB channels to.</param>
		/// <param name="a">The alpha channel value.</param>
		public Color(byte val, byte a = 0xFF)
		{
			_value =
				(uint)(val << R_SHIFT) |
				(uint)(val << G_SHIFT) |
				(uint)(val << B_SHIFT) |
				a;
		}

		/// <summary>
		/// Creates a gray-scale color with identical RGB channels.
		/// </summary>
		/// <param name="val">The gray-scale value to set the RGB channels to.</param>
		/// <param name="a">The alpha channel value.</param>
		public Color(float val, float a = 1.0f)
		{
			uint vc = (uint)(Mathf.UnitClamp(val) * 0xFF),
				 ac = (uint)(Mathf.UnitClamp(a) * 0xFF);

			_value =
				(vc << R_SHIFT) |
				(vc << G_SHIFT) |
				(vc << B_SHIFT) |
				ac;
		}

		/// <summary>
		/// Creates a color using existing RGB channels and a new alpha channel.
		/// </summary>
		/// <param name="c">The existing color to derive from.</param>
		/// <param name="a">The new alpha channel value.</param>
		public Color(Color c, byte a)
		{
			_value = (c._value & A_MASK_I) | a;
		}

		/// <summary>
		/// Creates a color using existing RGB channels and a new alpha channel.
		/// </summary>
		/// <param name="c">The existing color to derive from.</param>
		/// <param name="a">The new alpha channel value.</param>
		public Color(Color c, float a)
		{
			uint ac = (uint)(Mathf.UnitClamp(a) * 0xFF);
			_value = (c._value & A_MASK_I) | (ac & 0xFF);
		}
		#endregion // Constructors

		public override string ToString() => $"0x{_value:X8}";

		public override int GetHashCode() => (int)_value;

		public override bool Equals(object obj) => (obj is Color) ? (((Color)obj)._value == _value) : false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool IEquatable<Color>.Equals(Color other) => _value == other._value;

		/// <summary>
		/// Puts the floating point [0, 1] values into a new array of floats, in the order RGBA.
		/// </summary>
		/// <param name="alpha">If the alpha channel should be included in the array.</param>
		/// <returns>A new array with the channel values.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float[] ToArray(bool alpha = true) => 
			alpha ? new float[] { RFloat, GFloat, BFloat, AFloat } : new float[] { RFloat, GFloat, BFloat };

		/// <summary>
		/// Puts the floating point [0, 1] values into an existing array of floats, in the order RGBA. There is no
		/// bounds checking on the array, so ensure that it has enough space.
		/// </summary>
		/// <param name="arr">The array to put the values into.</param>
		/// <param name="alpha">If the alpha value should be written into the array.</param>
		/// <param name="off">The offset into the array to start writing at.</param>
		/// <returns>The number of elements written to the array, will always be either 3 or 4.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int ToArray(float[] arr, bool alpha = true, int off = 0)
		{
			arr[0 + off] = RFloat; arr[1 + off] = GFloat; arr[2 + off] = BFloat;
			if (alpha) { arr[3 + off] = AFloat; return 4; }
			return 3;
		}

		/// <summary>
		/// Creates a new color by reading from an array of floating point values, in the order RGBA.
		/// </summary>
		/// <param name="vals">The array of values to read from.</param>
		/// <param name="alpha">If the color should read the alpha value from the array.</param>
		/// <param name="off">The offset into the array to read from.</param>
		/// <returns>The new color, built from array values.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color FromArray(float[] vals, bool alpha = true, uint off = 0) =>
			alpha ? new Color(vals[0 + off], vals[1 + off], vals[2 + off], vals[3 + off]) : new Color(vals[0 + off], vals[1 + off], vals[2 + off]);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Color (uint packed) => new Color(packed);

		/// <summary>
		/// Converts a color into a <see cref="Vec3"/>, with the RGB values mapped to XYZ in the range [0, 1].
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Vec3 (Color c) => new Vec3(c.RFloat, c.GFloat, c.BFloat);

		/// <summary>
		/// Converts a color into a <see cref="Vec4"/>, with the RGBA values mapped to XYZW in the range [0, 1].
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Vec4 (Color c) => new Vec4(c.RFloat, c.GFloat, c.BFloat, c.AFloat);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (Color l, Color r) => l._value == r._value;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (Color l, Color r) => l._value != r._value;

		#region Predefined Colors

		#endregion // Predefined Colors
	}
}
