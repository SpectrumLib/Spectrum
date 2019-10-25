/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Describes how an image is sampled (interpolation and coordinate clamping).
	/// </summary>
	public readonly struct Sampler : IEquatable<Sampler>
	{
		/// <summary>
		/// The default linear sampler, with clamp to edge and no anisotropy.
		/// </summary>
		public static readonly Sampler Linear = new Sampler(
			TextureFilter.Linear, AddressMode.ClampToEdge, AnisotropyLevel.None, ClampBorderColor.OpaqueBlack
		);
		/// <summary>
		/// The default nearest sampler, with clamp to edge and no anisotropy.
		/// </summary>
		public static readonly Sampler Nearest = new Sampler(
			TextureFilter.Nearest, AddressMode.ClampToEdge, AnisotropyLevel.None, ClampBorderColor.OpaqueBlack
		);

		// Tracks the cached sampler objects. These which will not be created until a sampler is used in a pipeline
		//   for the first time. This is to allow users to define samplers before Vulkan is initialized, without
		//   causing errors because there is no device available to create the sampler objects. This caching is
		//   also helpful for reducing the number of possible Sampler objects, as there are limited number of
		//   permutations available.
		private static readonly Dictionary<Sampler, Vk.Sampler> _SamplerCache = new Dictionary<Sampler, Vk.Sampler>();
		internal static IReadOnlyDictionary<Sampler, Vk.Sampler> Samplers => _SamplerCache;

		#region Fields
		/// <summary>
		/// The filtering mode to sample the texture with.
		/// </summary>
		public readonly TextureFilter Filter;
		/// <summary>
		/// The addressing mode to sample the texture with.
		/// </summary>
		public readonly AddressMode AddressMode;
		/// <summary>
		/// The anisotropy to sample the texture with.
		/// </summary>
		public readonly AnisotropyLevel Anisotropy;
		/// <summary>
		/// The border color to use if sampling with <see cref="AddressMode.ClampToBorder"/>.
		/// </summary>
		public readonly ClampBorderColor BorderColor;

		// Cached hash, since Samplers are used as keys, allows fast equality checking
		private readonly int _hash;
		#endregion // Fields

		/// <summary>
		/// Creates a new texture sampler object.
		/// </summary>
		/// <param name="filter">The filtering mode.</param>
		/// <param name="addressMode">The coordinate addressing mode.</param>
		/// <param name="aniso">The anisotropic filtring level.</param>
		/// <param name="color">The border color for ClampToBorder sampling.</param>
		public Sampler
		(
			TextureFilter filter = TextureFilter.Linear,
			AddressMode addressMode = AddressMode.ClampToEdge,
			AnisotropyLevel aniso = AnisotropyLevel.None,
			ClampBorderColor color = ClampBorderColor.OpaqueBlack
		)
		{
			Filter = filter;
			AddressMode = addressMode;
			Anisotropy = aniso;
			BorderColor = color;

			// This works as long as each member has a value <= 255
			_hash = ((int)filter << 24) | ((int)addressMode << 16) | ((int)aniso << 8) | (int)color;
		}

		internal readonly Vk.Sampler GetSampler()
		{
			if (_SamplerCache.TryGetValue(this, out var vks))
				return vks;
			_SamplerCache.Add(this, vks = MakeSampler(this));
			return vks;
		}

		public readonly override string ToString() => $"{{{Filter} {AddressMode} {Anisotropy}}}";

		public readonly override int GetHashCode() => _hash;

		public readonly override bool Equals(object obj) => (obj is Sampler) && (((Sampler)obj)._hash == _hash);

		readonly bool IEquatable<Sampler>.Equals(Sampler other) => other._hash == _hash; // See ctor as to why this works

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Sampler l, in Sampler r) => l._hash == r._hash; // See ctor as to why this works

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Sampler l, in Sampler r) => l._hash != r._hash; // See ctor as to why this works

		private static Vk.Sampler MakeSampler(in Sampler samp)
		{
			var device = Core.Instance.GraphicsDevice;
			bool bad = (samp.Anisotropy != AnisotropyLevel.None && !device.Features.AnisotropicFiltering);
			var aniso = bad ? AnisotropyLevel.None : samp.Anisotropy;

			return device.VkDevice.CreateSampler(
				magFilter: (Vk.Filter)samp.Filter,
				minFilter: (Vk.Filter)samp.Filter,
				mipmapMode: Vk.SamplerMipmapMode.Linear,
				addressModeU: (Vk.SamplerAddressMode)samp.AddressMode,
				addressModeV: (Vk.SamplerAddressMode)samp.AddressMode,
				addressModeW: (Vk.SamplerAddressMode)samp.AddressMode,
				mipLodBias: 0,
				anisotropyEnable: (aniso != AnisotropyLevel.None),
				maxAnisotropy: (float)aniso,
				compareEnable: false,
				compareOp: Vk.CompareOp.Never,
				minLod: 0,
				maxLod: 0,
				borderColor: (Vk.BorderColor)samp.BorderColor,
				unnormalizedCoordinates: false,
				flags: Vk.SamplerCreateFlags.None
			);
		}
	}

	/// <summary>
	/// Describes the types of filtering used when sampling textures.
	/// </summary>
	public enum TextureFilter
	{
		/// <summary>
		/// The color from the nearest pixel to the sample is used unaltered.
		/// </summary>
		Nearest = Vk.Filter.Nearest,
		/// <summary>
		/// The average of the colors of the four nearest pixels to the sample are used.
		/// </summary>
		Linear = Vk.Filter.Linear
	}

	/// <summary>
	/// How texture coordinates outside of [0, 1] are treated when sampling textures.
	/// </summary>
	public enum AddressMode
	{
		/// <summary>
		/// The texture is repeated and tiled unaltered.
		/// </summary>
		Repeat = Vk.SamplerAddressMode.Repeat,
		/// <summary>
		/// The texture is repeated and tiled, but each new tile is the mirror image of its neighbors.
		/// </summary>
		MirrorRepeat = Vk.SamplerAddressMode.MirroredRepeat,
		/// <summary>
		/// The samples are set to the color of the nearest sample in the range [0, 1].
		/// </summary>
		ClampToEdge = Vk.SamplerAddressMode.ClampToEdge,
		/// <summary>
		/// The samples are set to a constant color.
		/// </summary>
		ClampToBorder = Vk.SamplerAddressMode.ClampToBorder
	}

	/// <summary>
	/// The level of anisotropic filtering. Any value other than <see cref="None"/> requires a feature to be
	/// active on the graphics device.
	/// </summary>
	/// <remarks>
	/// Anisotropic filtering is a relatively cheap way to make high-frequency noise, like textures seen nearly on edge,
	/// look better.
	/// </remarks>
	public enum AnisotropyLevel
	{
		/// <summary>
		/// No anisotropic filtering.
		/// </summary>
		None = 0,
		/// <summary>
		/// Two-sample anisotropic filtering.
		/// </summary>
		Two = 2,
		/// <summary>
		/// Four-sample anisotropic filtering.
		/// </summary>
		Four = 4,
		/// <summary>
		/// Eight-sample anisotropic filtering.
		/// </summary>
		Eight = 8,
		/// <summary>
		/// Sixteen-sample anisotropic filtering.
		/// </summary>
		Sixteen = 16
	}

	/// <summary>
	/// A list of the colors than can be used with the <see cref="AddressMode.ClampToBorder"/> setting.
	/// </summary>
	public enum ClampBorderColor
	{
		/// <summary>
		/// Black with 0 for the alpha channel.
		/// </summary>
		TransparentBlack = Vk.BorderColor.IntTransparentBlack,
		/// <summary>
		/// Black with 1 for the alpha channel.
		/// </summary>
		OpaqueBlack = Vk.BorderColor.IntOpaqueBlack,
		/// <summary>
		/// White with 1 for the alpha channel.
		/// </summary>
		OpaqueWhite = Vk.BorderColor.IntOpaqueWhite
	}
}
