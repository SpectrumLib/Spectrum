using System;
using Vk = VulkanCore;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Base class for all texture types, and cannot be instantiated directly. Types that derive from this one can only
	/// be used to upload from the CPU and sample in shaders. There is a separate type for textures to render to.
	/// </summary>
	public abstract class Texture : IDisposable
	{
		#region Fields
		/// <summary>
		/// The type of the texture (dimensionality).
		/// </summary>
		public readonly TextureType Type;

		/// <summary>
		/// The width (x-size) of the texture.
		/// </summary>
		public readonly uint Width;
		/// <summary>
		/// The width (y-size) of the texture. Will always be 1 for 1D textures.
		/// </summary>
		public readonly uint Height;
		/// <summary>
		/// The depth (z-size) of the texture. Will always be 1 for 1D and 2D textures.
		/// </summary>
		public readonly uint Depth;
		/// <summary>
		/// The number of layers in the texture array. Will always be 1 for non-array textures.
		/// </summary>
		public readonly uint Layers;

		// The vulkan objects
		internal readonly Vk.Image VkImage;
		internal readonly Vk.DeviceMemory VkMemory;

		/// <summary>
		/// The size of the pixel buffer storing this texture's data, in bytes.
		/// </summary>
		public readonly uint DataSize;

		// The handle to the graphics device for this image
		protected readonly GraphicsDevice Device;
		// The limits for the device
		protected DeviceLimits Limits => Device.Limits;
		// If the texture is disposed
		protected bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		private protected Texture(TextureType type, uint w, uint h, uint d, uint layers)
		{
			Device = SpectrumApp.Instance.GraphicsDevice;

			Type = type;
			Width = w;
			Height = h;
			Depth = d;
			Layers = layers;

			// Limits checking
			if (w == 0)
				throw new ArgumentOutOfRangeException(nameof(w), "A texture cannot have a width of zero");
			if (h == 0)
				throw new ArgumentOutOfRangeException(nameof(h), "A texture cannot have a height of zero");
			if (d == 0)
				throw new ArgumentOutOfRangeException(nameof(d), "A texture cannot have a depth of zero");
			if (layers == 0)
				throw new ArgumentOutOfRangeException(nameof(layers), "A texture cannot have 0 layers");
			if (layers > Limits.MaxTextureLayers)
				throw new ArgumentOutOfRangeException(nameof(layers), $"The texture array count ({layers}) is too big for the device ({Limits.MaxTextureLayers})");
			switch (type)
			{
				case TextureType.Texture1D:
					if (w > Limits.MaxTextureSize1D) throw new ArgumentOutOfRangeException(
						nameof(w), $"The 1D texture size ({w}) is too big for the device ({Limits.MaxTextureSize1D})"
					);
					break;
				case TextureType.Texture2D:
					if (w > Limits.MaxTextureSize2D || h > Limits.MaxTextureSize2D) throw new ArgumentOutOfRangeException(
						nameof(w), $"The 2D texture size ({w}x{h}) is too big for the device ({Limits.MaxTextureSize2D})"
					);
					break;
				case TextureType.Texture3D:
					if (w > Limits.MaxTextureSize3D || h > Limits.MaxTextureSize3D || d > Limits.MaxTextureSize3D) throw new ArgumentOutOfRangeException(
						nameof(w), $"The 3D texture size ({w}x{h}x{d}) is too big for the device ({Limits.MaxTextureSize3D})"
					);
					break;
			}

			// Create the image
			var ici = new Vk.ImageCreateInfo {
				ImageType = (Vk.ImageType)type,
				Extent = new Vk.Extent3D((int)w, (int)h, (int)d),
				MipLevels = 1,
				ArrayLayers = (int)layers,
				Format = Vk.Format.R8G8B8A8UNorm,
				Tiling = Vk.ImageTiling.Optimal,
				InitialLayout = Vk.ImageLayout.Undefined,
				Usage = Vk.ImageUsages.TransferDst | Vk.ImageUsages.Sampled,
				SharingMode = Vk.SharingMode.Exclusive,
				Samples = Vk.SampleCounts.Count1,
				Flags = Vk.ImageCreateFlags.None
			};
			VkImage = Device.VkDevice.CreateImage(ici);

			// Create the backing memory for the image
			var memReq = VkImage.GetMemoryRequirements();
			var memIdx = Device.FindMemoryTypeIndex(memReq.MemoryTypeBits, Vk.MemoryProperties.DeviceLocal);
			if (memIdx == -1)
				throw new InvalidOperationException("Cannot find a memory type that supports textures (this means bad or out-of-date hardware)");
			var mai = new Vk.MemoryAllocateInfo(memReq.Size, memIdx);
			VkMemory = Device.VkDevice.AllocateMemory(mai);
			DataSize = (uint)memReq.Size;
			VkImage.BindMemory(VkMemory);
		}
		~Texture()
		{
			if (!IsDisposed)
				Dispose(false);
			IsDisposed = true;
		}

		#region IDisposable
		public void Dispose()
		{
			if (!IsDisposed)
				Dispose(true);
			IsDisposed = true;
			GC.SuppressFinalize(this);
		}
		// ALWAYS call base.Dispose(disposing)
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				VkImage.Dispose();
				VkMemory?.Dispose();
			}
		}
		#endregion // IDisposable
	}

	/// <summary>
	/// The different types of textures available (described dimensionality of textures).
	/// </summary>
	public enum TextureType
	{
		/// <summary>
		/// The texture has data in one dimension.
		/// </summary>
		Texture1D = Vk.ImageType.Image1D,
		/// <summary>
		/// The texture has data in two dimensions.
		/// </summary>
		Texture2D = Vk.ImageType.Image2D,
		/// <summary>
		/// The texture has data in three dimensions.
		/// </summary>
		Texture3D = Vk.ImageType.Image3D
	}
}
