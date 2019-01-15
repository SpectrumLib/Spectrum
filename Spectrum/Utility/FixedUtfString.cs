using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Spectrum.Utility
{
	// A small utility class for holding raw UTF-8 string data in fixed memory. This is mostly used for interfaces to
	// native libraries, which often require string information to be passed as a pointer to fixed memory.
	// Note: Only use this in rare cases, System.String will be a better choice 99.9% of the time.
	internal unsafe sealed class FixedUtfString : IDisposable
	{
		#region Fields
		private readonly GCHandle _handle;
		private readonly uint _byteCount;

		public byte* Data => (byte*)_handle.AddrOfPinnedObject().ToPointer();
		#endregion // Fields

		public FixedUtfString(string s)
		{
			if (s == null)
				throw new ArgumentNullException(nameof(s));

			_byteCount = (uint)Encoding.UTF8.GetByteCount(s);
			byte[] rawBytes = new byte[_byteCount + 1];
			_handle = GCHandle.Alloc(rawBytes, GCHandleType.Pinned);
			Encoding.UTF8.GetBytes(s, 0, s.Length, rawBytes, 0);
		}
		~FixedUtfString()
		{
			_handle.Free();
		}

		public override string ToString() => Encoding.UTF8.GetString(Data, (int)_byteCount);
		public IntPtr AsIntPtr() => _handle.AddrOfPinnedObject();

		public static implicit operator FixedUtfString (string s) => new FixedUtfString(s);
		public static implicit operator string (FixedUtfString s) => s.ToString();
		public static implicit operator IntPtr (FixedUtfString s) => s.AsIntPtr();

		public void Dispose()
		{
			_handle.Free();
			GC.SuppressFinalize(this);
		}
	}
}
