using System;
using System.Runtime.InteropServices;
using Spectrum.Audio;

namespace Spectrum.Content
{
	// Used to load sound effects and songs
	[ContentLoader("AudioLoader")]
	internal class AudioLoader : ContentLoader<SoundEffect>
	{
		public unsafe override SoundEffect Load(ContentStream stream, LoaderContext ctx)
		{
			uint frameCount = stream.ReadUInt32() - 1;
			uint sampleRate = stream.ReadUInt32();
			bool isStereo = stream.ReadBoolean();
			bool isLossy = stream.ReadBoolean();

			if (!isLossy)
				throw new NotImplementedException("Loading lossless audio data not yet implemented.");

			uint fullLen = frameCount * 2 * (isStereo ? 2u : 1u);
			var data = Marshal.AllocHGlobal((int)fullLen);

			try
			{
				short* dst = (short*)data.ToPointer();

				for (uint fi = 0; fi < frameCount; ++fi)
				{
					*(dst++) = stream.ReadInt16();
					if (isStereo)
						*(dst++) = stream.ReadInt16();
				}

				//if (isStereo)
				//{
				//	short c1_l = stream.ReadInt16();
				//	short c2_l = stream.ReadInt16();

				//	for (uint fi = 0; fi < frameCount; fi += 2)
				//	{
				//		// Read the data needed to decode the residual frame
				//		sbyte c1_d = stream.ReadSByte();
				//		sbyte c2_d = stream.ReadSByte();
				//		short c1_r = stream.ReadInt16();
				//		short c2_r = stream.ReadInt16();

				//		// Store the full frame and calculate the residual frame
				//		dst[0] = c1_l;
				//		dst[1] = c2_l;
				//		dst[2] = (short)(((c1_l + c1_r) / 2) + c1_d);
				//		dst[4] = (short)(((c2_l + c2_r) / 2) + c2_d);

				//		// Save right frames as next left frames, and move dst pointer
				//		c1_l = c1_r;
				//		c2_l = c2_r;
				//		dst += 4;
				//	}
				//}
				//else
				//{
				//	short cl = stream.ReadInt16();

				//	for (uint fi = 0; fi < frameCount; fi += 2)
				//	{
				//		// Read the data needed to decode the residual frame
				//		sbyte cd = stream.ReadSByte();
				//		short cr = stream.ReadInt16();

				//		// Store the full frame and calculate the residual frame
				//		dst[0] = cl;
				//		dst[1] = (short)(((cl + cr) / 2) + cd);

				//		// Save right frames as next left frames, and move dst pointer
				//		cl = cr;
				//		dst += 2;
				//	}
				//}

				var sb = new SoundBuffer();
				sb.SetData(data, AudioFormat.Stereo16, sampleRate, fullLen);
				return new SoundEffect(sb);
			}
			finally
			{
				Marshal.FreeHGlobal(data);
			}
		}
	}
}
