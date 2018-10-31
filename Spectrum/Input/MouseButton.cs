using System;

namespace Spectrum.Input
{
	// TODO: Support more mouse buttons in the future, GLFW supports 5 extra buttons

	/// <summary>
	/// Values representing buttons on the mouse.
	/// </summary>
	public enum MouseButton : byte
	{
		/// <summary>
		/// The left mouse button.
		/// </summary>
		Left = 0,
		/// <summary>
		/// The middle mouse button (scroll wheel).
		/// </summary>
		Middle = 1,
		/// <summary>
		/// The right mouse button.
		/// </summary>
		Right = 2,
		/// <summary>
		/// First extra mouse button.
		/// </summary>
		X1 = 3,
		/// <summary>
		/// Second extra mouse button.
		/// </summary>
		X2 = 4
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
		/// A mask representing the extra (X1, X2) buttons.
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
		public bool Left => (Mask & (0x01 << (byte)MouseButton.Left)) > 0;
		/// <summary>
		/// Gets if the bit for <see cref="MouseButton.Middle"/> is set.
		/// </summary>
		public bool Middle => (Mask & (0x01 << (byte)MouseButton.Middle)) > 0;
		/// <summary>
		/// Gets if the bit for <see cref="MouseButton.Right"/> is set.
		/// </summary>
		public bool Right => (Mask & (0x01 << (byte)MouseButton.Right)) > 0;
		/// <summary>
		/// Gets if the bit for <see cref="MouseButton.X1"/> is set.
		/// </summary>
		public bool X1 => (Mask & (0x01 << (byte)MouseButton.X1)) > 0;
		/// <summary>
		/// Gets if the bit for <see cref="MouseButton.X2"/> is set.
		/// </summary>
		public bool X2 => (Mask & (0x01 << (byte)MouseButton.X2)) > 0;
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

		public override bool Equals(object obj) => (obj is MouseButtonMask) && ((MouseButtonMask)obj).Mask == Mask;

		public override int GetHashCode() => Mask;

		public override string ToString() => $"0x{Mask:X2}";

		/// <summary>
		/// Creates the bit-wise AND of the two masks.
		/// </summary>
		public static MouseButtonMask operator & (in MouseButtonMask l, in MouseButtonMask r)
		{
			return new MouseButtonMask(l.Mask & r.Mask);
		}

		/// <summary>
		/// Creates the bit-wise OR of the two masks.
		/// </summary>
		public static MouseButtonMask operator | (in MouseButtonMask l, in MouseButtonMask r)
		{
			return new MouseButtonMask(l.Mask | r.Mask);
		}

		public static bool operator == (in MouseButtonMask l, in MouseButtonMask r)
		{
			return l.Mask == r.Mask;
		}

		public static bool operator != (in MouseButtonMask l, in MouseButtonMask r)
		{
			return l.Mask != r.Mask;
		}
	}

	/// <summary>
	/// Utility functionality for working with <see cref="MouseButton"/> values.
	/// </summary>
	public static class MouseButtonUtils
	{
		private static readonly string[] mbNames = { "Left", "Middle", "Right", "X1", "X2" };

		/// <summary>
		/// Returns a standard name for the button, in English.
		/// </summary>
		/// <param name="mb">The mouse button to get the name for.</param>
		public static string Name(this MouseButton mb) => mbNames[(int)mb];
	}
}
