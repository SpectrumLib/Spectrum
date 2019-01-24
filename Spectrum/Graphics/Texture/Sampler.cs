using System;
using Vk = VulkanCore;
using static Spectrum.InternalLog;
using System.Collections.Generic;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Describes how an image is sampled (interpolation and coordinate clamping).
	/// </summary>
	public struct Sampler : IEquatable<Sampler>
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
		private static readonly Dictionary<Sampler, Vk.Sampler> s_samplerCache = new Dictionary<Sampler, Vk.Sampler>();
		internal static IReadOnlyDictionary<Sampler, Vk.Sampler> Samplers => s_samplerCache;

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

			unchecked
			{
				int hash = 14461 * (5051 + filter.GetHashCode());
				hash *= (5051 + addressMode.GetHashCode());
				hash *= (5051 + aniso.GetHashCode());
				_hash = hash * (5051 + color.GetHashCode()); 
			}
		}

		internal Vk.Sampler GetSampler()
		{
			if (!s_samplerCache.ContainsKey(this))
				s_samplerCache.Add(this, MakeSampler(this));
			return s_samplerCache[this];
		}

		public override string ToString() => $"{{F:{Filter} M:{AddressMode} A:{Anisotropy}}}";

		public override int GetHashCode() => _hash;

		public override bool Equals(object obj) => (obj is Sampler) && (((Sampler)obj) == this);

		bool IEquatable<Sampler>.Equals(Sampler other) =>
			(other.Filter == Filter) && (other.AddressMode == AddressMode) && (other.Anisotropy == Anisotropy) && 
			(other.BorderColor == BorderColor);
		
		public static bool operator == (in Sampler l, in Sampler r) =>
			(l.Filter == r.Filter) && (l.AddressMode == r.AddressMode) && (l.Anisotropy == r.Anisotropy) &&
			(l.BorderColor == r.BorderColor);

		public static bool operator != (in Sampler l, in Sampler r) =>
			(l.Filter != r.Filter) || (l.AddressMode != r.AddressMode) || (l.Anisotropy != r.Anisotropy) ||
			(l.BorderColor != r.BorderColor);

		private static Vk.Sampler MakeSampler(in Sampler samp)
		{
			var device = SpectrumApp.Instance.GraphicsDevice;
			bool bad = (samp.Anisotropy != AnisotropyLevel.None && !device.Features.AnisotropicFiltering);
			if (bad)
				LWARN($"Attempted to create a sampler with an unsupported anisotropy level.");
			var aniso = bad ? AnisotropyLevel.None : samp.Anisotropy;

			var sci = new Vk.SamplerCreateInfo {
				MagFilter = (Vk.Filter)samp.Filter,
				MinFilter = (Vk.Filter)samp.Filter,
				AddressModeU = (Vk.SamplerAddressMode)samp.AddressMode,
				AddressModeV = (Vk.SamplerAddressMode)samp.AddressMode,
				AddressModeW = (Vk.SamplerAddressMode)samp.AddressMode,
				AnisotropyEnable = (aniso != AnisotropyLevel.None),
				MaxAnisotropy = (float)aniso,
				BorderColor = (Vk.BorderColor)samp.BorderColor,
				UnnormalizedCoordinates = false,
				CompareEnable = false,
				MipmapMode = Vk.SamplerMipmapMode.Linear,
				MipLodBias = 0,
				MinLod = 0,
				MaxLod = 0
			};
			return device.VkDevice.CreateSampler(sci);
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
