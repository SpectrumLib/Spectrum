using System;

namespace Prism.Builtin
{
	// Holds size and pixel data of an image
	internal unsafe class ImageData : IDisposable
	{
		#region Fields
		public readonly uint Width;
		public readonly uint Height;
		public readonly uint Channels; // 1 = gray, 2 = gray + alpha, 3 = rgb, 4 = rgba
		public readonly byte* Data; // Raw packed data received from the native library

		private bool _isDisposed = false;
		#endregion // Fields

		public ImageData(uint w, uint h, uint c, byte* data)
		{
			Width = w;
			Height = h;
			Channels = c;
			Data = data;
		}
		~ImageData()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (!_isDisposed && (Data != (byte*)0))
				NativeImage.FreeImage(Data);
			_isDisposed = true;
		}
	}
}
