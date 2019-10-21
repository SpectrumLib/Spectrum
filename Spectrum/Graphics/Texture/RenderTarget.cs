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
	/// as a source of data in future shader invocations. 
	/// <para>
	/// Note that this type is reference counted by the <see cref="Pipeline"/> instances that use it, and attempting
	/// to dispose a render target that is still in use will result in an error.
	/// </para>
	/// </summary>
	public class RenderTarget : IDisposable
	{
		#region Fields
		/// <summary>
		/// The width of the render target texture.
		/// </summary>
		public uint Width { get; private set; }
		/// <summary>
		/// The height of the render target texture.
		/// </summary>
		public uint Height { get; private set; }
		/// <summary>
		/// The format of the render target texels.
		/// </summary>
		public readonly TexelFormat Format;
		/// <summary>
		/// The optional name for this render target, useful for identification and debugging.
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
		/// Gets if the format of this render target allows it to store depth information.
		/// </summary>
		public bool HasDepth => Format.IsDepthFormat();
		/// <summary>
		/// Gets if the format of this render target allows it to store stencil information.
		/// </summary>
		public bool HasStencil => Format.HasStencilComponent();

		/// <summary>
		/// A reference to the graphics device managing this render target.
		/// </summary>
		protected GraphicsDevice Device => Core.Instance.GraphicsDevice;

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
		private readonly object _refLock = new object();
		/// <summary>
		/// Gets the number of <see cref="Pipeline"/> instances currently using the render target. This property is
		/// thread-safe.
		/// </summary>
		public uint ReferenceCount
		{
			get { lock (_refLock) { return _refCount; } }
		}

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
			if ((width == 0) || (height == 0))
				throw new ArgumentException($"Render target with zero dimension ({width}x{height})");
			if ((width > Device.Limits.MaxTextureSize2D) || (height > Device.Limits.MaxTextureSize2D))
				throw new ArgumentException($"Render target larger than the image size limits ({width}x{height} > {Device.Limits.MaxTextureSize2D})");
			Format = format;
			VkAspects = HasDepth ? (Vk.ImageAspectFlags.Depth | (HasStencil ? Vk.ImageAspectFlags.Stencil : 0)) : Vk.ImageAspectFlags.Color;
			DefaultImageLayout = HasDepth ? Vk.ImageLayout.DepthStencilAttachmentOptimal : Vk.ImageLayout.ColorAttachmentOptimal;
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
			
			// Destroy the existing objects, if needed
			VkView?.Dispose();
			VkImage?.Dispose();
			VkMemory?.Free();

			// Create the image
			VkImage = Device.VkDevice.CreateImage(
				imageType: Vk.ImageType.Image2d,
				format: (Vk.Format)Format,
				extent: new Vk.Extent3D(width, height, 1),
				mipLevels: 1,
				arrayLayers: 1,
				samples: Vk.SampleCountFlags.SampleCount1,
				tiling: Vk.ImageTiling.Optimal,
				usage: Vk.ImageUsageFlags.Sampled | Vk.ImageUsageFlags.TransferSource | Vk.ImageUsageFlags.TransferDestination |
					   (HasDepth ? Vk.ImageUsageFlags.DepthStencilAttachment : Vk.ImageUsageFlags.ColorAttachment),
				sharingMode: Vk.SharingMode.Exclusive,
				queueFamilyIndices: new [] { Device.Queues.FamilyIndex },
				initialLayout: Vk.ImageLayout.Undefined,
				flags: Vk.ImageCreateFlags.None
			);

			// Create the backing memory for the image
			var memReq = VkImage.GetMemoryRequirements();
			var memIdx = Device.FindMemoryTypeIndex(memReq.MemoryTypeBits, Vk.MemoryPropertyFlags.DeviceLocal);
			if (memIdx == -1)
				throw new InvalidOperationException("No support for RenderTargets in device memory.");
			VkMemory = Device.VkDevice.AllocateMemory(
				allocationSize: memReq.Size,
				memoryTypeIndex: (uint)memIdx
			);
			DataSize = (uint)memReq.Size;
			VkImage.BindMemory(VkMemory, 0);

			// Create the view
			VkView = Device.VkDevice.CreateImageView(
				image: VkImage,
				viewType: Vk.ImageViewType.ImageView2d,
				format: (Vk.Format)Format,
				components: Vk.ComponentMapping.Identity,
				subresourceRange: new Vk.ImageSubresourceRange(VkAspects, 0, 1, 0, 1),
				flags: Vk.ImageViewCreateFlags.None
			);

			// Make the initial layout transition
			Device.SubmitScratchCommand(buf => {
				buf.PipelineBarrier(
					sourceStageMask: Vk.PipelineStageFlags.AllGraphics,
					destinationStageMask: Vk.PipelineStageFlags.AllGraphics,
					sourceAccessMask: Vk.AccessFlags.None,
					destinationAccessMask: Vk.AccessFlags.None,
					oldLayout: Vk.ImageLayout.Undefined,
					newLayout: DefaultImageLayout,
					sourceQueueFamilyIndex: Device.Queues.FamilyIndex,
					destinationQueueFamilyIndex: Device.Queues.FamilyIndex,
					image: VkImage,
					subresourceRange: new Vk.ImageSubresourceRange(VkAspects, 0, 1, 0, 1),
					dependencyFlags: Vk.DependencyFlags.None
				);
			});

			// Build the transitions
			ClearBarrier = new Vk.ImageMemoryBarrier
			{
				Image = VkImage,
				SubresourceRange = new Vk.ImageSubresourceRange(VkAspects, 0, 1, 0, 1),
				SourceAccessMask = Vk.AccessFlags.None,
				DestinationAccessMask = Vk.AccessFlags.None,
				OldLayout = DefaultImageLayout,
				NewLayout = Vk.ImageLayout.TransferDestinationOptimal,
				SourceQueueFamilyIndex = Device.Queues.FamilyIndex,
				DestinationQueueFamilyIndex = Device.Queues.FamilyIndex
			};
			AttachBarrier = new Vk.ImageMemoryBarrier
			{
				Image = VkImage,
				SubresourceRange = new Vk.ImageSubresourceRange(VkAspects, 0, 1, 0, 1),
				SourceAccessMask = Vk.AccessFlags.None,
				DestinationAccessMask = Vk.AccessFlags.None,
				OldLayout = Vk.ImageLayout.TransferDestinationOptimal,
				NewLayout = DefaultImageLayout,
				SourceQueueFamilyIndex = Device.Queues.FamilyIndex,
				DestinationQueueFamilyIndex = Device.Queues.FamilyIndex
			};
		}

		/// <summary>
		/// Clears this render target to be all the same color. This is only valid if the render target format is a
		/// color format, otherwise an exception is thrown.
		/// </summary>
		/// <param name="c">The color to clear the render target to.</param>
		public void ClearColor(Color c)
		{
			if (!Format.IsColorFormat())
				throw new InvalidOperationException("Color render target cleared with depth/stencil value.");

			Device.SubmitScratchCommand(buf => {
				buf.PipelineBarrier(
					sourceStageMask: Vk.PipelineStageFlags.AllGraphics,
					destinationStageMask: Vk.PipelineStageFlags.AllGraphics,
					memoryBarriers: null,
					bufferMemoryBarriers: null,
					imageMemoryBarriers: new [] { ClearBarrier }
				);
				buf.ClearColorImage(
					VkImage, 
					Vk.ImageLayout.TransferDestinationOptimal, 
					new Vk.ClearColorValue(c.RFloat, c.GFloat, c.BFloat, c.AFloat),
					new [] { new Vk.ImageSubresourceRange(VkAspects, 0, 1, 0, 1) }
				);
				buf.PipelineBarrier(
					sourceStageMask: Vk.PipelineStageFlags.AllGraphics,
					destinationStageMask: Vk.PipelineStageFlags.AllGraphics,
					memoryBarriers: null,
					bufferMemoryBarriers: null,
					imageMemoryBarriers: new [] { AttachBarrier }
				);
			});
		}
		/// <summary>
		/// Clears this render target to the specified depth and stencil values. This is only valid if the render
		/// target format is a depth/stencil format, otherwise an exception is thrown. If the render target does not
		/// have a stencil component, then the passed stencil value is ignored.
		/// </summary>
		/// <param name="depth">The depth value to clear the render target to.</param>
		/// <param name="stencil">The stencil value to clear the render target to.</param>
		public void ClearDepth(float depth = 1, byte stencil = 0)
		{
			if (!Format.IsDepthFormat())
				throw new InvalidOperationException("Depth/stencil render target cleared with color value.");

			Device.SubmitScratchCommand(buf => {
				buf.PipelineBarrier(
					sourceStageMask: Vk.PipelineStageFlags.AllGraphics,
					destinationStageMask: Vk.PipelineStageFlags.AllGraphics,
					memoryBarriers: null,
					bufferMemoryBarriers: null,
					imageMemoryBarriers: new[] { ClearBarrier }
				);
				buf.ClearDepthStencilImage(
					VkImage,
					Vk.ImageLayout.TransferDestinationOptimal,
					new Vk.ClearDepthStencilValue(depth, stencil),
					new Vk.ImageSubresourceRange(VkAspects, 0, 1, 0, 1)
				);
				buf.PipelineBarrier(
					sourceStageMask: Vk.PipelineStageFlags.AllGraphics,
					destinationStageMask: Vk.PipelineStageFlags.AllGraphics,
					memoryBarriers: null,
					bufferMemoryBarriers: null,
					imageMemoryBarriers: new[] { AttachBarrier }
				);
			});
		}

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
					throw new InvalidOperationException($"Disposing render target \"{Name ?? "none"}\" still in use (uses = {_refCount})");

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
