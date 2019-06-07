using System;
using System.Runtime.InteropServices;
using Vk = VulkanCore;
using Spectrum.Content;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Base class for all texture types, and cannot be instantiated directly. Types that derive from this one can only
	/// be used to upload from the CPU and sample in shaders. There is a separate type for textures to render to. All
	/// texel data to and from textures must be in R8G8B8A8 format (standard 8-bit 4-channel format)
	/// </summary>
	public abstract class Texture : IDisposableContent
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
		internal readonly Vk.ImageView VkView;

		/// <summary>
		/// The size of the pixel buffer storing this texture's data, in bytes.
		/// </summary>
		public readonly uint DataSize;

		// The handle to the graphics device for this image
		protected readonly GraphicsDevice Device;
		// The limits for the device
		protected DeviceLimits Limits => Device.Limits;
		// If the texture is disposed
		public bool IsDisposed { get; private set; } = false;
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
			if (type == TextureType.Texture3D && layers != 1)
				throw new ArgumentOutOfRangeException(nameof(layers), "3D textures cannot be arrays");
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
				Usage = Vk.ImageUsages.TransferDst | Vk.ImageUsages.Sampled | Vk.ImageUsages.TransferSrc,
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

			// Create the view
			var vci = new Vk.ImageViewCreateInfo(
				Vk.Format.R8G8B8A8UNorm,
				new Vk.ImageSubresourceRange(Vk.ImageAspects.Color, 0, 1, 0, (int)Layers),
				viewType: GetViewType(Type, layers)
			);
			VkView = VkImage.CreateView(vci);

			// Make the initial layout transition
			Device.SubmitScratchCommand(buf =>
			{
				buf.CmdPipelineBarrier(Vk.PipelineStages.AllGraphics, Vk.PipelineStages.AllGraphics, imageMemoryBarriers: new[] { new Vk.ImageMemoryBarrier(
					VkImage, new Vk.ImageSubresourceRange(Vk.ImageAspects.Color, 0, 1, 0, 1), Vk.Accesses.None, Vk.Accesses.ShaderRead,
					Vk.ImageLayout.Undefined, Vk.ImageLayout.ShaderReadOnlyOptimal
				)});
			});
		}
		~Texture()
		{
			if (!IsDisposed)
				Dispose(false);
			IsDisposed = true;
		}

		#region Set Data
		// Base function for copying data from the host into the image on the device
		// Contains all of the functionality that is needed by the different texture types for their own SetData functions
		// `start` and `length` are in array indices, not bytes.
		private protected unsafe void SetDataInternal<T>(T[] data, uint start, in TextureRegion region, uint layer, uint layerCount)
			where T : struct
		{
			uint typeSize = (uint)Marshal.SizeOf<T>();
			uint dstSize = region.TexelCount * layerCount * 4; // * 4 for R8G8B8A8UNorm format
			uint length = (uint)Mathf.Ceiling((float)dstSize / typeSize);

			if (data == null)
				throw new ArgumentNullException(nameof(data));
			if (!region.ValidFor(Type) || region.XMax > Width || region.YMax > Height || region.ZMax > Depth)
				throw new ArgumentException($"The texture region {region.ToString()} is not valid for the texture {{{Width}x{Height}x{Depth}x{Layers}}}");
			if ((data.Length - start) < length)
				throw new InvalidOperationException($"The texel source data is not large enough to supply the requested number of texels");

			uint srcLen = length * typeSize;
			uint srcOff = start * typeSize;
			if (srcLen != dstSize)
				throw new InvalidOperationException($"Mismatch between the source data length ({srcLen}) and image destination size ({dstSize})");

			var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
			try
			{
				byte* src = (byte*)handle.AddrOfPinnedObject().ToPointer();
				TransferBuffer.PushImage(src + srcOff, srcLen, Type, VkImage, region.Offset, region.Extent, layer, layerCount);
			}
			finally
			{
				handle.Free();
			}
		}

		/// <summary>
		/// Sets the data for the entire texture at once.
		/// </summary>
		/// <typeparam name="T">The type of the source texel data.</typeparam>
		/// <param name="data">The source data.</param>
		/// <param name="offset">The optional offset into the source array.</param>
		public void SetData<T>(T[] data, uint offset = 0)
			where T : struct =>
			SetDataInternal(data, offset, (0, 0, 0, Width, Height, Depth), 0, 1);

		/// <summary>
		/// Sets the data for a subset region of the texture.
		/// </summary>
		/// <typeparam name="T">The type of the source texel data.</typeparam>
		/// <param name="data">The source data.</param>
		/// <param name="region">The region of the texture to set the data in.</param>
		/// <param name="offset">The optional offset into the source array.</param>
		public void SetData<T>(T[] data, in TextureRegion region, uint offset = 0)
			where T : struct =>
			SetDataInternal(data, offset, region, 0, 1);
		#endregion // Set Data

		#region GetData
		// Base function for copying data from an image on the device to the host.
		// Contains all of the functionality that is needed by the different texture types for their own GetData functions
		// `start` and `length` are in array indices, not bytes.
		// Additionally, if a null array is passed to the function, it will create a new array of the correct size.
		private protected unsafe void GetDataInternal<T>(ref T[] data, uint start, in TextureRegion region, uint layer, uint layerCount)
			where T : struct
		{
			uint typeSize = (uint)Marshal.SizeOf<T>();
			uint srcSize = region.TexelCount * layerCount * 4; // * 4 for R8G8B8A8UNorm format
			uint length = (uint)Mathf.Ceiling((float)srcSize / typeSize);

			if (data == null)
				data = new T[length + start];

			if (!region.ValidFor(Type) || region.XMax > Width || region.YMax > Height || region.ZMax > Depth)
				throw new ArgumentException($"The texture region {region.ToString()} is not valid for the texture {{{Width}x{Height}x{Depth}x{Layers}}}");
			if ((data.Length - start) < length)
				throw new InvalidOperationException($"The texel destination array is not large enough to receive the requested number of texels");

			uint dstLen = length * typeSize;
			uint dstOff = start * typeSize;
			if (dstLen != srcSize)
				throw new InvalidOperationException($"Mismatch between the destination data length ({dstLen}) and image source size ({srcSize})");

			var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
			try
			{
				byte* dst = (byte*)handle.AddrOfPinnedObject().ToPointer();
				TransferBuffer.PullImage(dst + dstOff, dstLen, Type, VkImage, region.Offset, region.Extent, layer, layerCount);
			}
			finally
			{
				handle.Free();
			}
		}

		/// <summary>
		/// Retrieves the data of the entire texture at once into a buffer.
		/// </summary>
		/// <typeparam name="T">The type of the destination texel data.</typeparam>
		/// <param name="data">
		/// The array to place the data into, or null to have a correctly-size array created automatically. Any new
		/// array will be large enough to take into account passed offset.
		/// </param>
		/// <param name="offset">The optional offset into the destination array.</param>
		public void GetData<T>(ref T[] data, uint offset = 0)
			where T : struct =>
			GetDataInternal(ref data, offset, (0, 0, 0, Width, Height, Depth), 0, 1);

		/// <summary>
		/// Retrieves the data of a subset of the texture into a buffer.
		/// </summary>
		/// <typeparam name="T">The type of the destination texel data.</typeparam>
		/// <param name="data">
		/// The array to place the data into, or null to have a correctly-size array created automatically. Any new
		/// array will be large enough to take into account passed offset.
		/// </param>
		/// <param name="region">The subset region of the texture to pull data from.</param>
		/// <param name="offset">The optional offset into the destination array.</param>
		public void GetData<T>(ref T[] data, in TextureRegion region, uint offset = 0)
			where T : struct =>
			GetDataInternal(ref data, offset, region, 0, 1);
		#endregion // GetData

		/// <summary>
		/// Couples this texture to a sampler for use in a pipeline.
		/// </summary>
		/// <param name="sampler">The sampler to sample this texture with.</param>
		/// <returns>An object describing the coupled texture and sampler.</returns>
		public SampledTexture SampleWith(Sampler sampler) =>
			(sampler != null) ? new SampledTexture(this, sampler) : throw new ArgumentNullException(nameof(sampler));

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
				VkView?.Dispose();
				VkImage.Dispose();
				VkMemory?.Dispose();
			}
		}
		#endregion // IDisposable

		private static Vk.ImageViewType GetViewType(TextureType type, uint layers)
		{
			switch (type)
			{
				case TextureType.Texture1D: return (layers > 1) ? Vk.ImageViewType.Image1DArray : Vk.ImageViewType.Image1D;
				case TextureType.Texture2D: return (layers > 1) ? Vk.ImageViewType.Image2DArray : Vk.ImageViewType.Image2D;
				case TextureType.Texture3D: return Vk.ImageViewType.Image3D;
			}
			return Vk.ImageViewType.Image1D; // Should not be reached
		}
	}

	/// <summary>
	/// A lightweight object that couples a sampler to a texture for use in a pipeline. Created using the
	/// <see cref="Texture.SampleWith(Sampler)"/> method.
	/// </summary>
	public struct SampledTexture
	{
		#region Fields
		/// <summary>
		/// The texture the be sampled.
		/// </summary>
		public readonly Texture Texture;
		/// <summary>
		/// The rules for sampling the coupled texture.
		/// </summary>
		public readonly Sampler Sampler;
		#endregion // Fields

		internal SampledTexture(Texture tex, Sampler samp)
		{
			Texture = tex;
			Sampler = samp;
		}
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
