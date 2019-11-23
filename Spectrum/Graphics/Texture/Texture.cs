/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2018-2019 The Spectrum Team
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */
using System;
using Vk = SharpVk;

namespace Spectrum.Graphics
{
	/// <summary>
	/// Base type for all textures that store data in GPU memory. Implements common functionality, but cannot be
	/// instantiated directly.
	/// </summary>
	public abstract class Texture : IDisposable
	{
		#region Fields
		/// <summary>
		/// The dimensionality of the texture.
		/// </summary>
		public readonly TextureType Type;

		/// <summary>
		/// The dimensions of the texture.
		/// </summary>
		public readonly (
			uint Width,
			uint Height,
			uint Depth,
			uint Layers
			) Dimensions;

		// The vulkan objects
		internal readonly Vk.Image VkImage;
		internal readonly Vk.DeviceMemory VkMemory;
		internal readonly Vk.ImageView VkView;

		/// <summary>
		/// The size of the texture storing this texture's data, in bytes.
		/// </summary>
		public readonly uint DataSize;

		// If the texture is disposed
		protected bool IsDisposed { get; private set; } = false;
		#endregion // Fields

		private protected Texture(TextureType type, in (uint w, uint h, uint d, uint l) size)
		{
			var dev = Core.Instance.GraphicsDevice;

			CheckSize(type, size);
			Type = type;
			Dimensions = size;

			// Create the image and view
			VkImage = dev.VkDevice.CreateImage(
				imageType: GetImageType(type),
				format: Vk.Format.R8G8B8A8UNorm,
				extent: new Vk.Extent3D(size.w, size.h, size.d),
				mipLevels: 1,
				arrayLayers: size.l,
				samples: Vk.SampleCountFlags.SampleCount1,
				tiling: Vk.ImageTiling.Optimal,
				usage: Vk.ImageUsageFlags.TransferDestination | Vk.ImageUsageFlags.Sampled,
				sharingMode: Vk.SharingMode.Exclusive,
				queueFamilyIndices: Vk.Constants.QueueFamilyIgnored,
				initialLayout: Vk.ImageLayout.Undefined
			);
			VkView = dev.VkDevice.CreateImageView(
				image: VkImage,
				viewType: (Vk.ImageViewType)type,
				format: Vk.Format.R8G8B8A8UNorm,
				components: Vk.ComponentMapping.Identity,
				subresourceRange: new Vk.ImageSubresourceRange(Vk.ImageAspectFlags.Color, 0, 1, 0, size.l),
				flags: Vk.ImageViewCreateFlags.None
			);

			// Create and bind the backing memory
			var memReq = VkImage.GetMemoryRequirements();
			var memIdx = dev.Memory.Find(memReq.MemoryTypeBits, Vk.MemoryPropertyFlags.DeviceLocal);
			if (!memIdx.HasValue)
				throw new InvalidOperationException("Device does not support textures.");
			VkMemory = dev.VkDevice.AllocateMemory(
				allocationSize: memReq.Size,
				memoryTypeIndex: memIdx.Value
			);
			VkImage.BindMemory(VkMemory, 0);
			DataSize = (uint)memReq.Size;

			// Make the initial layout transition
			using (var buf = dev.GetScratchCommandBuffer())
			{
				buf.Buffer.Begin(Vk.CommandBufferUsageFlags.OneTimeSubmit);
				buf.Buffer.PipelineBarrier(
					sourceStageMask: Vk.PipelineStageFlags.TopOfPipe,
					destinationStageMask: Vk.PipelineStageFlags.Transfer | Vk.PipelineStageFlags.FragmentShader,
					memoryBarriers: null,
					bufferMemoryBarriers: null,
					imageMemoryBarriers: new Vk.ImageMemoryBarrier { 
						Image = VkImage,
						SourceAccessMask = Vk.AccessFlags.None,
						DestinationAccessMask = Vk.AccessFlags.MemoryRead | Vk.AccessFlags.MemoryWrite,
						SubresourceRange = new Vk.ImageSubresourceRange(Vk.ImageAspectFlags.Color, 0, 1, 0, size.l),
						OldLayout = Vk.ImageLayout.Undefined,
						NewLayout = Vk.ImageLayout.ShaderReadOnlyOptimal,
						SourceQueueFamilyIndex = Vk.Constants.QueueFamilyIgnored,
						DestinationQueueFamilyIndex = Vk.Constants.QueueFamilyIgnored
					}
				);
				buf.Buffer.End();
				buf.Submit();
			}
		}
		~Texture()
		{
			Dispose(false);
		}

		private protected unsafe void SetDataInternal(ReadOnlySpan<byte> data, in TextureRegion reg, uint layer)
		{
			using (var tb = Core.Instance.GraphicsDevice.GetTransferBuffer())
			{

			}

			throw new NotImplementedException();
		}

		#region IDisposable
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		// ALWAYS call base.Dispose(disposing)
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				VkView?.Dispose();
				VkImage.Dispose();
				VkMemory?.Free();
			}
			IsDisposed = true;
		}
		#endregion // IDisposable

		private static void CheckSize(TextureType type, in (uint w, uint h, uint d, uint l) size)
		{
			var dev = Core.Instance.GraphicsDevice;

			if (size.w == 0 || size.h == 0 || size.d == 0 || size.l == 0)
				throw new ArgumentException($"Zero texture size {size.w}x{size.h}x{size.d}x{size.l}.");
			if (size.l > dev.Limits.TextureLayers)
				throw new ArgumentException($"Texture layer count ({size.l}) greater than limit ({dev.Limits.TextureLayers}).");
			switch (type)
			{
				case TextureType.Tex1D:
				case TextureType.Tex1DArray:
					if (size.w > dev.Limits.TextureSize1D) throw new ArgumentOutOfRangeException(
						$"Texture size ({size.w}) greater than 1D limit ({dev.Limits.TextureSize1D})."
					);
					break;
				case TextureType.Tex2D:
				case TextureType.Tex2DArray:
					if (size.w > dev.Limits.TextureSize2D || size.h > dev.Limits.TextureSize2D) throw new ArgumentOutOfRangeException(
						$"Texture size ({size.w}x{size.h}) greater than 2D limit ({dev.Limits.TextureSize2D})."
					);
					break;
				case TextureType.Tex3D:
					if (size.w > dev.Limits.TextureSize3D || size.h > dev.Limits.TextureSize3D || size.d > dev.Limits.TextureSize3D)
						throw new ArgumentOutOfRangeException(
							$"Texture size ({size.w}x{size.h}x{size.d}) greater than 3D limit ({dev.Limits.TextureSize3D})."
						);
					break;
			}
		}

		private static Vk.ImageType GetImageType(TextureType tt) =>
			((tt == TextureType.Tex1D) || (tt == TextureType.Tex1DArray)) ? Vk.ImageType.Image1d :
			((tt == TextureType.Tex2D) || (tt == TextureType.Tex2DArray)) ? Vk.ImageType.Image2d :
			Vk.ImageType.Image3d;
	}

	/// <summary>
	/// The different texture dimensionalities.
	/// </summary>
	public enum TextureType
	{
		/// <summary>
		/// Texture with data in one dimension.
		/// </summary>
		Tex1D = Vk.ImageViewType.ImageView1d,
		/// <summary>
		/// Array of textures with data in one dimension.
		/// </summary>
		Tex1DArray = Vk.ImageViewType.ImageView1dArray,
		/// <summary>
		/// Texture with data in two dimensions.
		/// </summary>
		Tex2D = Vk.ImageViewType.ImageView2d,
		/// <summary>
		/// Array of textures with data in two dimensions.
		/// </summary>
		Tex2DArray = Vk.ImageViewType.ImageView2dArray,
		/// <summary>
		/// Texture with data in three dimensions.
		/// </summary>
		Tex3D = Vk.ImageType.Image3d
	}
}
