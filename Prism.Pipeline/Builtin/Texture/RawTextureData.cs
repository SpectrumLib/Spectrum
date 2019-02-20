using System;

namespace Prism.Builtin
{
	internal class RawTextureData
	{
		public uint Width;
		public uint Height;
		public uint BPP; // 24 = RGB, 32 = RGBA
		public byte[] Data; // Raw RGB(A) ordered data of the size Width * Height * (BPP / 8)
	}
}
