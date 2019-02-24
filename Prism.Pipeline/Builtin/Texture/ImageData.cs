using System;

namespace Prism.Builtin
{
	internal unsafe class ImageData
	{
		#region Fields
		public readonly uint Width;
		public readonly uint Height;
		public readonly uint Channels; // 1 = gray, 2 = gray + alpha, 3 = rgb, 4 = rgba
		public readonly byte* Data; // Raw packed data received from the native library
		#endregion // Fields

		public ImageData(uint w, uint h, uint c, byte* data)
		{
			Width = w;
			Height = h;
			Channels = c;
			Data = data;
		}
	}
}
