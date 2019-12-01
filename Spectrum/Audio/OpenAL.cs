/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using static Spectrum.InternalLog;

namespace Spectrum.Audio
{
	internal sealed partial class OpenAL : IDisposable
	{
		#region Fields
		private IntPtr _library = IntPtr.Zero;
		#endregion // Fields

		public OpenAL()
		{
			_library = Native.NativeLoader.LoadLibrary("openal", "libopenal.so.1",
				(lib, @new, time) => IINFO($"Loaded {(@new ? "new" : "existing")} native library '{lib}' in {time.TotalMilliseconds:.000}ms."));
		}
		~OpenAL()
		{
			Dispose();
		}

		#region IDisposable
		public void Dispose()
		{
			if (_library != IntPtr.Zero)
			{
				Native.NativeLoader.FreeLibrary(_library);
				_library = IntPtr.Zero;
			}
		}
		#endregion // IDisposable
	}
}
