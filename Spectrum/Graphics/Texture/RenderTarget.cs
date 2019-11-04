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
	/// A special type of texture that can be used as the target for pipeline draw commands. It can then be used
	/// as a source of data in future shader invocations. These are reference counted in <see cref="Pipeline"/>
	/// instances, and attempting to dispose of a referenced RenderTarget will generate an exception.
	/// </summary>
	public sealed class RenderTarget : IDisposable
	{
		#region Fields
		/// <summary>
		/// The width of the render target.
		/// </summary>
		public uint Width { get; private set; }
		/// <summary>
		/// The height of the render target.
		/// </summary>
		public uint Height { get; private set; }
		/// <summary>
		/// The format of the render target texels.
		/// </summary>
		public readonly TexelFormat Format;
		/// <summary>
		/// The optional debug name for this render target.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// The size of the backing memory for the render target, in bytes.
		/// </summary>
		public uint DataSize { get; private set; }

		/// <summary>
		/// Gets the size of the render target as an <see cref="Extent"/>.
		/// </summary>
		public Extent Size => new Extent(Width, Height);
		/// <summary>
		/// Gets if this render target stores color data.
		/// </summary>
		public bool IsColorTarget => Format.IsColorFormat();
		/// <summary>
		/// Gets if this render target stores depth data.
		/// </summary>
		public bool IsDepthTarget => Format.IsDepthFormat();
		/// <summary>
		/// Gets if this render target stores stencil data.
		/// </summary>
		public bool HasStencilData => Format.HasStencilComponent();

		/// <summary>
		/// Gets the default viewport to access the entire render target.
		/// </summary>
		public Viewport DefaultViewport => new Viewport(0, 0, Width, Height);
		/// <summary>
		/// Gets the default scissor to access the entire render target.
		/// </summary>
		public Scissor DefaultScissor => new Scissor(0, 0, Width, Height);

		// Vulkan objects
		internal Vk.Image VkImage { get; private set; } = null;
		internal Vk.DeviceMemory VkMemory { get; private set; } = null;
		internal Vk.ImageView VkView { get; private set; } = null;
		internal readonly Vk.ImageAspectFlags VkAspects;
		internal readonly Vk.ImageLayout DefaultImageLayout;

		// Clear objects
		internal Vk.ImageMemoryBarrier ClearBarrier { get; private set; } // Transition to transfer dst
		internal Vk.ImageMemoryBarrier AttachBarrier { get; private set; } // Transition to attachment

		// Reference counting
		private uint _refCount = 0;
		/// <summary>
		/// Gets the number of <see cref="Pipeline"/> instances currently using the render target. This property is
		/// thread-safe.
		/// </summary>
		public uint ReferenceCount => _refCount;

		private bool _isDisposed = false;
		#endregion // Fields

		/// <summary>
		/// Creates a new render target texture for immediate use.
		/// </summary>
		/// <param name="width">The width of the render target.</param>
		/// <param name="height">The height of the render target.</param>
		/// <param name="format">The render target format.</param>
		/// <param name="name">An optional name for the render target for identification and debugging.</param>
		public RenderTarget(uint width, uint height, TexelFormat format, string name = null)
		{
			var dev = Core.Instance.GraphicsDevice;

			if ((width == 0) || (height == 0))
				throw new ArgumentException($"Render target \"{Name ?? "unnamed"}\" with zero dimension.");
			if ((width > dev.Limits.MaxFramebufferWidth) || (height > dev.Limits.MaxFramebufferHeight))
				throw new ArgumentException($"Render target \"{Name ?? "unnamed"}\" larger than size limits.");
			Format = format;
			VkAspects = IsDepthTarget ? (Vk.ImageAspectFlags.Depth | (HasStencilData ? Vk.ImageAspectFlags.Stencil : 0)) : Vk.ImageAspectFlags.Color;
			DefaultImageLayout = IsDepthTarget ? Vk.ImageLayout.DepthStencilAttachmentOptimal : Vk.ImageLayout.ColorAttachmentOptimal;
			Name = name;

			Rebuild(width, height);
		}
		~RenderTarget()
		{
			dispose(false);
		}

		/// <summary>
		/// Rebuilds the render target to use a new size.
		/// </summary>
		/// <param name="width">The new width of the render target.</param>
		/// <param name="height">The new height of the render target.</param>
		/// <param name="force">If <c>true</c>, the render target will be rebuilt even if the size doesnt change.</param>
		public void Rebuild(uint width, uint height, bool force = false)
		{
			if (!force && width == Width && height == Height)
				return;
			Width = width;
			Height = height;

			var dev = Core.Instance.GraphicsDevice;

			// Destroy the existing objects, if needed
			VkView?.Dispose();
			VkImage?.Dispose();
			VkMemory?.Free();

			// Create the image
			VkImage = dev.VkDevice.CreateImage(
				imageType: Vk.ImageType.Image2d,
				format: (Vk.Format)Format,
				extent: new Vk.Extent3D(width, height, 1),
				mipLevels: 1,
				arrayLayers: 1,
				samples: Vk.SampleCountFlags.SampleCount1,
				tiling: Vk.ImageTiling.Optimal,
				usage: Vk.ImageUsageFlags.Sampled | Vk.ImageUsageFlags.TransferSource | Vk.ImageUsageFlags.TransferDestination |
					   (IsDepthTarget ? Vk.ImageUsageFlags.DepthStencilAttachment : Vk.ImageUsageFlags.ColorAttachment),
				sharingMode: Vk.SharingMode.Exclusive,
				queueFamilyIndices: new [] { Vk.Constants.QueueFamilyIgnored },
				initialLayout: Vk.ImageLayout.Undefined,
				flags: Vk.ImageCreateFlags.None
			);

			// Create the backing memory for the image
			var memReq = VkImage.GetMemoryRequirements();
			var memIdx = dev.Memory.Find(memReq.MemoryTypeBits, Vk.MemoryPropertyFlags.DeviceLocal);
			if (!memIdx.HasValue)
				throw new InvalidOperationException("Device memory does not support render targets.");
			VkMemory = dev.VkDevice.AllocateMemory(
				allocationSize: memReq.Size,
				memoryTypeIndex: memIdx.Value
			);
			DataSize = (uint)memReq.Size;
			VkImage.BindMemory(VkMemory, 0);

			// Create the view
			VkView = dev.VkDevice.CreateImageView(
				image: VkImage,
				viewType: Vk.ImageViewType.ImageView2d,
				format: (Vk.Format)Format,
				components: Vk.ComponentMapping.Identity,
				subresourceRange: new Vk.ImageSubresourceRange(VkAspects, 0, 1, 0, 1),
				flags: Vk.ImageViewCreateFlags.None
			);

			// Make the initial layout transition
			using (var sb = dev.GetScratchCommandBuffer())
			{
				sb.Buffer.Begin(Vk.CommandBufferUsageFlags.OneTimeSubmit);

				sb.Buffer.PipelineBarrier(
					sourceStageMask: Vk.PipelineStageFlags.TopOfPipe,
					destinationStageMask: Vk.PipelineStageFlags.EarlyFragmentTests | Vk.PipelineStageFlags.Transfer,
					memoryBarriers: null,
					bufferMemoryBarriers: null,
					imageMemoryBarriers: new [] { new Vk.ImageMemoryBarrier { 
						Image = VkImage,
						SourceAccessMask = Vk.AccessFlags.None,
						DestinationAccessMask = Vk.AccessFlags.MemoryRead,
						OldLayout = Vk.ImageLayout.Undefined,
						NewLayout = DefaultImageLayout,
						SourceQueueFamilyIndex = Vk.Constants.QueueFamilyIgnored,
						DestinationQueueFamilyIndex = Vk.Constants.QueueFamilyIgnored,
						SubresourceRange = new Vk.ImageSubresourceRange(VkAspects, 0, 1, 0, 1)
					}},
					dependencyFlags: Vk.DependencyFlags.ByRegion
				);

				sb.Buffer.End();
				sb.Submit();
			}

			// Build the transitions
			ClearBarrier = new Vk.ImageMemoryBarrier {
				Image = VkImage,
				SubresourceRange = new Vk.ImageSubresourceRange(VkAspects, 0, 1, 0, 1),
				SourceAccessMask = Vk.AccessFlags.MemoryWrite,
				DestinationAccessMask = Vk.AccessFlags.TransferRead,
				OldLayout = DefaultImageLayout,
				NewLayout = Vk.ImageLayout.TransferDestinationOptimal,
				SourceQueueFamilyIndex = Vk.Constants.QueueFamilyIgnored,
				DestinationQueueFamilyIndex = Vk.Constants.QueueFamilyIgnored
			};
			AttachBarrier = new Vk.ImageMemoryBarrier {
				Image = VkImage,
				SubresourceRange = new Vk.ImageSubresourceRange(VkAspects, 0, 1, 0, 1),
				SourceAccessMask = Vk.AccessFlags.TransferWrite,
				DestinationAccessMask = Vk.AccessFlags.MemoryRead,
				OldLayout = Vk.ImageLayout.TransferDestinationOptimal,
				NewLayout = DefaultImageLayout,
				SourceQueueFamilyIndex = Vk.Constants.QueueFamilyIgnored,
				DestinationQueueFamilyIndex = Vk.Constants.QueueFamilyIgnored
			};
		}

		#region Clearing
		/// <summary>
		/// Clears this render target to a color. Throws an exception for non-color targets.
		/// </summary>
		/// <param name="c">The color to clear the render target to.</param>
		public void ClearColor(Color c)
		{
			if (!IsColorTarget)
				throw new InvalidOperationException($"Depth/stencil render target \"{Name ?? "unnamed"}\" cleared with color value.");

			using (var sb = Core.Instance.GraphicsDevice.GetScratchCommandBuffer())
			{
				sb.Buffer.Begin(Vk.CommandBufferUsageFlags.OneTimeSubmit);

				sb.Buffer.PipelineBarrier(
					sourceStageMask: Vk.PipelineStageFlags.ColorAttachmentOutput,
					destinationStageMask: Vk.PipelineStageFlags.Transfer,
					memoryBarriers: null,
					bufferMemoryBarriers: null,
					imageMemoryBarriers: new [] { ClearBarrier }
				);
				sb.Buffer.ClearColorImage(
					VkImage,
					Vk.ImageLayout.TransferDestinationOptimal,
					new Vk.ClearColorValue(c.RFloat, c.GFloat, c.BFloat, c.AFloat),
					new [] { new Vk.ImageSubresourceRange(VkAspects, 0, 1, 0, 1) }
				);
				sb.Buffer.PipelineBarrier(
					sourceStageMask: Vk.PipelineStageFlags.Transfer,
					destinationStageMask: Vk.PipelineStageFlags.VertexShader,
					memoryBarriers: null,
					bufferMemoryBarriers: null,
					imageMemoryBarriers: new [] { AttachBarrier }
				);

				sb.Buffer.End();
				sb.Submit();
			}
		}

		/// <summary>
		/// Clears this render target to the specified depth/stencil values. Throws an exception for non-depth/stencil
		/// targets.
		/// </summary>
		/// <param name="depth">The depth value to clear the render target to.</param>
		/// <param name="stencil">The stencil value to clear the render target to.</param>
		public void ClearDepth(float depth = 1, byte stencil = 0)
		{
			if (!Format.IsDepthFormat())
				throw new InvalidOperationException($"Color render target \"{Name ?? "unnamed"}\" cleared with depth/stencil value.");

			using (var sb = Core.Instance.GraphicsDevice.GetScratchCommandBuffer())
			{
				sb.Buffer.Begin(Vk.CommandBufferUsageFlags.OneTimeSubmit);

				sb.Buffer.PipelineBarrier(
					sourceStageMask: Vk.PipelineStageFlags.ColorAttachmentOutput,
					destinationStageMask: Vk.PipelineStageFlags.Transfer,
					memoryBarriers: null,
					bufferMemoryBarriers: null,
					imageMemoryBarriers: new[] { ClearBarrier }
				);
				sb.Buffer.ClearDepthStencilImage(
					VkImage,
					Vk.ImageLayout.TransferDestinationOptimal,
					new Vk.ClearDepthStencilValue(depth, stencil),
					new Vk.ImageSubresourceRange(VkAspects, 0, 1, 0, 1)
				);
				sb.Buffer.PipelineBarrier(
					sourceStageMask: Vk.PipelineStageFlags.Transfer,
					destinationStageMask: Vk.PipelineStageFlags.VertexShader,
					memoryBarriers: null,
					bufferMemoryBarriers: null,
					imageMemoryBarriers: new[] { AttachBarrier }
				);

				sb.Buffer.End();
				sb.Submit();
			}
		}
		#endregion // Clearing

		#region IDisposable
		public void Dispose()
		{
			dispose(true);
			GC.SuppressFinalize(this);
		}

		private void dispose(bool disposing)
		{
			if (disposing && !_isDisposed)
			{
				if (ReferenceCount > 0)
					throw new InvalidOperationException($"Disposing render target \"{Name ?? "unnamed"}\" still in use (uses = {_refCount}).");

				VkView?.Dispose();
				VkImage.Dispose();
				VkMemory?.Free();
			}
			_isDisposed = true;
		}
		#endregion // IDisposable

		/// <summary>
		/// Creates a new default render target for holding color data (R8G8B8A8UNorm format).
		/// </summary>
		/// <param name="width">The width of the render target.</param>
		/// <param name="height">The height of the render target.</param>
		/// <param name="name">The optional render target name.</param>
		/// <returns>A new render target designed to hold color data.</returns>
		public static RenderTarget CreateColor(uint width, uint height, string name = null) =>
			new RenderTarget(width, height, TexelFormat.Color, name);

		/// <summary>
		/// Creates a new default render target for holding depth (and optionally stencil) data.
		/// </summary>
		/// <param name="width">The width of the render target.</param>
		/// <param name="height">The height of the render target.</param>
		/// <param name="stencil">
		/// If the render target should also support stencil data. If <c>true</c>, the render target will use 
		/// <see cref="TexelFormat.Depth24Stencil8"/>, otherwise it will use <see cref="TexelFormat.Depth32"/>.
		/// </param>
		/// <param name="name">The optional render target name.</param>
		/// <returns>A new render target designed to hold depth (and optionally stencil) data.</returns>
		public static RenderTarget CreateDepth(uint width, uint height, bool stencil = true, string name = null) =>
			new RenderTarget(width, height, stencil ? TexelFormat.Depth24Stencil8 : TexelFormat.Depth32, name);
	}
}
