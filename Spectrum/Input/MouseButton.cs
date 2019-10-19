/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;

namespace Spectrum.Input
{
	/// <summary>
	/// Values representing buttons on the mouse.
	/// </summary>
	public enum MouseButton : byte
	{
		/// <summary>
		/// The left mouse button.
		/// </summary>
		Left = Glfw3.MOUSE_BUTTON_1,
		/// <summary>
		/// The right mouse button.
		/// </summary>
		Right = Glfw3.MOUSE_BUTTON_2,
		/// <summary>
		/// The middle mouse button (scroll wheel).
		/// </summary>
		Middle = Glfw3.MOUSE_BUTTON_3,
		/// <summary>
		/// First extra mouse button.
		/// </summary>
		X1 = Glfw3.MOUSE_BUTTON_4,
		/// <summary>
		/// Second extra mouse button.
		/// </summary>
		X2 = Glfw3.MOUSE_BUTTON_5,
		/// <summary>
		/// Third extra mouse button.
		/// </summary>
		X3 = Glfw3.MOUSE_BUTTON_6,
		/// <summary>
		/// Fourth extra mouse button.
		/// </summary>
		X4 = Glfw3.MOUSE_BUTTON_7,
		/// <summary>
		/// Fifth extra mouse button.
		/// </summary>
		X5 = Glfw3.MOUSE_BUTTON_8
	}

	/// <summary>
	/// Represents a mask of mouse button values.
	/// </summary>
	public struct MouseButtonMask
	{
		/// <summary>
		/// A mask representing no buttons.
		/// </summary>
		public static readonly MouseButtonMask None = new MouseButtonMask(0);
		/// <summary>
		/// A mask representing the primary (left, middle, right) buttons.
		/// </summary>
		public static readonly MouseButtonMask Primary = new MouseButtonMask(0x07);
		/// <summary>
		/// A mask representing the extra buttons.
		/// </summary>
		public static readonly MouseButtonMask Extra = new MouseButtonMask(0xF8);
		/// <summary>
		/// A mask representing all mouse buttons.
		/// </summary>
		public static readonly MouseButtonMask All = new MouseButtonMask(0xFF);

		#region Fields
		/// <summary>
		/// The backing field of bits that represent the mask.
		/// </summary>
		public readonly byte Mask;

		/// <summary>
		/// Gets if the bit for <see cref="MouseButton.Left"/> is set.
		/// </summary>
		public readonly bool Left => (Mask & (0x01 << (byte)MouseButton.Left)) > 0;
		/// <summary>
		/// Gets if the bit for <see cref="MouseButton.Middle"/> is set.
		/// </summary>
		public readonly bool Middle => (Mask & (0x01 << (byte)MouseButton.Middle)) > 0;
		/// <summary>
		/// Gets if the bit for <see cref="MouseButton.Right"/> is set.
		/// </summary>
		public readonly bool Right => (Mask & (0x01 << (byte)MouseButton.Right)) > 0;
		/// <summary>
		/// Gets if the bit for <see cref="MouseButton.X1"/> is set.
		/// </summary>
		public readonly bool X1 => (Mask & (0x01 << (byte)MouseButton.X1)) > 0;
		/// <summary>
		/// Gets if the bit for <see cref="MouseButton.X2"/> is set.
		/// </summary>
		public readonly bool X2 => (Mask & (0x01 << (byte)MouseButton.X2)) > 0;
		/// <summary>
		/// Gets if the bit for <see cref="MouseButton.X3"/> is set.
		/// </summary>
		public readonly bool X3 => (Mask & (0x01 << (byte)MouseButton.X3)) > 0;
		/// <summary>
		/// Gets if the bit for <see cref="MouseButton.X4"/> is set.
		/// </summary>
		public readonly bool X4 => (Mask & (0x01 << (byte)MouseButton.X4)) > 0;
		/// <summary>
		/// Gets if the bit for <see cref="MouseButton.X5"/> is set.
		/// </summary>
		public readonly bool X5 => (Mask & (0x01 << (byte)MouseButton.X5)) > 0;
		#endregion // Fields

		/// <summary>
		/// Creates a new mask with the passed button bits set.
		/// </summary>
		/// <param name="buttons">The button bits to set.</param>
		public MouseButtonMask(params MouseButton[] buttons)
		{
			Mask = 0;
			foreach (var mb in buttons)
				Mask |= (byte)(0x01 << (byte)mb);
		}

		/// <summary>
		/// Creates a new mask with an explicit mask.
		/// </summary>
		/// <param name="mask">The mask, with only the first 8 bits used.</param>
		public MouseButtonMask(int mask)
		{
			Mask = (byte)(mask & 0xFF);
		}

		/// <summary>
		/// Returns a new mask like this mask with the passed mouse button bit set.
		/// </summary>
		/// <param name="mb">The mouse button bit to set.</param>
		public MouseButtonMask SetButton(MouseButton mb) => new MouseButtonMask(Mask | (byte)(0x01 << (byte)mb));

		/// <summary>
		/// Returns a new mask like this mask with the passed mouse button bit cleared.
		/// </summary>
		/// <param name="mb">The mouse button bit to clear.</param>
		public MouseButtonMask ClearButton(MouseButton mb) => new MouseButtonMask(Mask & (byte)(~(0x01 << (byte)mb)));

		/// <summary>
		/// Gets the bit for the passed button.
		/// </summary>
		/// <param name="mb">The button bit to check.</param>
		public readonly bool GetButton(MouseButton mb) => (Mask & (0x01 << (byte)mb)) > 0;

		public readonly override bool Equals(object obj) => (obj is MouseButtonMask) && ((MouseButtonMask)obj).Mask == Mask;

		public readonly override int GetHashCode() => Mask;

		public readonly override string ToString() => $"0x{Mask:X2}";

		/// <summary>
		/// Creates the bit-wise AND of the two masks.
		/// </summary>
		public static MouseButtonMask operator & (in MouseButtonMask l, in MouseButtonMask r) => 
			new MouseButtonMask(l.Mask & r.Mask);

		/// <summary>
		/// Creates the bit-wise OR of the two masks.
		/// </summary>
		public static MouseButtonMask operator | (in MouseButtonMask l, in MouseButtonMask r) => 
			new MouseButtonMask(l.Mask | r.Mask);

		public static bool operator == (in MouseButtonMask l, in MouseButtonMask r) => l.Mask == r.Mask;

		public static bool operator != (in MouseButtonMask l, in MouseButtonMask r) => l.Mask != r.Mask;
	}

	/// <summary>
	/// Utility functionality for working with <see cref="MouseButton"/> values.
	/// </summary>
	public static class MouseButtonUtils
	{
		internal const int MAX_BUTTON_INDEX = (int)MouseButton.X5;
		private static readonly string[] _Names = { "Left", "Middle", "Right", "X1", "X2", "X3", "X4", "X5" };

		/// <summary>
		/// Returns a standard name for the button, in English.
		/// </summary>
		/// <param name="mb">The mouse button to get the name for.</param>
		public static string Name(this MouseButton mb) => _Names[(int)mb];

		// Translate a glfw mouse button to the enum
		internal static MouseButton Translate(int button) => (MouseButton)(byte)(button & 0xFF);
	}
}
