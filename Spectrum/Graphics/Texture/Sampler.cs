using System;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Described how an image is sampled (interpolation and coordinate clamping).
	/// </summary>
	public sealed class Sampler : IDisposable
	{
		#region Fields
		private bool _isDisposed = false;
		#endregion // Fields

		~Sampler()
		{
			dispose(false);
		}

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}
		private void dispose(bool disposing)
		{
			if (!_isDisposed && disposing)
			{

			}
			_isDisposed = true;
		}
		#endregion // IDisposable
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
