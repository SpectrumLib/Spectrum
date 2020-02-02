/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2020 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using Spectrum.Audio;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Spectrum.Content
{
	// Internal default type for loading audio data, either streamed as Song, or in memory as Sound
	[ContentLoader("AudioLoader", "Audio")]
	internal sealed class AudioLoader : ContentLoader<IAudioContent>
	{
		public static readonly Type TYPE = typeof(AudioLoader);
		public static readonly ContentLoaderAttribute ATTR = TYPE.GetCustomAttribute<ContentLoaderAttribute>();
		public static readonly ConstructorInfo CTOR = TYPE.GetConstructor(Type.EmptyTypes);
		private static readonly Type SONG_TYPE = typeof(Song);
		private static readonly Type SOUND_TYPE = typeof(Sound);

		public override void Reset() { }

		public unsafe override IAudioContent Load(ContentReader reader, LoaderContext ctx)
		{
			if (ctx.LoadedType == SONG_TYPE)
				return new Song(new RLADStream(reader.Duplicate()));
			else if (ctx.LoadedType == SOUND_TYPE)
			{
				RLADStream.ReadStreamHeader(reader, out var fcount, out var rate, out var fmt, out _);

				var data = Marshal.AllocHGlobal((int)(fcount * fmt.GetFrameSize()));
				var dst = new Span<short>(data.ToPointer(), (int)(fcount * fmt.GetChannelCount()));

				try
				{
					RLADStream.DecodeAll(reader, dst, fmt, fcount);

					var ab = new AudioBuffer();
					ab.SetData(dst.AsReadOnly(), fmt, rate);
					return new Sound(ab);
				}
				finally
				{
					Marshal.FreeHGlobal(data);
				}
			}
			else
				ctx.Throw("invalid runtime content type for AudioLoader");

			return null;
		}
	}

	// Dummy type to allow AudioLoader to manage Song and Sound at the same time
	internal interface IAudioContent { }
}
