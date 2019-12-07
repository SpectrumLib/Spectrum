/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;

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
		Mono8 = OpenAL.AL.FORMAT_MONO8,
		/// <summary>
		/// Dual-channel 8-bit unsigned integer PCM
		/// </summary>
		Stereo8 = OpenAL.AL.FORMAT_STEREO8,
		/// <summary>
		/// Single-channel 16-bit signed integer PCM
		/// </summary>
		Mono16 = OpenAL.AL.FORMAT_MONO16,
		/// <summary>
		/// Dual-channel 16-bit signed integer PCM
		/// </summary>
		Stereo16 = OpenAL.AL.FORMAT_STEREO16,
		/// <summary>
		/// Single-channel single-precision floating point PCM
		/// </summary>
		MonoFloat = OpenAL.Ext.FORMAT_MONO_FLOAT32,
		/// <summary>
		/// Dual-channel single-precision floating point PCM
		/// </summary>
		StereoFloat = OpenAL.Ext.FORMAT_STEREO_FLOAT32
	}

	/// <summary>
	/// Contains utility functions for working with <see cref="AudioFormat"/> values.
	/// </summary>
	public static class AudioFormatExtensions
	{
		/// <summary>
		/// The size of a single format sample, in bytes.
		/// </summary>
		/// <param name="fmt">The format to get the sample size of.</param>
		/// <returns>The format sample size.</returns>
		public static uint GetSampleSize(this AudioFormat fmt) => fmt switch {
			AudioFormat.Mono8 => 1,
			AudioFormat.Stereo8 => 1,
			AudioFormat.Mono16 => 2,
			AudioFormat.Stereo16 => 2,
			AudioFormat.MonoFloat => 4,
			AudioFormat.StereoFloat => 4,
			_ => throw new ArgumentException("Bad format enum.")
		};

		/// <summary>
		/// The number of channels in the format.
		/// </summary>
		/// <param name="fmt">The format to get the channel count of.</param>
		/// <returns>The format channel count.</returns>
		public static uint GetChannelCount(this AudioFormat fmt) => fmt switch {
			AudioFormat.Mono8 => 1,
			AudioFormat.Stereo8 => 2,
			AudioFormat.Mono16 => 1,
			AudioFormat.Stereo16 => 2,
			AudioFormat.MonoFloat => 1,
			AudioFormat.StereoFloat => 2,
			_ => throw new ArgumentException("Bad format enum.")
		};
	}

	/// <summary>
	/// Represents the playback states that audio objects can have.
	/// </summary>
	public enum SoundState
	{
		/// <summary>
		/// The audio is not playing, and playback will start at the beginning. Will also represent audio that has
		/// never been played
		/// </summary>
		Stopped,
		/// <summary>
		/// The audio is not playing, and playback will start where it was last halted.
		/// </summary>
		Paused,
		/// <summary>
		/// The audio is currently playing.
		/// </summary>
		Playing
	}
}
