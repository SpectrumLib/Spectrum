using System;
using OpenAL;

namespace Spectrum.Audio
{
	/// <summary>
	/// Represents the different audio data formats supported by Spectrum.
	/// </summary>
	public enum AudioFormat
	{
		/// <summary>
		/// Single-channel 8-bit unsigned integer PCM
		/// </summary>
		Mono8 = AL10.AL_FORMAT_MONO8,
		/// <summary>
		/// Dual-channel 8-bit unsigned integer PCM
		/// </summary>
		Stereo8 = AL10.AL_FORMAT_STEREO8,
		/// <summary>
		/// Single-channel 16-bit signed integer PCM
		/// </summary>
		Mono16 = AL10.AL_FORMAT_MONO16,
		/// <summary>
		/// Dual-channel 16-bit signed integer PCM
		/// </summary>
		Stereo16 = AL10.AL_FORMAT_STEREO16,
		/// <summary>
		/// Single-channel single-precision floating point PCM
		/// </summary>
		MonoFloat = ALEXT.AL_FORMAT_MONO_FLOAT32,
		/// <summary>
		/// Dual-channel single-precision floating point PCM
		/// </summary>
		StereoFloat = ALEXT.AL_FORMAT_STEREO_FLOAT32
	}
}
