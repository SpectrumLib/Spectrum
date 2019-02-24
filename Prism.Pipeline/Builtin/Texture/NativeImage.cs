using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace Prism.Builtin
{
	// Interfaces with the native image loading library
	internal static class NativeImage
	{
		#region Fields
		private static readonly Delegates.stbi_load stbi_load;
		private static readonly Delegates.stbi_image_free stbi_image_free;
		private static readonly Delegates.stbi_failure_reason stbi_failure_reason;
		private static readonly Delegates.stbir_resize_uint8 stbir_resize_uint8;
		private static readonly Delegates.stl_c_free stl_c_free;
		private static readonly Delegates.stl_c_alloc stl_c_alloc;
		#endregion // Fields

		static NativeImage()
		{
			var handle = Native.GetLibraryHandle("image");
			stbi_load = Native.LoadFunction<Delegates.stbi_load>(handle, nameof(stbi_load));
			stbi_image_free = Native.LoadFunction<Delegates.stbi_image_free>(handle, nameof(stbi_image_free));
			stbi_failure_reason = Native.LoadFunction<Delegates.stbi_failure_reason>(handle, nameof(stbi_failure_reason));
			stbir_resize_uint8 = Native.LoadFunction<Delegates.stbir_resize_uint8>(handle, nameof(stbir_resize_uint8));
			stl_c_free = Native.LoadFunction<Delegates.stl_c_free>(handle, nameof(stl_c_free));
			stl_c_alloc = Native.LoadFunction<Delegates.stl_c_alloc>(handle, nameof(stl_c_alloc));
		}

		public unsafe static ImageData Load(string file)
		{
			if (!File.Exists(file))
				throw new ArgumentException("The image file does not exist.", nameof(file));

			// Want to load 4 channel only for now
			var data = stbi_load(file, out int x, out int y, out int channels, 4);
			if (data == IntPtr.Zero)
				throw new InvalidOperationException($"Unable to load image file ({GetFailureReason()}).");

			return new ImageData(
				(uint)x,
				(uint)y,
				4,
				(byte*)data.ToPointer()
			);
		}

		public unsafe static byte* AllocData(ulong size) => (byte*)stl_c_alloc(size).ToPointer();

		public unsafe static byte* AllocData(uint w, uint h, uint channels) => AllocData(w * h * channels);

		// For memory allocatedc with AllocData()
		public unsafe static void Free(byte* mem) => stl_c_free(new IntPtr(mem));

		// For memory allocated with Load() or Resize()
		public unsafe static void FreeImage(byte* mem) => stbi_image_free(new IntPtr(mem));

		public static string GetFailureReason() => Marshal.PtrToStringAnsi(stbi_failure_reason());

		// Unmanaged delegate types
		private static class Delegates
		{
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr stbi_load(string file, out int x, out int y, out int channels, int desired);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void stbi_image_free(IntPtr data);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr stbi_failure_reason();
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate int stbir_resize_uint8(IntPtr ip, int iw, int ih, int istride, IntPtr op, int ow, int oh, int ostride, int channels);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate void stl_c_free(IntPtr mem);
			[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
			public delegate IntPtr stl_c_alloc(ulong size);
		}
	}
}
