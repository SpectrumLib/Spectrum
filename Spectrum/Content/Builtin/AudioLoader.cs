using System;
using System.Runtime.InteropServices;
using Spectrum.Audio;

namespace Spectrum.Content
{
	// Used to load sound effects and songs
	[ContentLoader("AudioLoader")]
	internal class AudioLoader : ContentLoader<SoundEffect>
	{
		private const int MAX_FRAC = 127; // Max value encodable in 7 bits
		private const float MAX_FRAC_F = MAX_FRAC;

		public unsafe override SoundEffect Load(ContentStream stream, LoaderContext ctx)
		{
			uint frameCount = stream.ReadUInt32() - 1;
			uint sampleRate = stream.ReadUInt32();
			bool isStereo = stream.ReadBoolean();
			bool isLossy = stream.ReadBoolean();

			if (!isLossy)
				throw new NotImplementedException("Loading lossless audio data not yet implemented.");

			uint fullLen = frameCount * 2 * (isStereo ? 2u : 1u);
			uint chunkCount = (frameCount - 1) / 4;
			var data = Marshal.AllocHGlobal((int)fullLen);

			try
			{
				short* dstPtr = (short*)data.ToPointer();

				if (isStereo)
				{
					// Load the initial left border samples
					short c1_l = (short)(stream.ReadByte() | (stream.ReadByte() << 8)),
						  c2_l = (short)(stream.ReadByte() | (stream.ReadByte() << 8));

					for (uint ci = 0; ci < chunkCount; ++ci, dstPtr += 8)
					{
						// Write the left border samples
						dstPtr[0] = c1_l;
						dstPtr[1] = c2_l;

						// Read the fractional residuals
						byte c1_d1 = stream.ReadByte(),
							 c2_d1 = stream.ReadByte(),
							 c1_d2 = stream.ReadByte(),
							 c2_d2 = stream.ReadByte(),
							 c1_d3 = stream.ReadByte(),
							 c2_d3 = stream.ReadByte();

						// Read the right border samples
						short c1_r = (short)(stream.ReadByte() | (stream.ReadByte() << 8)),
						      c2_r = (short)(stream.ReadByte() | (stream.ReadByte() << 8));

						// Calculate the slopes, slope steps, and mid values
						int c1_s = c1_r - c1_l,
						    c2_s = c2_r - c2_l;
						ushort c1_ss = (ushort)Math.Max((c1_s < 0 ? -c1_s : c1_s) / MAX_FRAC_F, 1),
							   c2_ss = (ushort)Math.Max((c2_s < 0 ? -c2_s : c2_s) / MAX_FRAC_F, 1);
						short c1_m = (short)((c1_l + c1_r) / 2),
							  c2_m = (short)((c2_l + c2_r) / 2);

						// Decode and write the residual samples
						dstPtr[2] = (short)( c1_m + (c1_ss * ((c1_d1 & 0x80) > 0 ? -(c1_d1 & 0x7F) : (c1_d1 & 0x7F))) );
						dstPtr[3] = (short)( c2_m + (c2_ss * ((c2_d1 & 0x80) > 0 ? -(c2_d1 & 0x7F) : (c2_d1 & 0x7F))) );
						dstPtr[4] = (short)( c1_m + (c1_ss * ((c1_d2 & 0x80) > 0 ? -(c1_d2 & 0x7F) : (c1_d2 & 0x7F))) );
						dstPtr[5] = (short)( c2_m + (c2_ss * ((c2_d2 & 0x80) > 0 ? -(c2_d2 & 0x7F) : (c2_d2 & 0x7F))) );
						dstPtr[6] = (short)( c1_m + (c1_ss * ((c1_d3 & 0x80) > 0 ? -(c1_d3 & 0x7F) : (c1_d3 & 0x7F))) );
						dstPtr[7] = (short)( c2_m + (c2_ss * ((c2_d3 & 0x80) > 0 ? -(c2_d3 & 0x7F) : (c2_d3 & 0x7F))) );

						// Save the current right border samples as the new left border samples
						c1_l = c1_r;
						c2_l = c2_r;
					}

					// Write the final border samples
					dstPtr[0] = c1_l;
					dstPtr[1] = c2_l;
				}
				else
				{
					// Load the initial left border samples
					short sl = (short)(stream.ReadByte() | (stream.ReadByte() << 8));

					for (uint ci = 0; ci < chunkCount; ++ci, dstPtr += 4)
					{
						// Write the left border samples
						dstPtr[0] = sl;

						// Read the fractional residuals
						byte sd1 = stream.ReadByte(),
							 sd2 = stream.ReadByte(),
							 sd3 = stream.ReadByte();

						// Read the right border samples
						short sr = (short)(stream.ReadByte() | (stream.ReadByte() << 8));

						// Calculate the slopes, slope steps, and mid values
						int ss = sr - sl;
						ushort sss = (ushort)Math.Max((ss < 0 ? -ss : ss) / MAX_FRAC_F, 1);
						short sm = (short)((sl + sr) / 2);

						// Decode and write the residual samples
						dstPtr[1] = (short)(sm + sss * ((sd1 & 0x80) > 0 ? -(sd1 & 0x7F) : (sd1 & 0x7F)));
						dstPtr[2] = (short)(sm + sss * ((sd1 & 0x80) > 0 ? -(sd1 & 0x7F) : (sd1 & 0x7F)));
						dstPtr[3] = (short)(sm + sss * ((sd2 & 0x80) > 0 ? -(sd2 & 0x7F) : (sd2 & 0x7F)));

						// Save the current right border samples as the new left border samples
						sl = sr;
					}

					// Write the final border samples
					dstPtr[0] = sl;
				}

				var sb = new SoundBuffer();
				sb.SetData(data, isStereo ? AudioFormat.Stereo16 : AudioFormat.Mono16, sampleRate, fullLen);
				return new SoundEffect(sb);
			}
			finally
			{
				Marshal.FreeHGlobal(data);
			}
		}
	}
}
