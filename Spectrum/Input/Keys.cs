using System;

namespace Spectrum.Input
{
	/// <summary>
	/// Values representing the standard keyboard keys.
	/// </summary>
	public enum Keys : byte
	{
		/// <summary>
		/// Represents a key that does not have a named mapping. Spectrum will not generate input events for this key.
		/// This key value is also sometimes used to represent an error with the keyboard input.
		/// </summary>
		Unknown = 0,

		// === Alphabetical Keys ===
		/// <summary>The alphabetical `A` key.</summary>
		A = 1,
		/// <summary>The alphabetical `B` key.</summary>
		B = 2,
		/// <summary>The alphabetical `C` key.</summary>
		C = 3,
		/// <summary>The alphabetical `D` key.</summary>
		D = 4,
		/// <summary>The alphabetical `E` key.</summary>
		E = 5,
		/// <summary>The alphabetical `F` key.</summary>
		F = 6,
		/// <summary>The alphabetical `G` key.</summary>
		G = 7,
		/// <summary>The alphabetical `H` key.</summary>
		H = 8,
		/// <summary>The alphabetical `I` key.</summary>
		I = 9,
		/// <summary>The alphabetical `J` key.</summary>
		J = 10,
		/// <summary>The alphabetical `K` key.</summary>
		K = 11,
		/// <summary>The alphabetical `L` key.</summary>
		L = 12,
		/// <summary>The alphabetical `M` key.</summary>
		M = 13,
		/// <summary>The alphabetical `N` key.</summary>
		N = 14,
		/// <summary>The alphabetical `O` key.</summary>
		O = 15,
		/// <summary>The alphabetical `P` key.</summary>
		P = 16,
		/// <summary>The alphabetical `Q` key.</summary>
		Q = 17,
		/// <summary>The alphabetical `R` key.</summary>
		R = 18,
		/// <summary>The alphabetical `S` key.</summary>
		S = 19,
		/// <summary>The alphabetical `T` key.</summary>
		T = 20,
		/// <summary>The alphabetical `U` key.</summary>
		U = 21,
		/// <summary>The alphabetical `V` key.</summary>
		V = 22,
		/// <summary>The alphabetical `W` key.</summary>
		W = 23,
		/// <summary>The alphabetical `X` key.</summary>
		X = 24,
		/// <summary>The alphabetical `Y` key.</summary>
		Y = 25,
		/// <summary>The alphabetical `Z` key.</summary>
		Z = 26,

		// === Main Keyboard Number Keys ===
		/// <summary>The main keyboard number `1` key.</summary>
		D1 = 27,
		/// <summary>The main keyboard number `2` key.</summary>
		D2 = 28,
		/// <summary>The main keyboard number `3` key.</summary>
		D3 = 29,
		/// <summary>The main keyboard number `4` key.</summary>
		D4 = 30,
		/// <summary>The main keyboard number `5` key.</summary>
		D5 = 31,
		/// <summary>The main keyboard number `6` key.</summary>
		D6 = 32,
		/// <summary>The main keyboard number `7` key.</summary>
		D7 = 33,
		/// <summary>The main keyboard number `8` key.</summary>
		D8 = 34,
		/// <summary>The main keyboard number `9` key.</summary>
		D9 = 35,
		/// <summary>The main keyboard number `0` key.</summary>
		D0 = 36,

		// === Keypad Number Keys ===
		/// <summary>The numpad number `1` key.</summary>
		KP1 = 37,
		/// <summary>The numpad number `2` key.</summary>
		KP2 = 38,
		/// <summary>The numpad number `3` key.</summary>
		KP3 = 39,
		/// <summary>The numpad number `4` key.</summary>
		KP4 = 40,
		/// <summary>The numpad number `5` key.</summary>
		KP5 = 41,
		/// <summary>The numpad number `6` key.</summary>
		KP6 = 42,
		/// <summary>The numpad number `7` key.</summary>
		KP7 = 43,
		/// <summary>The numpad number `8` key.</summary>
		KP8 = 44,
		/// <summary>The numpad number `9` key.</summary>
		KP9 = 45,
		/// <summary>The numpad number `0` key.</summary>
		KP0 = 46,

		// === Numpad Characters ===
		/// <summary>The divide key on the numpad.</summary>
		KPDivide = 47,
		/// <summary>The multiply key on the numpad.</summary>
		KPMultiply = 48,
		/// <summary>The subtract key on the numpad.</summary>
		KPSubtract = 49,
		/// <summary>The add key on the numpad.</summary>
		KPAdd = 50,
		/// <summary>The period (dot) key on the numpad.</summary>
		KPDecimal = 51,

		// === Main Keyboard Non-Alphanumeric Characters ===
		/// <summary>The grave/tilde key (` ~).</summary>
		Grave = 52,
		/// <summary>The minus/underscore key (- _).</summary>
		Minus = 53,
		/// <summary>The equals/plus key (= +).</summary>
		Equals = 54,
		/// <summary>The left brackets key ([ {).</summary>
		LeftBracket = 55,
		/// <summary>The right brackets key (] }).</summary>
		RightBracket = 56,
		/// <summary>The backslash/pipe key (\ |).</summary>
		Backslash = 57,
		/// <summary>The semicolon/colon key (; :).</summary>
		Semicolon = 58,
		/// <summary>The apostrophe/quote key (' ").</summary>
		Apostrophe = 59,
		/// <summary>The comma/less-than key (, &lt;).</summary>
		Comma = 60,
		/// <summary>The period/greater-than key (. &gt;).</summary>
		Period = 61,
		/// <summary>The slash/question mark key (/ ?).</summary>
		Slash = 62,

		// === Function Keys ===
		/// <summary>Function key 1.</summary>
		F1 = 63,
		/// <summary>Function key 2.</summary>
		F2 = 64,
		/// <summary>Function key 3.</summary>
		F3 = 65,
		/// <summary>Function key 4.</summary>
		F4 = 66,
		/// <summary>Function key 5.</summary>
		F5 = 67,
		/// <summary>Function key 6.</summary>
		F6 = 68,
		/// <summary>Function key 7.</summary>
		F7 = 69,
		/// <summary>Function key 8.</summary>
		F8 = 70,
		/// <summary>Function key 9.</summary>
		F9 = 71,
		/// <summary>Function key 10.</summary>
		F10 = 72,
		/// <summary>Function key 11.</summary>
		F11 = 73,
		/// <summary>Function key 12.</summary>
		F12 = 74,

		// === Arrow Keys ===
		/// <summary>The up arrow key.</summary>
		Up = 75,
		/// <summary>The down arrow key.</summary>
		Down = 76,
		/// <summary>The left arrow key.</summary>
		Left = 77,
		/// <summary>The right arrow key.</summary>
		Right = 78,

		// === Editing Keys ===
		/// <summary>The tab key.</summary>
		Tab = 79,
		/// <summary>The caps lock key.</summary>
		CapsLock = 80,
		/// <summary>The backspace key.</summary>
		Backspace = 81,
		/// <summary>The main keyboard enter key.</summary>
		Enter = 82,
		/// <summary>The insert key.</summary>
		Insert = 83,
		/// <summary>The home key.</summary>
		Home = 84,
		/// <summary>The page up key.</summary>
		PageUp = 85,
		/// <summary>The delete key.</summary>
		Delete = 86,
		/// <summary>The end key.</summary>
		End = 87,
		/// <summary>The page down key.</summary>
		PageDown = 88,
		/// <summary>The numpad enter key.</summary>
		KPEnter = 89,

		// === System Keys ===
		/// <summary>The escape key.</summary>
		Escape = 90,
		/// <summary>The left super (OS) key. This is the Windows key (or keyboard specific version).</summary>
		LeftSuper = 91,
		/// <summary>The right super (OS) key. This is the Windows key (or keyboard specific version).</summary>
		RightSuper = 92,
		/// <summary>The menu key (appears on some keyboards near the space bar, looks like a drop down menu.</summary>
		Menu = 93,
		/// <summary>The print screen key.</summary>
		PrintScreen = 94,
		/// <summary>The scroll lock key.</summary>
		ScrollLock = 95,
		/// <summary>The pause key.</summary>
		Pause = 96,
		/// <summary>The numlock key.</summary>
		NumLock = 97,

		// === Modifier Keys ===
		/// <summary>The left-hand shift key.</summary>
		LeftShift = 98,
		/// <summary>The left-hand ctrl key.</summary>
		LeftControl = 99,
		/// <summary>The left-hand alt key.</summary>
		LeftAlt = 100,
		/// <summary>The right-hand shift key.</summary>
		RightShift = 101,
		/// <summary>The right-hand ctrl key.</summary>
		RightControl = 102,
		/// <summary>The right-hand alt key.</summary>
		RightAlt = 103
	}

	/// <summary>
	/// Represents a mask of modifier keys (shift, control, and alt).
	/// </summary>
	public struct ModKeyMask
	{
		#region Fields
		/// <summary>
		/// The backing field that holds the bitmask.
		/// </summary>
		public readonly byte Mask;

		/// <summary>
		/// Gets if the bit for the left-hand shift key is set.
		/// </summary>
		public bool LeftShift => (Mask & 0x01) > 0;
		/// <summary>
		/// Gets if the bit for the left-hand control key is set.
		/// </summary>
		public bool LeftControl => (Mask & 0x02) > 0;
		/// <summary>
		/// Gets if the bit for the left-hand alt key is set.
		/// </summary>
		public bool LeftAlt => (Mask & 0x04) > 0;
		/// <summary>
		/// Gets if the bit for the right-hand shift key is set.
		/// </summary>
		public bool RightShift => (Mask & 0x08) > 0;
		/// <summary>
		/// Gets if the bit for the right-hand control key is set.
		/// </summary>
		public bool RightControl => (Mask & 0x10) > 0;
		/// <summary>
		/// Gets if the bit for the right-hand alt key is set.
		/// </summary>
		public bool RightAlt => (Mask & 0x20) > 0;

		/// <summary>
		/// Gets if either shift key is set in the mask.
		/// </summary>
		public bool Shift => (Mask & 0x09) > 0;
		/// <summary>
		/// Gets if either control key is set in the mask.
		/// </summary>
		public bool Control => (Mask & 0x12) > 0;
		/// <summary>
		/// Gets if either alt key is set in the mask.
		/// </summary>
		public bool Alt => (Mask & 0x24) > 0;
		#endregion // Fields

		/// <summary>
		/// Creates a new mask from an explicit mask.
		/// </summary>
		/// <param name="mask">The value to use the 8 least-significant bits for as the mask.</param>
		public ModKeyMask(int mask)
		{
			Mask = (byte)(mask & 0xFF);
		}

		/// <summary>
		/// Creates a mask with booleans representing the status of the bit for each modifier key.
		/// </summary>
		/// <param name="ls">Left-hand shift bit.</param>
		/// <param name="lc">Left-hand control bit.</param>
		/// <param name="la">Left-hand alt bit.</param>
		/// <param name="rs">Right-hand shift bit.</param>
		/// <param name="rc">Right-hand control bit.</param>
		/// <param name="ra">Right-hand alt bit.</param>
		public ModKeyMask(bool ls, bool lc, bool la, bool rs, bool rc, bool ra)
		{
			Mask = (byte)((ls ? 0x01 : 0x00) | (lc ? 0x02 : 0x00) | (la ? 0x04 : 0x00) | 
						  (rs ? 0x08 : 0x00) | (rc ? 0x10 : 0x00) | (ra ? 0x20 : 0x00));
		}

		public override bool Equals(object obj) => (obj is ModKeyMask) && ((ModKeyMask)obj).Mask == Mask;

		public override int GetHashCode() => Mask;

		public override string ToString() => $"0x{Mask:X2}";

		public static bool operator == (in ModKeyMask l, in ModKeyMask r)
		{
			return l.Mask == r.Mask;
		}

		public static bool operator != (in ModKeyMask l, in ModKeyMask r)
		{
			return l.Mask != r.Mask;
		}
	}
}
