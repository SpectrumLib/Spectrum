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
				short* dstPtr = (short*)data.ToPointer();

				if (isStereo)
				{
					short c1_l = (short)((stream.ReadByte() | (stream.ReadByte() << 8)) & 0xFFFF),
					      c2_l = (short)((stream.ReadByte() | (stream.ReadByte() << 8)) & 0xFFFF);

					for (uint fi = 0; fi < frameCount; fi += 2, dstPtr += 4)
					{
						// Read in the residuals
						sbyte c1_d = stream.ReadSByte(),
							  c2_d = stream.ReadSByte();

						// Read in the right-hand samples
						short c1_r = (short)((stream.ReadByte() | (stream.ReadByte() << 8)) & 0xFFFF),
						      c2_r = (short)((stream.ReadByte() | (stream.ReadByte() << 8)) & 0xFFFF);

						// Store the full frame first
						dstPtr[0] = c1_l;
						dstPtr[1] = c2_l;

						// Calculate and store the residul frame second
						dstPtr[2] = (short)(((c1_l + c1_r) / 2) + c1_d);
						dstPtr[3] = (short)(((c2_l + c2_r) / 2) + c2_d);

						// Save the current right-hand samples as the new left-hand samples
						c1_l = c1_r;
						c2_l = c2_r;
					}
				}
				else
				{
					short sl = (short)((stream.ReadByte() | (stream.ReadByte() << 8)) & 0xFFFF);

					for (uint fi = 0; fi < frameCount; fi += 2, dstPtr += 2)
					{
						// Read in the residuals
						sbyte sd = stream.ReadSByte();

						// Read in the right-hand samples
						short sr = (short)((stream.ReadByte() | (stream.ReadByte() << 8)) & 0xFFFF);

						// Store the full frame first
						dstPtr[0] = sl;

						// Calculate and store the residul frame second
						dstPtr[1] = (short)(((sl + sr) / 2) + sd);

						// Save the current right-hand samples as the new left-hand samples
						sl = sr;
					}
				}

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
