using System;
using System.Text;

namespace Spectrum.Utility
{
	// Contains functionality for working with pointers and unmanaged arrays
	internal static class PointerUtils
	{
		// Used to search a raw byte string for a specific character
		public unsafe static uint IndexOf(byte *ptr, byte val)
		{
			uint idx = 0;
			while (*(ptr++) != val) ++idx;
			return idx;
		}

		// Used to convert a raw byte string of indeterminate length (but with a null termiantor) to a managed string
		public unsafe static string RawToString(byte *ptr, Encoding enc) => enc.GetString(ptr, (int)PointerUtils.IndexOf(ptr, 0x00));
	}
}
