/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Runtime.InteropServices;
using Spectrum.Audio;

namespace Spectrum.Content
{
	// Loads Sounds and Songs
	[ContentLoader("AudioLoader")]
	internal class AudioLoader : ContentLoader<IAudioContent>
	{
		private static readonly Type SONG_TYPE = typeof(Song);
		private static readonly Type SOUND_TYPE = typeof(Sound);

		public unsafe override IAudioContent Load(ContentStream stream, LoaderContext ctx)
		{
			uint frameCount = stream.ReadUInt32();
			uint sampleRate = stream.ReadUInt32();
			bool isStereo = stream.ReadBoolean();
			bool isLossy = stream.ReadBoolean();

			if (ctx.ContentType == SONG_TYPE)
			{
				var astream = new RLADStream(stream.Duplicate(), isStereo, frameCount);
				return new Song(astream, sampleRate);
			}
			else if (ctx.ContentType == SOUND_TYPE)
			{
				uint rawlen = frameCount * 2 * (isStereo ? 2u : 1);
				var data = Marshal.AllocHGlobal((int)rawlen);
				var dspan = new Span<short>(data.ToPointer(), (int)rawlen);

				try
				{
					RLADStream.DecodeAll(stream, dspan, isStereo, frameCount);

					var ab = new AudioBuffer();
					ab.SetData((ReadOnlySpan<short>)dspan, isStereo ? AudioFormat.Stereo16 : AudioFormat.Mono16, sampleRate);
					return new Sound(ab);
				}
				finally
				{
					Marshal.FreeHGlobal(data);
				}
			}
			else
				throw new ContentLoadException(ctx.ItemName, "invalid content type for AudioLoader");
		}
	}

	// Dummy type used for Song and Sound so AudioLoader can manage both at once
	internal interface IAudioContent { }
}
